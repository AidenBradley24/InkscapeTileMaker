using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InkscapeTileMaker.Models;
using InkscapeTileMaker.Services;
using SkiaSharp;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
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
		[NotifyPropertyChangedFor(nameof(Title))]
		public partial string? FileName { get; set; }

		[ObservableProperty]
		public partial ObservableCollection<decimal> ZoomLevels { get; set; } =
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
		public partial decimal SelectedZoomLevel { get; set; }

		[ObservableProperty]
		public partial PointF PreviewOffset { get; set; }

		[ObservableProperty]
		public partial TileViewModel? SelectedTile { get; set; }

		[ObservableProperty]
		public partial (int x, int y) TileSize { get; set; }

		[ObservableProperty]
		public partial ObservableCollection<TileViewModel> Tiles { get; set; } = [];

		[ObservableProperty]
		public partial (int row, int col)? HoveredTile { get; set; }

		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(Title))]
		public partial bool HasUnsavedChanges { get; set; } = false;

		[ObservableProperty]
		public partial DesignerMode SelectedDesignerMode { get; set; } = DesignerMode.TileSet;

		[ObservableProperty]
		public partial PreviewMode SelectedPreviewMode { get; set; } = PreviewMode.Image;

		public string Title => FileName != null ? $"Inkscape Tile Maker - {FileName}" + (HasUnsavedChanges ? " *" : "") : "Inkscape Tile Maker";

		private SKBitmap? _renderedBitmap;
		private readonly ConcurrentDictionary<(int row, int col), SKBitmap> _tileBitmaps = [];

		public SvgConnectionService SvgConnectionService => _svgConnectionService;

		public event Action CanvasNeedsRedraw = delegate { };

		public DesignerViewModel(SvgConnectionService svgConnectionService, IWindowService windowService, ISvgRenderingService svgRenderingService, IFileSaver fileSaver)
		{
			_svgConnectionService = svgConnectionService;
			_svgConnectionService.DocumentLoaded += OnDocumentLoaded;
			_windowService = windowService;
			_svgRenderingService = svgRenderingService;
			_fileSaver = fileSaver;

			SelectedZoomLevel = 1.0m;
		}

		~DesignerViewModel()
		{
			_svgConnectionService.DocumentLoaded -= OnDocumentLoaded;
		}

		#region Value Changed Handlers

		partial void OnSelectedZoomLevelChanged(decimal value)
		{
			CanvasNeedsRedraw.Invoke();
		}

		partial void OnPreviewOffsetChanged(PointF value)
		{
			CanvasNeedsRedraw.Invoke();
		}

		partial void OnSelectedTileChanged(TileViewModel? value)
		{
			SelectedDesignerMode = value == null ? DesignerMode.TileSet : DesignerMode.SingleTile;
			CanvasNeedsRedraw.Invoke();
		}

		partial void OnHoveredTileChanged((int row, int col)? value)
		{
			CanvasNeedsRedraw.Invoke();
		}

		partial void OnSelectedDesignerModeChanged(DesignerMode value)
		{
			CanvasNeedsRedraw.Invoke();
		}

		partial void OnSelectedPreviewModeChanged(PreviewMode value)
		{
			CanvasNeedsRedraw.Invoke();
		}

		#endregion

		private void OnDocumentLoaded(XDocument obj)
		{
			_renderedBitmap?.Dispose();
			_renderedBitmap = null;

			foreach (var tileBitmap in _tileBitmaps.Values)
			{
				tileBitmap.Dispose();
			}
			_tileBitmaps.Clear();

			var svgFile = _svgConnectionService.SvgFile;
			FileName = svgFile?.Name;
			if (svgFile == null) return;

			TileSize = _svgConnectionService.TileSize ?? (0, 0);

			var newTiles = _svgConnectionService.GetAllTiles(this).ToArray();
			if (Tiles.Count > 0)
			{
				if (SelectedTile != null)
				{
					var matchingTile = newTiles.FirstOrDefault(t => t.Value.Row == SelectedTile.Value.Row && t.Value.Column == SelectedTile.Value.Column);
					SelectedTile = matchingTile;
				}
			}

			Tiles.Clear();
			foreach (var tile in newTiles)
			{
				Tiles.Add(tile);
			}

			Task.Run(async () =>
			{
				using (var stream = await _svgRenderingService.RenderSvgFile(svgFile, CancellationToken.None))
				{
					if (stream.CanSeek) stream.Position = 0;
					_renderedBitmap = SKBitmap.Decode(stream);
				}

				foreach (var tileWrapper in Tiles)
				{
					tileWrapper.PreviewImage = ImageSource.FromStream(token =>
					{
						return _svgRenderingService.RenderSvgSegment(
						svgFile,
						left: tileWrapper.Value.Column * _svgConnectionService.TileSize!.Value.width,
						top: tileWrapper.Value.Row * _svgConnectionService.TileSize!.Value.height,
						right: (tileWrapper.Value.Column + 1) * _svgConnectionService.TileSize!.Value.width,
						bottom: (tileWrapper.Value.Row + 1) * _svgConnectionService.TileSize!.Value.height,
						token);
					});
				}

				await MainThread.InvokeOnMainThreadAsync(CanvasNeedsRedraw);
			});
		}

		public void RenderCanvas(SKCanvas canvas, int width, int height) // note that this must run synchronously
		{
			DrawTransparentBackground(canvas, width, height);

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

			switch (SelectedPreviewMode)
			{
				case PreviewMode.Image:
					DrawImagePreview(canvas);
					break;
				case PreviewMode.InContext:
					DrawInContextPreview(canvas);
					break;
			}
		}

		#region Preview Canvas Methods

		private void DrawImagePreview(SKCanvas canvas)
		{
			if (_renderedBitmap == null) return;

			var previewRect = GetImageRect()!.Value;
			canvas.DrawBitmap(_renderedBitmap, previewRect);

			if (_svgConnectionService.Document != null)
			{
				var gridElement = _svgConnectionService.Grid;
				if (gridElement != null) DrawGrid(canvas, gridElement, previewRect);

				var svgElement = _svgConnectionService.SvgRoot;
				if (svgElement != null) DrawBorder(canvas, previewRect);
			}

			if (HoveredTile != null)
			{
				var tileRect = GetTileRect(HoveredTile.Value.row, HoveredTile.Value.col);
				DrawSelectionOutline(canvas, tileRect, SKColors.DarkGreen.WithAlpha(128));
				var hoveredTile = Tiles.FirstOrDefault(t => t.Value.Row == HoveredTile.Value.row && t.Value.Column == HoveredTile.Value.col);
				if (hoveredTile != null) DrawTileLabel(canvas, hoveredTile.Value.Name, tileRect);
			}

			if (SelectedTile != null)
			{
				var tileRect = GetTileRect(SelectedTile.Value.Row, SelectedTile.Value.Column);
				DrawSelectionOutline(canvas, tileRect, SKColors.Blue);
			}
		}

		private void DrawInContextPreview(SKCanvas canvas)
		{
			if (_renderedBitmap == null) return;
			if (SelectedTile == null) return;
			var previewRect = GetInContextRect()!.Value;

			if (SelectedTile.Type == TileType.Singular)
			{
				for (int row = 0; row < 8; row++)
				{
					for (int col = 0; col < 8; col++)
					{
						var tileRect = GetTileRect(row, col);
						DrawSingleTile(canvas, SelectedTile.Value, tileRect);
					}
				}
			}

			if (_svgConnectionService.Document != null)
			{
				var gridElement = _svgConnectionService.Grid;
				if (gridElement != null) DrawGrid(canvas, gridElement, previewRect);

				var svgElement = _svgConnectionService.SvgRoot;
				if (svgElement != null) DrawBorder(canvas, previewRect);
			}
		}

		#endregion

		#region Drawing Methods

		public SKRect? GetImageRect()
		{
			if (_renderedBitmap == null) return null;

			var zoomFactor = (float)SelectedZoomLevel;
			return new SKRect()
			{
				Top = PreviewOffset.Y * zoomFactor,
				Left = PreviewOffset.X * zoomFactor,
				Size = new SKSize(
					_renderedBitmap.Width * zoomFactor,
					_renderedBitmap.Height * zoomFactor),
			};
		}

		public SKRect? GetInContextRect()
		{
			const int IN_CONTEXT_SIZE = 8;
			if (TileSize == (0, 0)) return null;
			var zoomFactor = (float)SelectedZoomLevel;
			return new SKRect()
			{
				Top = PreviewOffset.Y * zoomFactor,
				Left = PreviewOffset.X * zoomFactor,
				Size = new SKSize(
					IN_CONTEXT_SIZE * TileSize.x * zoomFactor,
					IN_CONTEXT_SIZE * TileSize.y * zoomFactor),
			};
		}

		public SKRect GetTileRect(int row, int column)
		{
			if (_svgConnectionService.TileSize == null) return SKRect.Empty;
			float width = _svgConnectionService.TileSize!.Value.width * (float)SelectedZoomLevel;
			float height = _svgConnectionService.TileSize!.Value.height * (float)SelectedZoomLevel;
			float top = height * row + PreviewOffset.Y * (float)SelectedZoomLevel;
			float left = width * column + PreviewOffset.X * (float)SelectedZoomLevel;
			float right = left + width;
			float bottom = top + height;

			return new SKRect(left, top, right, bottom);
		}

		public SKRectI GetUnscaledTileRect(int row, int column)
		{
			if (_svgConnectionService.TileSize == null) return SKRectI.Empty;
			int width = _svgConnectionService.TileSize!.Value.width;
			int height = _svgConnectionService.TileSize!.Value.height;
			int top = height * row;
			int left = width * column;
			int right = left + width;
			int bottom = top + height;
			return new SKRectI(left, top, right, bottom);
		}

		private void DrawTransparentBackground(SKCanvas canvas, int width, int height)
		{
			const int tileSize = 20;
			using var lightPaint = new SKPaint { Color = new SKColor(0xEE, 0xEE, 0xEE) };
			using var darkPaint = new SKPaint { Color = new SKColor(0xCC, 0xCC, 0xCC) };

			for (var y = -tileSize; y < height + tileSize; y += tileSize)
			{
				for (var x = -tileSize; x < width + tileSize; x += tileSize)
				{
					var useDark = (((int)Math.Floor(x / (float)tileSize)) +
								   ((int)Math.Floor(y / (float)tileSize))) % 2 == 0;

					var rect = new SKRect(x, y, x + tileSize, y + tileSize);
					canvas.DrawRect(rect, useDark ? darkPaint : lightPaint);
				}
			}
		}

		private void DrawGrid(SKCanvas canvas, XElement gridElement, SKRect rect)
		{
			if (!((bool?)gridElement.Attribute("enabled") ?? true)) return;
			if (gridElement.Attribute("units")?.Value != "px") return;

			float spacingX = Convert.ToSingle(gridElement.Attribute("spacingx")?.Value);
			float spacingY = Convert.ToSingle(gridElement.Attribute("spacingy")?.Value);
			int empSpacing = Convert.ToInt32(gridElement.Attribute("empspacing")?.Value);
			if (spacingX <= 0 || spacingY <= 0 || empSpacing <= 0 || SelectedZoomLevel <= 0) return;
			spacingX *= (float)SelectedZoomLevel;
			spacingY *= (float)SelectedZoomLevel;

			var color = SKColor.Parse(gridElement.Attribute("color")?.Value ?? "#0099e5");
			var empColor = SKColor.Parse(gridElement.Attribute("empcolor")?.Value ?? "#e500a7");
			var opacity = Convert.ToSingle(gridElement.Attribute("opacity")?.Value ?? "0.2");
			var empOpacity = Convert.ToSingle(gridElement.Attribute("empopacity")?.Value ?? "0.5");

			using var normalPaint = new SKPaint
			{
				Color = color.WithAlpha((byte)(opacity * 255)),
				StrokeWidth = 1,
				IsAntialias = false,
				Style = SKPaintStyle.Stroke,
			};

			using var empPaint = new SKPaint
			{
				Color = empColor.WithAlpha((byte)(empOpacity * 255)),
				StrokeWidth = 1,
				IsAntialias = false,
				Style = SKPaintStyle.Stroke
			};

			// Vertical lines
			int verticalIndex = 0;
			for (float x = rect.Left; x <= rect.Right; x += spacingX)
			{
				var paint = (verticalIndex % empSpacing == 0) ? empPaint : normalPaint;
				canvas.DrawLine(x, rect.Top, x, rect.Bottom, paint);
				verticalIndex++;
			}

			// Horizontal lines
			int horizontalIndex = 0;
			for (float y = rect.Top; y <= rect.Bottom; y += spacingY)
			{
				var paint = (horizontalIndex % empSpacing == 0) ? empPaint : normalPaint;
				canvas.DrawLine(rect.Left, y, rect.Right, y, paint);
				horizontalIndex++;
			}
		}

		private void DrawBorder(SKCanvas canvas, SKRect interiorRect)
		{
			var svgElement = _svgConnectionService.SvgRoot;
			if (svgElement == null) return;
			if (interiorRect.Width <= 0 || interiorRect.Height <= 0 || SelectedZoomLevel <= 0)
			{
				return;
			}

			var namedViewElement = svgElement.Element(XName.Get("namedview", "sodipodi"));
			SKColor borderColor = SKColor.Parse(namedViewElement?.Attribute("bordercolor")?.Value ?? "#ffffff");

			// Canvas bounds
			int canvasWidth = canvas.DeviceClipBounds.Width;
			int canvasHeight = canvas.DeviceClipBounds.Height;

			using var borderPaint = new SKPaint
			{
				Color = borderColor,
				Style = SKPaintStyle.Fill,
				IsAntialias = false
			};

			// Left area (to the left of the box, including top-left corner if in view)
			if (interiorRect.Left > 0)
			{
				var leftRect = new SKRect(0, 0, Math.Min(interiorRect.Left, canvasWidth), canvasHeight);
				if (!leftRect.IsEmpty)
				{
					canvas.DrawRect(leftRect, borderPaint);
				}
			}

			// Right area (to the right of the box)
			if (interiorRect.Right < canvasWidth)
			{
				var rightRect = new SKRect(Math.Max(interiorRect.Right, 0), 0, canvasWidth, canvasHeight);
				if (!rightRect.IsEmpty)
				{
					canvas.DrawRect(rightRect, borderPaint);
				}
			}

			// Top area (above the box)
			if (interiorRect.Top > 0)
			{
				var topRect = new SKRect(
					Math.Max(interiorRect.Left, 0),
					0,
					Math.Min(interiorRect.Right, canvasWidth),
					Math.Min(interiorRect.Top, canvasHeight));
				if (!topRect.IsEmpty)
				{
					canvas.DrawRect(topRect, borderPaint);
				}
			}

			// Bottom area (below the box)
			if (interiorRect.Bottom < canvasHeight)
			{
				var bottomRect = new SKRect(
					Math.Max(interiorRect.Left, 0),
					Math.Max(interiorRect.Bottom, 0),
					Math.Min(interiorRect.Right, canvasWidth),
					canvasHeight);
				if (!bottomRect.IsEmpty)
				{
					canvas.DrawRect(bottomRect, borderPaint);
				}
			}
		}

		private void DrawSelectionOutline(SKCanvas canvas, SKRect rect, SKColor color)
		{
			using var outlinePaint = new SKPaint
			{
				Color = color,
				Style = SKPaintStyle.Stroke,
				StrokeWidth = 2,
				IsAntialias = true
			};
			canvas.DrawRect(rect, outlinePaint);
		}

		private void DrawTileLabel(SKCanvas canvas, string label, SKRect rect)
		{
			using var textPaint = new SKPaint
			{
				Color = SKColors.Black,
				IsAntialias = true,
			};
			using var font = SKTypeface.FromFamilyName("Arial");
			using var skFont = new SKFont(font, 16);
			var x = rect.MidX;
			var y = rect.MidY;
			canvas.DrawText(label, x, y, SKTextAlign.Center, skFont, textPaint);
		}

		private void DrawSingleTile(SKCanvas canvas, Tile tile, SKRect rect)
		{
			if (_renderedBitmap == null) return;
			using var tileBitmap = new SKBitmap(TileSize.x, TileSize.y);
			if (!_renderedBitmap.ExtractSubset(tileBitmap, GetUnscaledTileRect(tile.Row, tile.Column))) return;
			canvas.DrawBitmap(tileBitmap, rect);
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
			PreSave();
			var svgFile = _svgConnectionService.SvgFile;
			if (svgFile == null) return;
			_svgConnectionService.SaveSvg(svgFile);
			HasUnsavedChanges = false;
		}

		[RelayCommand]
		public async Task SaveDesignAs()
		{
			PreSave();
			var svgFile = _svgConnectionService.SvgFile;
			if (svgFile == null) return;

			using (var ms = new MemoryStream())
			{
				await _svgConnectionService.Document!.SaveAsync(ms, SaveOptions.None, CancellationToken.None);
				ms.Position = 0;
				var result = await _fileSaver.SaveAsync(svgFile.FullName, svgFile.Name, ms);
			}
		}

		public void PreSave()
		{
			foreach (var tileWrapper in Tiles)
			{
				tileWrapper.Sync();
			}
		}

		[RelayCommand]
		public void ResetView()
		{
			PreviewOffset = new PointF(0, 0);
		}

		public void SelectTileFromPreviewAt(int row, int column)
		{
			switch (SelectedPreviewMode)
			{
				case PreviewMode.Image:
					var tile = Tiles.FirstOrDefault(t => t.Value.Row == row && t.Value.Column == column);
					SelectedTile = tile;
					break;
			}
		}

		[RelayCommand]
		public void SelectTile(TileViewModel tvm)
		{
			SelectedTile = tvm;
		}

		[RelayCommand]
		public void AddNewTile((int row, int col) position)
		{
			_svgConnectionService.AddTile(new Tile { Row = position.row, Column = position.col });
			HasUnsavedChanges = true;
		}

		[RelayCommand]
		public void FillTiles()
		{
			_svgConnectionService.FillTiles();
			HasUnsavedChanges = true;
		}

		[RelayCommand]
		public void ReturnToTileSet()
		{
			if (SelectedTile == null) return;
			SelectedTile = null;
		}

		#endregion
	}

	public enum DesignerMode
	{
		TileSet,
		SingleTile
	}

	public enum PreviewMode
	{
		Image,
		InContext,
	}
}
