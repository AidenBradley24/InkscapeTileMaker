using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InkscapeTileMaker.Models;
using InkscapeTileMaker.Services;
using InkscapeTileMaker.Utility;
using Microsoft.Maui.Graphics;
using SkiaSharp;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.IO.Compression;

namespace InkscapeTileMaker.ViewModels
{
	public partial class DesignerViewModel : ObservableObject
	{
		private ITilesetConnection? _tilesetConnection;
		private readonly IWindowOpeningService _windowService;
		private readonly ITilesetRenderingService _svgRenderingService;
		private readonly IFileSaver _fileSaver;

		private IWindowProvider? _windowProvider;

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
		[NotifyPropertyChangedFor(nameof(SelectedDesignerMode))]
		public partial TileViewModel? SelectedTile { get; set; }

		[ObservableProperty]
		public partial Scale TileSize { get; set; }

		[ObservableProperty]
		public partial ObservableCollection<TileViewModel> Tiles { get; set; } = [];

		[ObservableProperty]
		public partial (int row, int col)? HoveredTile { get; set; }

		[ObservableProperty]
		public partial (float x, float y) HoveredTileOffset { get; set; }

		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(Title))]
		public partial bool HasUnsavedChanges { get; set; } = false;

		public DesignerMode SelectedDesignerMode
		{
			get
			{
				if (SelectedPreviewMode == PreviewMode.Paint) return DesignerMode.Paint;
				return SelectedTile == null ? DesignerMode.TileSet : DesignerMode.SingleTile;
			}
		}

		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(SelectedDesignerMode))]
		public partial PreviewMode SelectedPreviewMode { get; set; } = PreviewMode.Image;

		[ObservableProperty]
		public partial TilemapViewModel PreviewTilemap { get; set; }
		private readonly TilemapViewModel _inContextTilemap;
		private readonly TilemapViewModel _paintTilemap;

		[ObservableProperty]
		public partial PaintTool SelectedPaintTool { get; set; } = PaintTool.Cursor;

		public string Title => FileName != null ? $"Inkscape Tile Maker - {FileName}" + (HasUnsavedChanges ? " *" : "") : "Inkscape Tile Maker";

		private SKBitmap? _renderedBitmap;
		private readonly ConcurrentDictionary<(int row, int col), SKBitmap> _tileBitmaps = [];

		public event Action CanvasNeedsRedraw = delegate { };

		public event Action CloseRequested = delegate { };

		const int TILEMAP_SCALE = 12;

		public DesignerViewModel(IWindowOpeningService windowService, ITilesetRenderingService renderingService, IFileSaver fileSaver)
		{
			_windowService = windowService;
			_svgRenderingService = renderingService;
			_fileSaver = fileSaver;
			SelectedZoomLevel = 1.0m;

			_inContextTilemap = new TilemapViewModel(TILEMAP_SCALE, TILEMAP_SCALE);
			_paintTilemap = new TilemapViewModel(TILEMAP_SCALE, TILEMAP_SCALE);
			PreviewTilemap = _inContextTilemap;

			_paintTilemap.NeedsRedraw += () => CanvasNeedsRedraw.Invoke();
		}

		public void RegisterWindow(IWindowProvider windowProvider)
		{
			_windowProvider = windowProvider;
		}

		public void SetTilesetConnection(ITilesetConnection connection)
		{
			if (_tilesetConnection != null)
			{
				_tilesetConnection.TilesetChanged -= OnTilesetChanged;
			}

			_tilesetConnection = connection;
			_tilesetConnection.TilesetChanged += OnTilesetChanged;
			if (_tilesetConnection.Tileset != null)
			{
				OnTilesetChanged(_tilesetConnection.Tileset);
			}
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
			CanvasNeedsRedraw.Invoke();
		}

		partial void OnHoveredTileChanged((int row, int col)? value)
		{
			CanvasNeedsRedraw.Invoke();
		}

		partial void OnSelectedPreviewModeChanged(PreviewMode value)
		{
			PreviewTilemap = value == PreviewMode.InContext ? _inContextTilemap : _paintTilemap;
			CanvasNeedsRedraw.Invoke();
		}

		partial void OnSelectedPreviewModeChanging(PreviewMode value)
		{
			if (value == PreviewMode.Paint)
			{
				SelectedPaintTool = PaintTool.Cursor;
			}
			else
			{
				HoveredTileOffset = (0f, 0f);
			}

			CanvasNeedsRedraw.Invoke();
		}

		partial void OnSelectedPaintToolChanged(PaintTool value)
		{
			if (value == PaintTool.Cursor)
				HoveredTileOffset = (0f, 0f);
			else
				HoveredTileOffset = (-0.5f, -0.5f);
			CanvasNeedsRedraw.Invoke();
		}

		#endregion

		private void OnTilesetChanged(ITileset tileset)
		{
			MainThread.BeginInvokeOnMainThread(() =>
			{
				RecalculateTileset(tileset);
			});
		}

		private void RecalculateTileset(ITileset tileset)
		{
			_renderedBitmap?.Dispose();
			_renderedBitmap = null;

			foreach (var tileBitmap in _tileBitmaps.Values)
			{
				tileBitmap.Dispose();
			}
			_tileBitmaps.Clear();

			var file = _tilesetConnection!.CurrentFile;
			FileName = file?.Name;
			if (file == null) return;

			TileSize = tileset.TileSize;

			var newTiles = tileset.GetAllTileViewModels(this);
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
				using (var stream = await _svgRenderingService.RenderFileAsync(file, ".png"))
				{
					if (stream.CanSeek) stream.Position = 0;
					_renderedBitmap = SKBitmap.Decode(stream);
				}

				foreach (var tileWrapper in Tiles)
				{
					tileWrapper.PreviewImage = ImageSource.FromStream(token =>
					{
						return _svgRenderingService.RenderSegmentAsync(
						file,
						".png",
						left: tileWrapper.Value.Column * tileset.TileSize.width,
						top: tileWrapper.Value.Row * tileset.TileSize.height,
						right: (tileWrapper.Value.Column + 1) * tileset.TileSize.width,
						bottom: (tileWrapper.Value.Row + 1) * tileset.TileSize.height,
						token);
					});
				}

				await MainThread.InvokeOnMainThreadAsync(CanvasNeedsRedraw);
			});
		}

		public void RenderCanvas(SKCanvas canvas, int width, int height) // note that this must run synchronously
		{
			DrawTransparentBackground(canvas, width, height);

			if (_tilesetConnection?.CurrentFile == null)
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
				case PreviewMode.Paint:
					DrawPaintPreview(canvas);
					break;
			}
		}

		#region Preview Canvas Methods

		private void DrawImagePreview(SKCanvas canvas)
		{
			if (_renderedBitmap == null) return;

			var previewRect = GetImageRect()!.Value;
			canvas.DrawBitmap(_renderedBitmap, previewRect);

			if (_tilesetConnection?.Tileset != null)
			{
				using var majorPaint = new SKPaint
				{
					Color = SKColors.Black.WithAlpha(128),
					StrokeWidth = 2,
					IsAntialias = false,
					Style = SKPaintStyle.Stroke,
				};

				using var minorPaint = new SKPaint
				{
					Color = SKColors.Gray.WithAlpha(64),
					StrokeWidth = 1,
					IsAntialias = false,
					Style = SKPaintStyle.Stroke,
					PathEffect = SKPathEffect.CreateDash([10, 10], 0),
				};

				DrawGrid(canvas, previewRect, _tilesetConnection.Tileset.TileSize / 2, 2, majorPaint, minorPaint);
				DrawBorder(canvas, previewRect);
			}

			if (HoveredTile != null)
			{
				var tileRect = GetTileRect(HoveredTile.Value.row, HoveredTile.Value.col);
				var hoveredTileModel = Tiles.FirstOrDefault(t => t.Value.Row == HoveredTile.Value.row && t.Value.Column == HoveredTile.Value.col);
				if (hoveredTileModel == null)
				{
					DrawTileX(canvas, tileRect, SKColors.Red.WithAlpha(128));
					DrawTileOutline(canvas, tileRect, SKColors.Red.WithAlpha(128));
				}
				else
				{
					DrawTileOutline(canvas, tileRect, SKColors.DarkGreen.WithAlpha(128));
					DrawTileLabel(canvas, hoveredTileModel.Value.Name, tileRect);
				}
			}

			if (SelectedTile != null)
			{
				var tileRect = GetTileRect(SelectedTile.Value.Row, SelectedTile.Value.Column);
				DrawTileOutline(canvas, tileRect, SKColors.Blue);
			}
		}

		private void DrawInContextPreview(SKCanvas canvas)
		{
			if (_renderedBitmap == null) return;
			if (SelectedTile == null) return;

			var previewRect = GetRectAtScale(new Scale(_inContextTilemap.Width, _inContextTilemap.Height))!.Value;

			SKPaint? majorPaint = null;
			SKPaint? minorPaint = null;

			if (SelectedTile.Type == TileType.Singular)
			{
				for (int row = 0; row < _inContextTilemap.Height; row++)
				{
					for (int col = 0; col < _inContextTilemap.Width; col++)
					{
						var tileRect = GetTileRect(row, col);
						DrawSingleTile(canvas, new TileData() { tile = SelectedTile.Value }, tileRect);
					}
				}

				majorPaint = new SKPaint
				{
					Color = SKColors.Black.WithAlpha(128),
					StrokeWidth = 2,
					IsAntialias = false,
					Style = SKPaintStyle.Stroke,
				};

				minorPaint = new SKPaint
				{
					Color = SKColors.Gray.WithAlpha(64),
					StrokeWidth = 1,
					IsAntialias = false,
					Style = SKPaintStyle.Stroke,
					PathEffect = SKPathEffect.CreateDash([10, 10], 0),
				};
			}
			else if (SelectedTile.IsMaterial)
			{
				var material = new Material(SelectedTile.Value.MaterialName, () => Tiles.Select(t => t.Value));
				_inContextTilemap.Clear();
				_inContextTilemap.AddSampleMaterial(material);
				DrawMaterialTilemap(canvas, _inContextTilemap.Tilemap);

				majorPaint = new SKPaint
				{
					Color = SKColors.Magenta.WithAlpha(64),
					StrokeWidth = 2,
					IsAntialias = false,
					Style = SKPaintStyle.Stroke,
					PathEffect = SKPathEffect.CreateDash([10, 10], 0),
				};

				minorPaint = new SKPaint
				{
					Color = SKColors.LimeGreen.WithAlpha(128),
					StrokeWidth = 1,
					IsAntialias = false,
					Style = SKPaintStyle.Stroke,
				};

				if (HoveredTile != null)
				{
					var tileRect = GetTileRect(HoveredTile.Value.row, HoveredTile.Value.col);
					var tiles = _inContextTilemap.Tilemap.GetTilesOnDuelGrid(HoveredTile.Value.col, HoveredTile.Value.row);
					var tile = tiles != null && tiles.Count > 0 ? tiles[0].tile : null;
					if (tile == null)
					{
						DrawTileX(canvas, tileRect, SKColors.Red.WithAlpha(128));
						DrawTileOutline(canvas, tileRect, SKColors.Red.WithAlpha(128));
					}
					else
					{
						DrawTileOutline(canvas, tileRect, SKColors.DarkGreen.WithAlpha(128));
						DrawTileLabel(canvas, tile.Name, tileRect);
					}
				}
			}

			if (_tilesetConnection?.Tileset != null && majorPaint != null && minorPaint != null)
			{
				DrawGrid(canvas, previewRect, _tilesetConnection.Tileset.TileSize / 2, 2, majorPaint, minorPaint);
				DrawBorder(canvas, previewRect);

				majorPaint.Dispose();
				minorPaint.Dispose();
			}
		}

		private void DrawPaintPreview(SKCanvas canvas)
		{
			if (_renderedBitmap == null) return;

			var previewRect = GetRectAtScale(new Scale(_paintTilemap.Width, _paintTilemap.Height))!.Value;

			DrawMaterialTilemap(canvas, _paintTilemap.Tilemap);

			if (_tilesetConnection?.Tileset != null)
			{
				using var majorPaint = new SKPaint
				{
					Color = SKColors.Magenta.WithAlpha(64),
					StrokeWidth = 2,
					IsAntialias = false,
					Style = SKPaintStyle.Stroke,
					PathEffect = SKPathEffect.CreateDash([10, 10], 0),
				};

				using var minorPaint = new SKPaint
				{
					Color = SKColors.LimeGreen.WithAlpha(128),
					StrokeWidth = 1,
					IsAntialias = false,
					Style = SKPaintStyle.Stroke,
				};

				DrawGrid(canvas, previewRect, _tilesetConnection.Tileset.TileSize / 2, 2, majorPaint, minorPaint);
				previewRect.Bottom -= _tilesetConnection.Tileset.TileSize.height * (float)SelectedZoomLevel;
				previewRect.Right -= _tilesetConnection.Tileset.TileSize.width * (float)SelectedZoomLevel;
				DrawBorder(canvas, previewRect);
			}

			if (HoveredTile != null)
			{
				var tileRect = GetTileRect(HoveredTile.Value.row, HoveredTile.Value.col);
				switch (SelectedPaintTool)
				{
					case PaintTool.Cursor:
						{
							var tiles = _paintTilemap.Tilemap.GetTilesOnDuelGrid(HoveredTile.Value.col, HoveredTile.Value.row);
							var tile = tiles != null && tiles.Count > 0 ? tiles[0].tile : null;
							if (tile == null)
							{
								DrawTileX(canvas, tileRect, SKColors.Red.WithAlpha(128));
								DrawTileOutline(canvas, tileRect, SKColors.Red.WithAlpha(128));
							}
							else
							{
								DrawTileOutline(canvas, tileRect, SKColors.DarkGreen.WithAlpha(128));
								DrawTileLabel(canvas, tile.Name, tileRect);
							}
						}
						break;
					case PaintTool.Paint:
						{
							tileRect.Offset(HoveredTileOffset.x * tileRect.Width, HoveredTileOffset.y * tileRect.Height);
							DrawTileOutline(canvas, tileRect, SKColors.Green.WithAlpha(128));
						}
						break;
					case PaintTool.Eraser:
						{
							tileRect.Offset(HoveredTileOffset.x * tileRect.Width, HoveredTileOffset.y * tileRect.Height);
							DrawTileOutline(canvas, tileRect, SKColors.Red.WithAlpha(128));
						}
						break;
				}

			}
		}

		#endregion

		#region Drawing Methods

		public SKRect? GetPreviewRect()
		{
			return SelectedPreviewMode switch
			{
				PreviewMode.Image => GetImageRect(),
				PreviewMode.InContext => GetRectAtScale(new Scale(TILEMAP_SCALE, TILEMAP_SCALE)),
				PreviewMode.Paint => GetRectAtScale(new Scale(TILEMAP_SCALE, TILEMAP_SCALE)),
				_ => null,
			};
		}

		public SKRectI? GetUnscaledPreviewRect()
		{
			return SelectedPreviewMode switch
			{
				PreviewMode.Image => GetUnscaledImageRect(),
				PreviewMode.InContext => new SKRectI(0, 0, TileSize.width * 8, TileSize.height * 8),
				PreviewMode.Paint => new SKRectI(0, 0, TileSize.width * 8, TileSize.height * 8),
				_ => null,
			};
		}

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

		public SKRectI? GetUnscaledImageRect()
		{
			if (_renderedBitmap == null) return null;
			return new SKRectI(0, 0, _renderedBitmap.Width, _renderedBitmap.Height);
		}

		public SKRect? GetRectAtScale(Scale scale)
		{
			if (_tilesetConnection?.Tileset == null) return SKRect.Empty;
			var zoomFactor = (float)SelectedZoomLevel;
			return new SKRect()
			{
				Top = PreviewOffset.Y * zoomFactor,
				Left = PreviewOffset.X * zoomFactor,
				Size = new SKSize(
					scale.width * _tilesetConnection.Tileset.TileSize.width * zoomFactor,
					scale.height * _tilesetConnection.Tileset.TileSize.height * zoomFactor),
			};
		}

		public SKRect GetTileRect(int row, int column)
		{
			if (_tilesetConnection?.Tileset == null) return SKRect.Empty;
			float width = _tilesetConnection.Tileset.TileSize.width * (float)SelectedZoomLevel;
			float height = _tilesetConnection.Tileset.TileSize.height * (float)SelectedZoomLevel;
			float top = height * row + PreviewOffset.Y * (float)SelectedZoomLevel;
			float left = width * column + PreviewOffset.X * (float)SelectedZoomLevel;
			float right = left + width;
			float bottom = top + height;

			return new SKRect(left, top, right, bottom);
		}

		public SKRectI GetUnscaledTileRect(int row, int column)
		{
			if (_tilesetConnection?.Tileset == null) return SKRectI.Empty;
			int width = _tilesetConnection.Tileset.TileSize.width;
			int height = _tilesetConnection.Tileset.TileSize.height;
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

		private void DrawGrid(SKCanvas canvas, SKRect rect, Scale spacing, int majorSpacing, SKPaint majorPaint, SKPaint minorPaint)
		{
			if (spacing.width <= 0 || spacing.height <= 0 || SelectedZoomLevel <= 0) return;

			float sx = spacing.width * (float)SelectedZoomLevel;
			float sy = spacing.height * (float)SelectedZoomLevel;

			// Vertical lines
			int verticalIndex = 0;
			for (float x = rect.Left; x <= rect.Right; x += sx)
			{
				var paint = (verticalIndex % majorSpacing == 0) ? majorPaint : minorPaint;
				canvas.DrawLine(x, rect.Top, x, rect.Bottom, paint);
				verticalIndex++;
			}

			// Horizontal lines
			int horizontalIndex = 0;
			for (float y = rect.Top; y <= rect.Bottom; y += sy)
			{
				var paint = (horizontalIndex % majorSpacing == 0) ? majorPaint : minorPaint;
				canvas.DrawLine(rect.Left, y, rect.Right, y, paint);
				horizontalIndex++;
			}
		}

		private void DrawBorder(SKCanvas canvas, SKRect interiorRect)
		{
			SKColor borderColor = SKColor.Parse("#222222").WithAlpha(200);

			// Canvas bounds
			int canvasWidth = canvas.DeviceClipBounds.Width;
			int canvasHeight = canvas.DeviceClipBounds.Height;

			using var borderPaint = new SKPaint
			{
				Color = borderColor,
				Style = SKPaintStyle.Fill,
				IsAntialias = false,
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

			// lines from each rect corner to each canvas corner
			using var linePaint = new SKPaint
			{
				Color = borderColor,
				Style = SKPaintStyle.Stroke,
				StrokeWidth = 1,
				IsAntialias = false,
			};

			var topLeftCanvas = new SKPoint(0, 0);
			var topRightCanvas = new SKPoint(canvasWidth, 0);
			var bottomLeftCanvas = new SKPoint(0, canvasHeight);
			var bottomRightCanvas = new SKPoint(canvasWidth, canvasHeight);

			var topLeftRect = new SKPoint(interiorRect.Left, interiorRect.Top);
			var topRightRect = new SKPoint(interiorRect.Right, interiorRect.Top);
			var bottomLeftRect = new SKPoint(interiorRect.Left, interiorRect.Bottom);
			var bottomRightRect = new SKPoint(interiorRect.Right, interiorRect.Bottom);

			canvas.DrawLine(topLeftRect, topLeftCanvas, linePaint);
			canvas.DrawLine(topRightRect, topRightCanvas, linePaint);
			canvas.DrawLine(bottomLeftRect, bottomLeftCanvas, linePaint);
			canvas.DrawLine(bottomRightRect, bottomRightCanvas, linePaint);
		}

		private void DrawTileOutline(SKCanvas canvas, SKRect rect, SKColor color)
		{
			using var paint = new SKPaint
			{
				Color = color,
				Style = SKPaintStyle.Stroke,
				StrokeWidth = 2,
				IsAntialias = false
			};
			canvas.DrawRect(rect, paint);
		}

		private void DrawTileX(SKCanvas canvas, SKRect rect, SKColor color)
		{
			using var paint = new SKPaint
			{
				Color = color,
				Style = SKPaintStyle.Stroke,
				StrokeWidth = 2,
				IsAntialias = true
			};
			canvas.DrawLine(rect.Left, rect.Top, rect.Right, rect.Bottom, paint);
			canvas.DrawLine(rect.Right, rect.Top, rect.Left, rect.Bottom, paint);
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

		private void DrawSingleTile(SKCanvas canvas, TileData tileData, SKRect rect)
		{
			if (_renderedBitmap == null) return;
			using var tileBitmap = new SKBitmap(TileSize.width, TileSize.height);
			if (!_renderedBitmap.ExtractSubset(tileBitmap, GetUnscaledTileRect(tileData.tile.Row, tileData.tile.Column))) return;
			using var transformedBitmap = new SKBitmap(TileSize.width, TileSize.height);
			using (var tileCanvas = new SKCanvas(transformedBitmap))
			{
				tileCanvas.Clear(SKColors.Transparent);

				float cx = TileSize.width / 2f;
				float cy = TileSize.height / 2f;

				var matrix = SKMatrix.Identity;
				matrix = matrix.PostConcat(SKMatrix.CreateTranslation(-cx, -cy));
				matrix = matrix.PostConcat(tileData.transformation.ToSKMatrix());
				matrix = matrix.PostConcat(SKMatrix.CreateTranslation(cx, cy));

				tileCanvas.SetMatrix(matrix);
				tileCanvas.DrawBitmap(tileBitmap, 0, 0);
			}
			canvas.DrawBitmap(transformedBitmap, rect);
		}

		private void DrawMaterialTilemap(SKCanvas canvas, MaterialTilemap tilemap)
		{
			for (int row = 0; row < tilemap.Height; row++)
			{
				for (int col = 0; col < tilemap.Width; col++)
				{
					var tileDataList = tilemap.GetTilesOnDuelGrid(col, row);
					if (tileDataList.Count == 0) continue;
					SKRect tileRect = GetTileRect(row, col);
					tileRect.Offset(0.5f, 0.5f);
					foreach (var tileData in tileDataList)
					{
						DrawSingleTile(canvas, tileData, tileRect);
					}
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
			if (_tilesetConnection?.Tileset == null) return;
			PreSave();
			var file = _tilesetConnection.CurrentFile;
			if (file == null) return;
			await _tilesetConnection.SaveAsync(file);
			HasUnsavedChanges = false;
		}

		[RelayCommand]
		public async Task SaveDesignAs()
		{
			if (_tilesetConnection?.Tileset == null) return;
			PreSave();
			var svgFile = _tilesetConnection.CurrentFile;
			if (svgFile == null) return;
			using var ms = new MemoryStream();
			await _tilesetConnection.SaveToStreamAsync(ms);
			ms.Position = 0;
			var result = await _fileSaver.SaveAsync(svgFile.FullName, svgFile.Name, ms);
		}

		public void PreSave()
		{
			var bakedTiles = Tiles.ToArray(); // prevent collection modified during enumeration
			foreach (var tileWrapper in bakedTiles)
			{
				tileWrapper.Sync();
			}
		}

		[RelayCommand]
		public async Task Exit()
		{
			CloseRequested.Invoke();
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
					SelectedTile = Tiles.FirstOrDefault(t => t.Value.Row == row && t.Value.Column == column);
					break;
				case PreviewMode.InContext:
					{
						var tileRect = GetTileRect(row, column);
						var tiles = _inContextTilemap.Tilemap.GetTilesOnDuelGrid(column, row);
						var tile = tiles != null && tiles.Count > 0 ? tiles[0].tile : null;
						if (tile == null) return;
						SelectedTile = Tiles.FirstOrDefault(t => t.Value.Row == tile.Row && t.Value.Column == tile.Column);
					}
					break;
				case PreviewMode.Paint:
					{
						switch (SelectedPaintTool)
						{
							case PaintTool.Cursor:
								{
									var tiles = _paintTilemap.Tilemap.GetTilesOnDuelGrid(column, row);
									var tile = tiles != null && tiles.Count > 0 ? tiles[0].tile : null;
									if (tile == null) return;
									SelectedTile = Tiles.FirstOrDefault(t => t.Value.Row == tile.Row && t.Value.Column == tile.Column);
								}
								break;
							case PaintTool.Paint:
								{
									if (SelectedTile == null) return;
									var material = new Material(SelectedTile.Value.MaterialName, () => Tiles.Select(t => t.Value));
									_paintTilemap.Tilemap[column, row] = material;
									CanvasNeedsRedraw.Invoke();
								}
								break;
							case PaintTool.Eraser:
								{
									_paintTilemap.Tilemap[column, row] = null;
									CanvasNeedsRedraw.Invoke();
								}
								break;
						}
					}
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
			if (_tilesetConnection?.Tileset == null) return;
			_tilesetConnection.Tileset.Add(new Tile { Row = position.row, Column = position.col });
			HasUnsavedChanges = true;
		}

		[RelayCommand]
		public async Task DeleteSelectedTile()
		{
			if (_tilesetConnection?.Tileset == null) return;
			if (SelectedTile == null) return;

			if (_windowProvider != null)
			{
				var confirm = await _windowProvider.PopupService.ShowConfirmationAsync(
					"Delete Tile",
					$"Are you sure you want to delete tile '{SelectedTile.Name}' at ({SelectedTile.Value.Row}, {SelectedTile.Value.Column})?",
					"Delete");
				if (!confirm) return;
			}

			_tilesetConnection.Tileset.Remove(SelectedTile.Value);
			SelectedTile = null;
			HasUnsavedChanges = true;
		}

		[RelayCommand]
		public async Task FillTiles()
		{
			if (_tilesetConnection?.Tileset == null) return;
			TilesetFillSettings settings = TilesetFillSettings.None;
			if (ReplaceExistingTiles) settings |= TilesetFillSettings.ReplaceExisting;
			if (FillEmptyTiles) settings |= TilesetFillSettings.FillEmptyTiles;

			if (_windowProvider == null)
			{
				await _tilesetConnection.Tileset.FillTilesAsync(settings);
			}
			else await _windowProvider.PopupService.ShowProgressOnTaskAsync(message: "Filling Tiles...", isIndeterminate: false, async progress =>
			{
				await _tilesetConnection.Tileset.FillTilesAsync(settings, progress);
			});

			HasUnsavedChanges = true;
		}

		[ObservableProperty] public partial bool ReplaceExistingTiles { get; set; } = false;
		[ObservableProperty] public partial bool FillEmptyTiles { get; set; } = false;

		[RelayCommand]
		public async Task ClearAllTiles()
		{
			if (_tilesetConnection?.Tileset == null) return;

			if (_windowProvider != null)
			{
				var confirm = await _windowProvider.PopupService.ShowConfirmationAsync(
					"Clear All Tiles",
					"Are you sure you want to clear all tiles from the tileset?",
					"Clear All");
				if (!confirm) return;
			}

			_tilesetConnection.Tileset.Clear();
			HasUnsavedChanges = true;
			if (_windowProvider == null) return;
			await _windowProvider.PopupService.ShowTextAsync("All tiles have been cleared.");
		}

		[RelayCommand]
		public void ReturnToTileSet()
		{
			if (SelectedTile == null) return;
			SelectedTile = null;
		}

		[RelayCommand]
		public void SelectTool(string tool)
		{
			switch (tool)
			{
				case "Cursor":
					SelectedPaintTool = PaintTool.Cursor;
					break;
				case "Paint":
					SelectedPaintTool = PaintTool.Paint;
					break;
				case "Eraser":
					SelectedPaintTool = PaintTool.Eraser;
					break;
			}
		}

		#endregion


		#region Exports
		[RelayCommand]
		public async Task ExportTilesetImage(string extension)
		{
			if (_tilesetConnection?.CurrentFile == null) return;

			if (_windowProvider == null)
			{
				using var stream = await _svgRenderingService.RenderFileAsync(_tilesetConnection.CurrentFile, extension);
				var result = await _fileSaver.SaveAsync($"{Path.GetFileNameWithoutExtension(_tilesetConnection.CurrentFile.Name)}.{extension}", stream);
			}
			else await _windowProvider.PopupService.ShowProgressOnTaskAsync("Exporting...", isIndeterminate: true, async _ =>
			{
				using var stream = await _svgRenderingService.RenderFileAsync(_tilesetConnection.CurrentFile, extension);
				var result = await _fileSaver.SaveAsync($"{Path.GetFileNameWithoutExtension(_tilesetConnection.CurrentFile.Name)}.{extension}", stream);
			});
		}

		[RelayCommand]
		public async Task ExportSelectedTileImage(string extension)
		{
			if (_tilesetConnection?.CurrentFile == null) return;
			if (SelectedTile == null) return;

			if (_windowProvider == null)
			{
				using var stream = await GetTileImageStream(SelectedTile.Value, extension);
				if (stream == null) return;
				string tileFileName = $"{SelectedTile.Name} [{Path.GetFileNameWithoutExtension(_tilesetConnection.CurrentFile.Name)}].{extension}";
				var result = await _fileSaver.SaveAsync(tileFileName, stream);
			}
			else await _windowProvider.PopupService.ShowProgressOnTaskAsync("Exporting...", isIndeterminate: true, async _ =>
			{
				using var stream = await GetTileImageStream(SelectedTile.Value, extension);
				if (stream == null) return;
				string tileFileName = $"{SelectedTile.Name} [{Path.GetFileNameWithoutExtension(_tilesetConnection.CurrentFile.Name)}].{extension}";
				var result = await _fileSaver.SaveAsync(tileFileName, stream);
			});
		}

		private async Task<Stream?> GetTileImageStream(Tile tile, string extension)
		{
			if (_tilesetConnection?.CurrentFile == null || _tilesetConnection.Tileset == null) return null;
			var tileset = _tilesetConnection.Tileset;

			return await _svgRenderingService.RenderSegmentAsync(
				_tilesetConnection.CurrentFile,
				extension,
				left: tile.Column * tileset.TileSize.width,
				top: tile.Row * tileset.TileSize.height,
				right: (tile.Column + 1) * tileset.TileSize.width,
				bottom: (tile.Row + 1) * tileset.TileSize.height, CancellationToken.None);
		}

		[RelayCommand]
		public async Task ExportSeparatedTilesPng()
		{
			if (_tilesetConnection?.CurrentFile == null) return;
			if (_tilesetConnection.Tileset == null) return;
			var tileset = _tilesetConnection.Tileset;

			using var zipStream = new MemoryStream();
			using (var zip = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
			{
				if (_windowProvider == null)
				{
					foreach (var tvm in Tiles)
					{
						using var stream = await _svgRenderingService.RenderSegmentAsync(
							_tilesetConnection.CurrentFile,
							".png",
							left: tvm.Value.Column * tileset.TileSize.width,
							top: tvm.Value.Row * tileset.TileSize.height,
							right: (tvm.Value.Column + 1) * tileset.TileSize.width,
							bottom: (tvm.Value.Row + 1) * tileset.TileSize.height,
							CancellationToken.None);
						string tileFileName = $"{tvm.Name} [{Path.GetFileNameWithoutExtension(_tilesetConnection.CurrentFile.Name)}].png";
						var entry = zip.CreateEntry(tileFileName);
						using var entryStream = await entry.OpenAsync();
						await stream.CopyToAsync(entryStream);
					}
				}
				else await _windowProvider.PopupService.ShowProgressOnTaskAsync("Exporting...", isIndeterminate: false, async progress =>
				{
					foreach (var (tvm, i) in Tiles.Select((t, i) => (t, i)))
					{
						progress.Report((double)i / Tiles.Count);
						using var stream = await _svgRenderingService.RenderSegmentAsync(
							_tilesetConnection.CurrentFile,
							".png",
							left: tvm.Value.Column * tileset.TileSize.width,
							top: tvm.Value.Row * tileset.TileSize.height,
							right: (tvm.Value.Column + 1) * tileset.TileSize.width,
							bottom: (tvm.Value.Row + 1) * tileset.TileSize.height,
							CancellationToken.None);
						string tileFileName = $"{tvm.Name} [{Path.GetFileNameWithoutExtension(_tilesetConnection.CurrentFile.Name)}].png";
						var entry = zip.CreateEntry(tileFileName);
						using var entryStream = await entry.OpenAsync();
						await stream.CopyToAsync(entryStream);
					}
				});
			}

			zipStream.Position = 0;
			var result = await _fileSaver.SaveAsync($"{Path.GetFileNameWithoutExtension(_tilesetConnection.CurrentFile.Name)}.zip", zipStream);
		}

		#endregion

	}

	public enum DesignerMode
	{
		TileSet,
		SingleTile,
		Paint
	}

	public enum PreviewMode
	{
		Image,
		InContext,
		Paint
	}

	public enum PaintTool
	{
		Cursor,
		Paint,
		Eraser
	}
}
