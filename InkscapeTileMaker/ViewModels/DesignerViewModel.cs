using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InkscapeTileMaker.Services;
using Microsoft.Maui.ApplicationModel;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Xml.Linq;

namespace InkscapeTileMaker.ViewModels
{
	public partial class DesignerViewModel : ObservableObject
	{
		private readonly SvgConnectionService _svgConnectionService;
		private readonly IWindowService _windowService;
		private readonly ISvgRenderingService _svgRenderingService;
		private readonly IFileSaver _fileSaver;

		[ObservableProperty]
		private string fileName;

		[ObservableProperty]
		private PointF previewOffset;

		[ObservableProperty]
		private ObservableCollection<decimal> zoomLevels =
		[
			0.1m,
			0.25m,
			0.5m,
			0.75m,
			1.0m,
			1.25m,
			1.5m,
			2.0m,
			3.0m,
			4.0m,
			5.0m,
		];

		[ObservableProperty]
		private decimal _selectedZoomLevel;

		private SKBitmap? _renderedBitmap;

		public SvgConnectionService SvgConnectionService => _svgConnectionService;

		public event Action CanvasNeedsRedraw = delegate { };

		SKRect _previewRect;

		public DesignerViewModel(SvgConnectionService svgConnectionService, IWindowService windowService, ISvgRenderingService svgRenderingService, IFileSaver fileSaver)
		{
			_svgConnectionService = svgConnectionService;
			_svgConnectionService.DocumentLoaded += UpdateSVG;
			_windowService = windowService;
			_svgRenderingService = svgRenderingService;
			_fileSaver = fileSaver;

			SelectedZoomLevel = 1.0m;
		}

		~DesignerViewModel()
		{
			_svgConnectionService.DocumentLoaded -= UpdateSVG;
		}

		private void UpdateSVG(XDocument obj)
		{
			_renderedBitmap?.Dispose();
			_renderedBitmap = null;

			var svgFile = _svgConnectionService.SvgFile;
			if (svgFile == null) return;

			Task.Run(async () =>
			{
				using var stream = await _svgRenderingService.RenderSvgFile(svgFile);
				if (stream.CanSeek) stream.Position = 0;
				_renderedBitmap = SKBitmap.Decode(stream);
				await MainThread.InvokeOnMainThreadAsync(CanvasNeedsRedraw);
			});
		}

		public void RenderCanvas(SKCanvas canvas, int width, int height) // note that this must run synchronously
		{
			if (_svgConnectionService.Document != null && _svgConnectionService.SvgRoot != null)
			{
				var svgElement = _svgConnectionService.SvgRoot;
				float w = Convert.ToSingle(svgElement.Attribute("width")?.Value) * (float)SelectedZoomLevel;
				float h = Convert.ToSingle(svgElement.Attribute("height")?.Value) * (float)SelectedZoomLevel;
				_previewRect = new SKRect(PreviewOffset.X, PreviewOffset.Y, w, h);
			}
			else
			{
				_previewRect = new SKRect(PreviewOffset.X, PreviewOffset.Y, width, height);
			}

			DrawTransparentBackground(canvas, width, height, PreviewOffset);

			if (_svgConnectionService.SvgFile == null)
			{
				using var textPaint = new SKPaint
				{
					Color = SKColors.Black,
					IsAntialias = true,
				};

				using var font = SKTypeface.FromFamilyName("Arial");
				using var skFont = new SKFont(font, 24);

				var message = "No SVG file loaded";
				var x = width / 2f;
				var y = height / 2f;

				canvas.DrawText(message, x, y, SKTextAlign.Center, skFont, textPaint);
				return;
			}

			if (_renderedBitmap == null)
			{
				using var errorPaint = new SKPaint
				{
					Color = SKColors.Red,
					IsStroke = true,
					StrokeWidth = 3
				};

				canvas.DrawRect(new SKRect(10, 10, width - 10, height - 10), errorPaint);
				canvas.DrawLine(10, 10, width - 10, height - 10, errorPaint);
				canvas.DrawLine(width - 10, 10, 10, height - 10, errorPaint);
				return;
			}

			var destPoint = new SKPoint((width - _renderedBitmap.Width) / 2f + PreviewOffset.X, (height - _renderedBitmap.Height) / 2f + PreviewOffset.Y);
			canvas.DrawBitmap(_renderedBitmap, destPoint);

			if (_svgConnectionService.Document != null)
			{
				var gridElement = _svgConnectionService.Grid;
				if (gridElement != null) DrawGrid(canvas, gridElement, PreviewOffset, SelectedZoomLevel);

				var svgElement = _svgConnectionService.SvgRoot;
				if (svgElement != null) DrawBorder(canvas, svgElement, PreviewOffset, SelectedZoomLevel);
			}
		}

