using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InkscapeTileMaker.Services;
using Microsoft.Maui.ApplicationModel;
using SkiaSharp;
using System;
using System.Collections.Generic;
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

		private SKBitmap? _renderedBitmap;

		public SvgConnectionService SvgConnectionService => _svgConnectionService;

		public event Action CanvasNeedsRedraw = delegate { };

		public DesignerViewModel(SvgConnectionService svgConnectionService, IWindowService windowService, ISvgRenderingService svgRenderingService, IFileSaver fileSaver)
		{
			_svgConnectionService = svgConnectionService;
			_svgConnectionService.DocumentLoaded += UpdateSVG;
			_windowService = windowService;
			_svgRenderingService = svgRenderingService;
			_fileSaver = fileSaver;
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
			const int tileSize = 20;

			using (var lightPaint = new SKPaint { Color = new SKColor(0xEE, 0xEE, 0xEE) })
			using (var darkPaint = new SKPaint { Color = new SKColor(0xCC, 0xCC, 0xCC) })
			{
				for (var y = 0; y < height; y += tileSize)
				{
					for (var x = 0; x < width; x += tileSize)
					{
						var useDark = ((x / tileSize) + (y / tileSize)) % 2 == 0;
						var rect = new SKRect(x, y, x + tileSize, y + tileSize);
						canvas.DrawRect(rect, useDark ? darkPaint : lightPaint);
					}
				}
			}

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

			if (_svgConnectionService.Document != null)
			{
				var gridElement = _svgConnectionService.Grid;
				if (gridElement != null) DrawGrid(canvas, gridElement);

				var svgElement = _svgConnectionService.SvgRoot;
				if (svgElement != null) DrawBorder(canvas, svgElement);
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
				return;
			}

			var destPoint = new SKPoint((width - _renderedBitmap.Width) / 2f, (height - _renderedBitmap.Height) / 2f);
			canvas.DrawBitmap(_renderedBitmap, destPoint);
		}

		private void DrawGrid(SKCanvas canvas, XElement gridElement)
		{
			if (!((bool?)gridElement.Attribute("enabled") ?? true)) return;
			if (gridElement.Attribute("units")?.Value != "px") return;

			var color = SKColor.Parse(gridElement.Attribute("color")?.Value ?? "#0099e5");
			var empColor = SKColor.Parse(gridElement.Attribute("empcolor")?.Value ?? "#e500a7");

			float spacingX = Convert.ToSingle(gridElement.Attribute("spacingx")?.Value);
			float spacingY = Convert.ToSingle(gridElement.Attribute("spacingy")?.Value);
			int empSpacing = Convert.ToInt32(gridElement.Attribute("empspacing")?.Value);

			if (spacingX <= 0 || spacingY <= 0 || empSpacing <= 0)
			{
				return;
			}

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

			// Vertical lines
			int verticalIndex = 0;
			for (float x = 0; x <= width; x += spacingX)
			{
				var paint = (verticalIndex % empSpacing == 0) ? empPaint : normalPaint;
				canvas.DrawLine(x, 0, x, height, paint);
				verticalIndex++;
			}

			// Horizontal lines
			int horizontalIndex = 0;
			for (float y = 0; y <= height; y += spacingY)
			{
				var paint = (horizontalIndex % empSpacing == 0) ? empPaint : normalPaint;
				canvas.DrawLine(0, y, width, y, paint);
				horizontalIndex++;
			}
		}

		private void DrawBorder(SKCanvas canvas, XElement svgElement)
		{
			float width = Convert.ToSingle(svgElement.Attribute("width")?.Value);
			float height = Convert.ToSingle(svgElement.Attribute("height")?.Value);
			var namedViewElement = svgElement.Element(XName.Get("namedview", "sodipodi"));
			SKColor borderColor = SKColor.Parse(namedViewElement?.Attribute("bordercolor")?.Value ?? "#ffffff");

			// Canvas bounds
			var canvasWidth = canvas.DeviceClipBounds.Width;
			var canvasHeight = canvas.DeviceClipBounds.Height;

			// Clamp SVG width/height to canvas in case they are larger or invalid
			if (width <= 0 || height <= 0)
			{
				return;
			}

			var drawWidth = Math.Min(width, canvasWidth);
			var drawHeight = Math.Min(height, canvasHeight);

			using var borderPaint = new SKPaint
			{
				Color = SKColors.Black,
				Style = SKPaintStyle.Fill,
				IsAntialias = false
			};

			// Left area (excluding the top-left corner)
			if (drawWidth < canvasWidth)
			{
				var leftRect = new SKRect(0, drawHeight, canvasWidth - drawWidth, canvasHeight);
				if (!leftRect.IsEmpty)
				{
					canvas.DrawRect(leftRect, borderPaint);
				}
			}

			// Right area
			if (drawWidth < canvasWidth)
			{
				var rightRect = new SKRect(drawWidth, 0, canvasWidth, canvasHeight);
				if (!rightRect.IsEmpty)
				{
					canvas.DrawRect(rightRect, borderPaint);
				}
			}

			// Bottom area
			if (drawHeight < canvasHeight)
			{
				var bottomRect = new SKRect(0, drawHeight, drawWidth, canvasHeight);
				if (!bottomRect.IsEmpty)
				{
					canvas.DrawRect(bottomRect, borderPaint);
				}
			}
		}

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
	}
}