		partial void OnSelectedZoomLevelChanged(decimal value)
		{
			CanvasNeedsRedraw.Invoke();
		}

		partial void OnPreviewOffsetChanged(PointF value)
		{
			CanvasNeedsRedraw.Invoke();
		}

		#region Drawing Methods

		private void DrawTransparentBackground(SKCanvas canvas, int width, int height, PointF offset)
		{
			const int tileSize = 20;
			using var lightPaint = new SKPaint { Color = new SKColor(0xEE, 0xEE, 0xEE) };
			using var darkPaint = new SKPaint { Color = new SKColor(0xCC, 0xCC, 0xCC) };

			// Normalize offset so we always fill the entire canvas
			var offsetX = offset.X % tileSize;
			var offsetY = offset.Y % tileSize;

			if (offsetX < 0)
				offsetX += tileSize;
			if (offsetY < 0)
				offsetY += tileSize;

			for (var y = -tileSize; y < height + tileSize; y += tileSize)
			{
				for (var x = -tileSize; x < width + tileSize; x += tileSize)
				{
					var tileX = x + offsetX;
					var tileY = y + offsetY;

					// Determine which color this tile should be, taking offset into account
					var useDark = (((int)Math.Floor(tileX / (float)tileSize)) +
					               ((int)Math.Floor(tileY / (float)tileSize))) % 2 == 0;

					var rect = new SKRect(tileX, tileY, tileX + tileSize, tileY + tileSize);
					canvas.DrawRect(rect, useDark ? darkPaint : lightPaint);
				}
			}
		}

		private void DrawGrid(SKCanvas canvas, XElement gridElement, PointF offset, decimal zoom)
		{
			if (!((bool?)gridElement.Attribute("enabled") ?? true)) return;
			if (gridElement.Attribute("units")?.Value != "px") return;

			var color = SKColor.Parse(gridElement.Attribute("color")?.Value ?? "#0099e5");
			var empColor = SKColor.Parse(gridElement.Attribute("empcolor")?.Value ?? "#e500a7");

			float spacingX = Convert.ToSingle(gridElement.Attribute("spacingx")?.Value);
			float spacingY = Convert.ToSingle(gridElement.Attribute("spacingy")?.Value);
			int empSpacing = Convert.ToInt32(gridElement.Attribute("empspacing")?.Value);

			if (spacingX <= 0 || spacingY <= 0 || empSpacing <= 0 || zoom <= 0)
			{
				return;
			}

			// Apply zoom (unit scale)
			spacingX *= (float)zoom;
			spacingY *= (float)zoom;

			using var normalPaint = new SKPaint
			{
				Color = color,
				StrokeWidth = 1,
				IsAntialias = false,
				Style = SKPaintStyle.Stroke,
			};

			using var empPaint = new SKPaint
			{
				Color = empColor,
				StrokeWidth = 1,
				IsAntialias = false,
				Style = SKPaintStyle.Stroke
			};

			var width = canvas.DeviceClipBounds.Width;
			var height = canvas.DeviceClipBounds.Height;

			// Scale offset with zoom so grid stays aligned with content
			var scaledOffsetX = offset.X * (float)zoom;
			var scaledOffsetY = offset.Y * (float)zoom;

			// Align grid with offset (same logic as transparent background, but zoom-aware)
			var offsetX = scaledOffsetX % spacingX;
			var offsetY = scaledOffsetY % spacingY;

			if (offsetX < 0)
				offsetX += spacingX;
			if (offsetY < 0)
				offsetY += spacingY;

			// Vertical lines
			int verticalIndex = 0;
			for (float x = -spacingX; x <= width + spacingX; x += spacingX)
			{
				var drawX = x + offsetX;
				if (drawX < 0 || drawX > width) continue;

				var paint = (verticalIndex % empSpacing == 0) ? empPaint : normalPaint;
				canvas.DrawLine(drawX, 0, drawX, height, paint);
				verticalIndex++;
			}

			// Horizontal lines
			int horizontalIndex = 0;
			for (float y = -spacingY; y <= height + spacingY; y += spacingY)
			{
				var drawY = y + offsetY;
				if (drawY < 0 || drawY > height) continue;

				var paint = (horizontalIndex % empSpacing == 0) ? empPaint : normalPaint;
				canvas.DrawLine(0, drawY, width, drawY, paint);
				horizontalIndex++;
			}
		}

		private void DrawBorder(SKCanvas canvas, XElement svgElement, PointF offset, decimal zoom)
		{
			float width = Convert.ToSingle(svgElement.Attribute("width")?.Value);
			float height = Convert.ToSingle(svgElement.Attribute("height")?.Value);
			var namedViewElement = svgElement.Element(XName.Get("namedview", "sodipodi"));
			SKColor borderColor = SKColor.Parse(namedViewElement?.Attribute("bordercolor")?.Value ?? "#ffffff");

			if (width <= 0 || height <= 0 || zoom <= 0)
			{
				return;
			}

			// Apply zoom (unit scale)
			width *= (float)zoom;
			height *= (float)zoom;

			// Canvas bounds
			var canvasWidth = canvas.DeviceClipBounds.Width;
			var canvasHeight = canvas.DeviceClipBounds.Height;

			// Box position aligned to offset and zoom
			var boxLeft = offset.X * (float)zoom;
			var boxTop = offset.Y * (float)zoom;
			var boxRight = boxLeft + width;
			var boxBottom = boxTop + height;

			using var borderPaint = new SKPaint
			{
				Color = SKColors.Black,
				Style = SKPaintStyle.Fill,
				IsAntialias = false
			};

			// Left area (to the left of the box, including top-left corner if in view)
			if (boxLeft > 0)
			{
				var leftRect = new SKRect(0, 0, Math.Min(boxLeft, canvasWidth), canvasHeight);
				if (!leftRect.IsEmpty)
				{
					canvas.DrawRect(leftRect, borderPaint);
				}
			}

			// Right area (to the right of the box)
			if (boxRight < canvasWidth)
			{
				var rightRect = new SKRect(Math.Max(boxRight, 0), 0, canvasWidth, canvasHeight);
				if (!rightRect.IsEmpty)
				{
					canvas.DrawRect(rightRect, borderPaint);
				}
			}

			// Top area (above the box)
			if (boxTop > 0)
			{
				var topRect = new SKRect(
					Math.Max(boxLeft, 0),
					0,
					Math.Min(boxRight, canvasWidth),
					Math.Min(boxTop, canvasHeight));
				if (!topRect.IsEmpty)
				{
					canvas.DrawRect(topRect, borderPaint);
				}
			}

			// Bottom area (below the box)
			if (boxBottom < canvasHeight)
			{
				var bottomRect = new SKRect(
					Math.Max(boxLeft, 0),
					Math.Max(boxBottom, 0),
					Math.Min(boxRight, canvasWidth),
					canvasHeight);
				if (!bottomRect.IsEmpty)
				{
					canvas.DrawRect(bottomRect, borderPaint);
				}
			}
		}

		#endregion

		#region Commands

		[RelayCommand]
		public async Task NewDesign()
		{
			_windowService.OpenDesignerWindow(svgFile: null);
		}

		[RelayCommand]
		public async Task OpenDesign()
		{
			var result = await FilePicker.PickAsync(new PickOptions
			{
				PickerTitle = "Open SVG design",
				FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
				{
					{ DevicePlatform.iOS, new[] { "public.svg" } },
					{ DevicePlatform.Android, new[] { "image/svg+xml" } },
					{ DevicePlatform.WinUI, new[] { ".svg" } },
					{ DevicePlatform.MacCatalyst, new[] { "public.svg" } },
				})
			});

			if (result == null)
			{
				return;
			}

			if (!string.Equals(Path.GetExtension(result.FullPath), ".svg", StringComparison.OrdinalIgnoreCase))
			{
				return;
			}

			var svgFile = new FileInfo(result.FullPath);
			_windowService.OpenDesignerWindow(svgFile);
		}

		[RelayCommand]
		public async Task SaveDesign()
		{
			var svgFile = _svgConnectionService.SvgFile;
			if (svgFile == null) return;
			_svgConnectionService.SaveSvg(svgFile);
		}

		[RelayCommand]
		public async Task SaveDesignAs()
		{
			var svgFile = _svgConnectionService.SvgFile;
			if (svgFile == null) return;

			using (var ms = new MemoryStream())
			{
				await _svgConnectionService.Document!.SaveAsync(ms, SaveOptions.None, CancellationToken.None);
				ms.Position = 0;
				var result = await _fileSaver.SaveAsync(svgFile.FullName, svgFile.Name, ms);
			}
		}

		[RelayCommand]
		public void ResetView()
		{
			PreviewOffset = new PointF(0, 0);
		}
		#endregion
	}
}
