using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InkscapeTileMaker.Models;
using InkscapeTileMaker.Services;
using InkscapeTileMaker.Utility;
using InkscapeTileMaker.Utility.TilesetExporters;
using InkscapeTileMaker.Views;
using SkiaSharp;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.IO.Compression;
using System.Reflection;
using UnityPackageNET;

namespace InkscapeTileMaker.ViewModels
{
	public partial class DesignerViewModel : ObservableObject, IAsyncDisposable
	{
		private ITilesetConnection? _tilesetConnection;
		private readonly IWindowOpeningService _windowService;
		private readonly IFileSaver _fileSaver;
		private readonly IServiceProvider _serviceProvider;
		private readonly ISettingsService _settingsService;

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
		public partial bool ShowGridLines { get; set; } = true;

		[ObservableProperty]
		public partial double TileIconScale { get; set; } = 80;

		[ObservableProperty]
		public partial PointF PreviewOffset { get; set; }

		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(SelectedDesignerMode))]
		public partial TileViewModel? SelectedTile { get; set; }

		public Scale TilePixelSize => _cachedTilePixelSize;
		private Scale _cachedTilePixelSize;
		public Scale ImagePixelSize => _cachedImagePixelSize;
		private Scale _cachedImagePixelSize;
		public Scale TileSetSize => _cachedTileSetSize;
		private Scale _cachedTileSetSize;

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

		private CancellationTokenSource? _tilesetChangedDebouceCts;
		private static readonly TimeSpan TilesetChangedDebounceDelay = TimeSpan.FromMilliseconds(100);

		[ObservableProperty]
		public partial PaintTool SelectedPaintTool { get; set; } = PaintTool.Cursor;

		public string Title => FileName != null ? ((HasUnsavedChanges ? "*" : "") + $"{FileName} - Inkscape Tile Maker") : "Inkscape Tile Maker";

		private SKBitmap? _renderedBitmap;
		private readonly ConcurrentDictionary<(int row, int col), SKBitmap> _tileBitmaps = [];

		const int ACTIVE = 1, DISPOSAL = 2;
		private readonly CancellationTokenSource _disposeCts = new();
		private int _disposeState = ACTIVE;
		private int _activeOperationCount = 0;
		private readonly TaskCompletionSource<object?> _disposeCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
		private int _refreshVersion = 0;
		public event Action CanvasNeedsRedraw = delegate { };

		private void HandleTilesetChanged(ITilesetConnection _)
		{
			if (_disposeState == DISPOSAL) return;
			OnTilesetChanged();
		}

		const int TILEMAP_SCALE = 12;

		private bool _showRenderMessage;

		public DesignerViewModel(IServiceProvider serviceProvider,
			IWindowOpeningService windowService,
			IFileSaver fileSaver,
			ISettingsService settingsService)
		{
			_windowService = windowService;
			_fileSaver = fileSaver;
			_settingsService = settingsService;
			_serviceProvider = serviceProvider;

			_inContextTilemap = new TilemapViewModel(TILEMAP_SCALE, TILEMAP_SCALE);
			PreviewTilemap = _inContextTilemap;
			_paintTilemap = new TilemapViewModel(TILEMAP_SCALE, TILEMAP_SCALE);
			_paintTilemap.NeedsRedraw += () => CanvasNeedsRedraw.Invoke();

			SelectedZoomLevel = 1.0m;
			_showRenderMessage = true;
		}

		private void ThrowIfDisposed()
		{
			bool disposed = Volatile.Read(ref _disposeState) == DISPOSAL;
			ObjectDisposedException.ThrowIf(disposed, this);
		}

		private bool CheckIfDisposed()
		{
			return Volatile.Read(ref _disposeState) == DISPOSAL;
		}

		private void BeginOperation()
		{
			ThrowIfDisposed();
			Interlocked.Increment(ref _activeOperationCount);

			if (Volatile.Read(ref _disposeState) == DISPOSAL)
			{
				EndOperation();
				throw new ObjectDisposedException(nameof(SvgRenderingService));
			}
		}

		private void EndOperation()
		{
			if (Interlocked.Decrement(ref _activeOperationCount) == 0 &&
				Volatile.Read(ref _disposeState) == DISPOSAL)
			{
				_disposeCompletion.TrySetResult(null);
			}
		}

		public void RegisterWindow(IWindowProvider windowProvider)
		{
			_windowProvider = windowProvider;
		}

		public void SetTilesetConnection(ITilesetConnection connection)
		{
			if (_tilesetConnection != null)
			{
				_tilesetConnection.TilesetChanged -= HandleTilesetChanged;
				_tilesetConnection.Dispose();
			}

			_tilesetConnection = connection;
			_tilesetConnection.TilesetChanged += HandleTilesetChanged;
			if (_tilesetConnection.Tileset != null)
			{
				OnTilesetChanged();
			}
		}

		#region Value Changed Handlers

		partial void OnSelectedZoomLevelChanged(decimal value)
		{
			CanvasNeedsRedraw.Invoke();
		}

		partial void OnShowGridLinesChanged(bool value)
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
			OnSelectedPaintToolChanged(SelectedPaintTool);
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
			{
				HoveredTileOffset = (0f, 0f);
			}
			else if (SelectedTile == null || SelectedTile.Type != TileType.DualTileMaterial)
			{
				HoveredTileOffset = (0f, 0f);
			}
			else
			{
				HoveredTileOffset = (-0.5f, -0.5f);
			}
			CanvasNeedsRedraw.Invoke();
		}

		#endregion

		private void OnTilesetChanged()
		{
			if (CheckIfDisposed()) return;

			var cts = new CancellationTokenSource();
			var previousCts = Interlocked.Exchange(ref _tilesetChangedDebouceCts, cts);
			previousCts?.Cancel();
			previousCts?.Dispose();

			var token = cts.Token;

			_ = Task.Run(async () =>
			{
				try
				{
					await Task.Delay(TilesetChangedDebounceDelay, token);
					if (token.IsCancellationRequested) return;
					await MainThread.InvokeOnMainThreadAsync(RecalculateTileset);
				}
				catch (TaskCanceledException) { }
			}, _disposeCts.Token);
		}

		private void RecalculateTileset()
		{
			if (CheckIfDisposed()) return;

			int refreshVersion = Interlocked.Increment(ref _refreshVersion);
			
			_cachedTilePixelSize = _tilesetConnection?.Tileset?.TilePixelSize ?? new Scale(1, 1);
			_cachedImagePixelSize = _tilesetConnection?.Tileset?.ImagePixelSize ?? new Scale(1, 1);
			_cachedTileSetSize = _cachedImagePixelSize / _cachedTilePixelSize;

			OnPropertyChanged(nameof(TilePixelSize));
			OnPropertyChanged(nameof(ImagePixelSize));
			OnPropertyChanged(nameof(TileSetSize));

			_renderedBitmap?.Dispose();
			_renderedBitmap = null;

			foreach (var tileBitmap in _tileBitmaps.Values)
			{
				tileBitmap.Dispose();
			}
			_tileBitmaps.Clear();

			FileName = _tilesetConnection?.CurrentFile?.Name;
			if (_tilesetConnection == null || _tilesetConnection.CurrentFile == null || _tilesetConnection.Tileset == null)
			{
				Tiles.Clear();
				return;
			}

			TileViewModel[] newTiles = _tilesetConnection.Tileset.GetAllTiles().Select(t => new TileViewModel(t, this)).ToArray();
			if (Tiles.Count > 0 && SelectedTile != null)
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

			foreach (var tile in Tiles)
			{
				tile.RunValidation();
			}

			MainThread.BeginInvokeOnMainThread(async () =>
			{
				if (CheckIfDisposed()) return;

				if (_windowProvider == null || !_showRenderMessage)
				{
					await RenderPreviews(refreshVersion, _disposeCts.Token);
				}
				else await _windowProvider.PopupService.ShowProgressOnTaskAsync(
					"Rendering Preview", isIndeterminate: true,
					_ => RenderPreviews(refreshVersion, _disposeCts.Token));
				_showRenderMessage = false;
			});
		}

		private async Task RenderPreviews(int version, CancellationToken cancellationToken)
		{
			if (_tilesetConnection == null || _tilesetConnection.CurrentFile == null || _tilesetConnection.Tileset == null) return;

			BeginOperation();
			try
			{
				using (var stream = await _tilesetConnection.RenderFileAsync(".png", cancellationToken))
				{
					if (stream.CanSeek) stream.Position = 0;
					_renderedBitmap = SKBitmap.Decode(stream);
				}

				var tilePixelSize = TilePixelSize;
				if (Interlocked.CompareExchange(ref _refreshVersion, version, version) != version)
				{
					throw new OperationCanceledException("A newer render operation has started.");
				}

				foreach (var tile in Tiles)
				{
					tile.PreviewImage = ImageSource.FromStream(token =>
					{
						if (Interlocked.CompareExchange(ref _refreshVersion, version, version) != version)
						{
							throw new OperationCanceledException("A newer render operation has started.");
						}

						return _tilesetConnection.RenderSegmentAsync(
						".png",
						left: tile.Value.Column * tilePixelSize.Width,
						top: tile.Value.Row * tilePixelSize.Height,
						right: (tile.Value.Column + 1) * tilePixelSize.Width,
						bottom: (tile.Value.Row + 1) * tilePixelSize.Height,
						null,
						token);
					});
				}

				await MainThread.InvokeOnMainThreadAsync(CanvasNeedsRedraw);
			}
			catch (OperationCanceledException) { }
			finally
			{
				EndOperation();
			}
		}

		public void RenderCanvas(SKCanvas canvas, int width, int height) // note that this must run synchronously
		{
			if (CheckIfDisposed()) return;
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

				DrawGrid(canvas, previewRect, TilePixelSize / 2, 2, majorPaint, minorPaint);
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

			var previewRect = GetRectAtScale(new Scale(_inContextTilemap.Rect.Width, _inContextTilemap.Rect.Height))!.Value;

			SKPaint? majorPaint = null;
			SKPaint? minorPaint = null;

			if (SelectedTile.Type == TileType.Singular)
			{
				for (int row = 0; row < _inContextTilemap.Rect.Height; row++)
				{
					for (int col = 0; col < _inContextTilemap.Rect.Width; col++)
					{
						var tileRect = GetTileRect(row, col);
						DrawSingleTile(canvas, new TileData() { Tile = SelectedTile.Value }, tileRect);
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
			else if (SelectedTile.Type == TileType.DualTileMaterial)
			{
				var material = new Material(SelectedTile.Value.MaterialName, () => Tiles.Select(t => t.Value));
				_inContextTilemap.Clear();
				_inContextTilemap.AddSampleDualGridMaterial(material);
				DrawTilemap(canvas, _inContextTilemap.Composite);

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
					var tiles = _inContextTilemap.Composite.GetTilesAt(HoveredTile.Value.col, HoveredTile.Value.row);
					var tile = tiles != null && tiles.Count > 0 ? tiles[0].Tile : null;
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
				DrawGrid(canvas, previewRect, TilePixelSize / 2, 2, majorPaint, minorPaint);
				DrawBorder(canvas, previewRect);

				majorPaint.Dispose();
				minorPaint.Dispose();
			}
		}

		private void DrawPaintPreview(SKCanvas canvas)
		{
			if (_renderedBitmap == null) return;

			var previewRect = GetRectAtScale(new Scale(_paintTilemap.Rect.Width, _paintTilemap.Rect.Height))!.Value;

			DrawTilemap(canvas, _paintTilemap.Composite);

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

				DrawGrid(canvas, previewRect, TilePixelSize / 2, 2, majorPaint, minorPaint);
				previewRect.Bottom -= TilePixelSize.Height * (float)SelectedZoomLevel;
				previewRect.Right -= TilePixelSize.Width * (float)SelectedZoomLevel;
				DrawBorder(canvas, previewRect);
			}

			if (HoveredTile != null)
			{
				var tileRect = GetTileRect(HoveredTile.Value.row, HoveredTile.Value.col);
				switch (SelectedPaintTool)
				{
					case PaintTool.Cursor:
						{
							var tiles = _paintTilemap.Composite.GetTilesAt(HoveredTile.Value.col, HoveredTile.Value.row);
							var tile = tiles != null && tiles.Count > 0 ? tiles[0].Tile : null;
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
				PreviewMode.InContext => new SKRectI(0, 0, TilePixelSize.Width * 8, TilePixelSize.Height * 8),
				PreviewMode.Paint => new SKRectI(0, 0, TilePixelSize.Width * 8, TilePixelSize.Height * 8),
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
					scale.Width * TilePixelSize.Width * zoomFactor,
					scale.Height * TilePixelSize.Height * zoomFactor),
			};
		}

		public SKRect GetTileRect(int row, int column)
		{
			if (_tilesetConnection?.Tileset == null) return SKRect.Empty;
			float width = TilePixelSize.Width * (float)SelectedZoomLevel;
			float height = TilePixelSize.Height * (float)SelectedZoomLevel;
			float top = height * row + PreviewOffset.Y * (float)SelectedZoomLevel;
			float left = width * column + PreviewOffset.X * (float)SelectedZoomLevel;
			float right = left + width;
			float bottom = top + height;

			return new SKRect(left, top, right, bottom);
		}

		public SKRectI GetUnscaledTileRect(int row, int column)
		{
			if (_tilesetConnection?.Tileset == null) return SKRectI.Empty;
			int width = TilePixelSize.Width;
			int height = TilePixelSize.Height;
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
			if (!ShowGridLines) return;
			if (spacing.Width <= 0 || spacing.Height <= 0 || SelectedZoomLevel <= 0) return;

			float sx = spacing.Width * (float)SelectedZoomLevel;
			float sy = spacing.Height * (float)SelectedZoomLevel;

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
			using var tileBitmap = new SKBitmap(TilePixelSize.Width, TilePixelSize.Height);
			if (!_renderedBitmap.ExtractSubset(tileBitmap, GetUnscaledTileRect(tileData.Tile.Row, tileData.Tile.Column))) return;
			using var transformedBitmap = new SKBitmap(TilePixelSize.Width, TilePixelSize.Height);
			using (var tileCanvas = new SKCanvas(transformedBitmap))
			{
				tileCanvas.Clear(SKColors.Transparent);

				float cx = TilePixelSize.Width / 2f;
				float cy = TilePixelSize.Height / 2f;

				var matrix = SKMatrix.Identity;
				matrix = matrix.PostConcat(SKMatrix.CreateTranslation(-cx, -cy));
				matrix = matrix.PostConcat(tileData.Transformation.ToSKMatrix());
				matrix = matrix.PostConcat(SKMatrix.CreateTranslation(cx, cy));

				tileCanvas.SetMatrix(matrix);
				tileCanvas.DrawBitmap(tileBitmap, 0, 0);
			}
			canvas.DrawBitmap(transformedBitmap, rect);
		}

		private void DrawTilemap(SKCanvas canvas, ITilemap tilemap)
		{
			foreach (var (tiles, (x, y)) in tilemap)
			{
				if (tiles.Count == 0) continue;
				SKRect tileRect = GetTileRect(y, x);
				tileRect.Offset(0.5f, 0.5f);
				foreach (var tileData in tiles)
				{
					DrawSingleTile(canvas, tileData, tileRect);
				}
			}
		}

		#endregion

		#region Commands

		[RelayCommand]
		public async Task OpenLanding()
		{
			_windowService.OpenLandingWindow();
		}

		[RelayCommand]
		public async Task SaveDesign()
		{
			if (_tilesetConnection?.Tileset == null) return;
			BeginOperation();
			try
			{
				PreSave();
				var file = _tilesetConnection.CurrentFile;
				if (file == null) return;
				await _tilesetConnection.SaveAsync(file);
				HasUnsavedChanges = false;
			}
			finally
			{
				EndOperation();
			}
		}

		[RelayCommand]
		public async Task SaveDesignAs()
		{
			if (_tilesetConnection?.Tileset == null) return;
			BeginOperation();
			try
			{
				PreSave();
				var svgFile = _tilesetConnection.CurrentFile;
				if (svgFile == null) return;
				using var ms = new MemoryStream();
				await _tilesetConnection.SaveToStreamAsync(ms);
				ms.Position = 0;
				var result = await _fileSaver.SaveAsync(svgFile.FullName, svgFile.Name, ms);
			}
			finally
			{
				EndOperation();
			}
		}

		private void PreSave()
		{
			if (_tilesetConnection?.Tileset == null) return;
			var bakedTiles = Tiles.ToArray(); // prevent collection modified during enumeration
			foreach (var tileWrapper in bakedTiles)
			{
				_tilesetConnection.Tileset.Update(tileWrapper.Value);
			}
		}

		[RelayCommand]
		public void Exit()
		{
			_windowProvider?.CloseWindow();
		}

		[RelayCommand]
		public async Task OpenSettings()
		{
			var task = _windowProvider?.NavPage.PushAsync(_serviceProvider.GetRequiredService<SettingsPage>());
			if (task == null) return;
			await task;
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
						var tiles = _inContextTilemap.Composite.GetTilesAt(column, row);
						var tile = tiles != null && tiles.Count > 0 ? tiles[0].Tile : null;
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
									var tiles = _paintTilemap.Composite.GetTilesAt(column, row);
									var tile = tiles != null && tiles.Count > 0 ? tiles[0].Tile : null;
									if (tile == null) return;
									SelectedTile = Tiles.FirstOrDefault(t => t.Value.Row == tile.Row && t.Value.Column == tile.Column);
								}
								break;
							case PaintTool.Paint:
								{
									if (SelectedTile == null) return;
									switch (SelectedTile.Type)
									{
										case TileType.Singular:
											_paintTilemap.Regular.SetTileAt(column, row, new TileData() { Tile = SelectedTile.Value });
											break;
										case TileType.DualTileMaterial:
											_paintTilemap.DualGridMaterial[column, row] = new Material(SelectedTile.Value.MaterialName, () => Tiles.Select(t => t.Value));
											break;
									}

									CanvasNeedsRedraw.Invoke();
								}
								break;
							case PaintTool.Eraser:
								{
									if (SelectedTile == null) return;
									switch (SelectedTile.Type)
									{
										case TileType.Singular:
											_paintTilemap.Regular.ClearTilesAt(column, row);
											break;
										case TileType.DualTileMaterial:
											_paintTilemap.DualGridMaterial[column, row] = null;
											break;
									}

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

			BeginOperation();
			try
			{
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
			finally
			{
				EndOperation();
			}
		}

		[RelayCommand]
		public async Task FillTiles()
		{
			if (_tilesetConnection?.Tileset == null) return;

			BeginOperation();
			try
			{
				TilesetFillSettings settings = TilesetFillSettings.None;
				if (ReplaceExistingTiles) settings |= TilesetFillSettings.ReplaceExisting;
				if (FillEmptyTiles) settings |= TilesetFillSettings.FillEmptyTiles;

				if (_windowProvider == null)
				{
					await _tilesetConnection.FillTilesAsync(settings, null, _disposeCts.Token);
				}
				else await _windowProvider.PopupService.ShowProgressOnTaskAsync(message: "Filling Tiles...", isIndeterminate: false, async progress =>
				{
					await _tilesetConnection.FillTilesAsync(settings, progress, _disposeCts.Token);
				});

				HasUnsavedChanges = true;
			}
			finally
			{
				EndOperation(); 
			}
		}

		[ObservableProperty] public partial bool ReplaceExistingTiles { get; set; } = false;
		[ObservableProperty] public partial bool FillEmptyTiles { get; set; } = false;

		[RelayCommand]
		public async Task ClearAllTiles()
		{
			if (_tilesetConnection?.Tileset == null) return;

			BeginOperation();
			try
			{
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
			finally
			{
				EndOperation(); 
			}
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

		[RelayCommand]
		public void EditInInkscape()
		{
			if (_tilesetConnection?.CurrentFile == null) return;
			_tilesetConnection.OpenInExternalEditor();
		}

		#endregion


		#region Exports
		[RelayCommand]
		public async Task ExportTilesetImage(string extension)
		{
			if (_tilesetConnection?.CurrentFile == null) return;
			BeginOperation();
			try
			{
				if (_windowProvider == null)
				{
					using var stream = await _tilesetConnection.RenderFileAsync(extension);
					var result = await _fileSaver.SaveAsync($"{Path.GetFileNameWithoutExtension(_tilesetConnection.CurrentFile.Name)}.{extension}", stream);
				}
				else await _windowProvider.PopupService.ShowProgressOnTaskAsync("Exporting...", isIndeterminate: true, async _ =>
				{
					using var stream = await _tilesetConnection.RenderFileAsync(extension);
					var result = await _fileSaver.SaveAsync($"{Path.GetFileNameWithoutExtension(_tilesetConnection.CurrentFile.Name)}.{extension}", stream);
				});
			}
			finally
			{
				EndOperation();
			}
		}

		[RelayCommand]
		public async Task ExportSelectedTileImage(string extension)
		{
			if (_tilesetConnection?.CurrentFile == null) return;
			if (SelectedTile == null) return;

			BeginOperation();
			try
			{
				if (_windowProvider == null)
				{
					using var stream = await GetTileImageStream(SelectedTile.Value, extension);
					if (stream == null) return;
					string tileFileName = $"{SelectedTile.Name} [{Path.GetFileNameWithoutExtension(_tilesetConnection.CurrentFile.Name)}].{extension}";
					var result = await _fileSaver.SaveAsync(tileFileName, stream, _disposeCts.Token);
				}
				else await _windowProvider.PopupService.ShowProgressOnTaskAsync("Exporting...", isIndeterminate: true, async _ =>
				{
					using var stream = await GetTileImageStream(SelectedTile.Value, extension);
					if (stream == null) return;
					string tileFileName = $"{SelectedTile.Name} [{Path.GetFileNameWithoutExtension(_tilesetConnection.CurrentFile.Name)}].{extension}";
					var result = await _fileSaver.SaveAsync(tileFileName, stream, _disposeCts.Token);
				});
			}
			finally
			{
				EndOperation();
			}
		}

		private async Task<Stream?> GetTileImageStream(Tile tile, string extension)
		{
			if (_tilesetConnection?.CurrentFile == null || _tilesetConnection.Tileset == null) return null;
			var tileset = _tilesetConnection.Tileset;

			return await _tilesetConnection.RenderSegmentAsync(
				extension,
				left: tile.Column * tileset.TilePixelSize.Width,
				top: tile.Row * tileset.TilePixelSize.Height,
				right: (tile.Column + 1) * tileset.TilePixelSize.Width,
				bottom: (tile.Row + 1) * tileset.TilePixelSize.Height,
				null,
				_disposeCts.Token);
		}

		[RelayCommand]
		public async Task ExportSeparatedTilesPng()
		{
			if (_tilesetConnection?.CurrentFile == null) return;
			if (_tilesetConnection.Tileset == null) return;

			BeginOperation();
			FileInfo? tmpFile = null;
			try
			{
				tmpFile = new FileInfo(Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.zip"));
				var tileset = _tilesetConnection.Tileset;
				using (var zip = new ZipArchive(tmpFile.OpenWrite(), ZipArchiveMode.Create, leaveOpen: false))
				{
					if (_windowProvider == null)
					{
						foreach (var tvm in Tiles)
						{
							using var stream = await _tilesetConnection.RenderSegmentAsync(
								".png",
								left: tvm.Value.Column * tileset.TilePixelSize.Width,
								top: tvm.Value.Row * tileset.TilePixelSize.Height,
								right: (tvm.Value.Column + 1) * tileset.TilePixelSize.Width,
								bottom: (tvm.Value.Row + 1) * tileset.TilePixelSize.Height,
								null,
								_disposeCts.Token);
							string tileFileName = $"{tvm.Name} [{Path.GetFileNameWithoutExtension(_tilesetConnection.CurrentFile.Name)}].png";
							var entry = zip.CreateEntry(tileFileName);
							using var entryStream = await entry.OpenAsync();
							await stream.CopyToAsync(entryStream, _disposeCts.Token);
						}
					}
					else await _windowProvider.PopupService.ShowProgressOnTaskAsync("Exporting...", isIndeterminate: false, async progress =>
					{
						foreach (var (tvm, i) in Tiles.Select((t, i) => (t, i)))
						{
							progress.Report((double)i / Tiles.Count);
							using var stream = await _tilesetConnection.RenderSegmentAsync(
								".png",
								left: tvm.Value.Column * tileset.TilePixelSize.Width,
								top: tvm.Value.Row * tileset.TilePixelSize.Height,
								right: (tvm.Value.Column + 1) * tileset.TilePixelSize.Width,
								bottom: (tvm.Value.Row + 1) * tileset.TilePixelSize.Height,
								null,
								_disposeCts.Token);
							string tileFileName = $"{tvm.Name} [{Path.GetFileNameWithoutExtension(_tilesetConnection.CurrentFile.Name)}].png";
							var entry = zip.CreateEntry(tileFileName);
							using var entryStream = await entry.OpenAsync();
							await stream.CopyToAsync(entryStream, _disposeCts.Token);
						}
					});
				}
				using var fs = new FileStream(tmpFile.FullName, FileMode.Open, FileAccess.Read);
				_ = await _fileSaver.SaveAsync($"{Path.GetFileNameWithoutExtension(_tilesetConnection.CurrentFile.Name)}.zip", fs);		
			}
			finally
			{
				if (tmpFile?.Exists ?? false) tmpFile.Delete();
				EndOperation();
			}			
		}

		[RelayCommand]
		public async Task ExportUnityPackage()
		{
			if (_tilesetConnection?.CurrentFile == null) return;
			if (_tilesetConnection.Tileset == null) return;

			BeginOperation();
			FileInfo? tmpFile = null;
			try
			{
				tmpFile = new FileInfo(Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.unitypackage"));
				using (var writer = new UnityPackageWriter(tmpFile.OpenWrite(), leaveOpen: false))
				{
					var exporter = new UnityPackageExporter(_settingsService, _tilesetConnection);

					if (_windowProvider == null)
					{
						await exporter.WriteTilesetPackageAsync(writer);
					}
					else await _windowProvider.PopupService.ShowProgressOnTaskAsync("Exporting...", isIndeterminate: true, async progress =>
					{
						await exporter.WriteTilesetPackageAsync(writer);
					});
				}
				using var fs = tmpFile.OpenRead();
				_ = await _fileSaver.SaveAsync($"{Path.GetFileNameWithoutExtension(_tilesetConnection.CurrentFile.Name)}.unitypackage", fs);
			}
			finally
			{
				if (tmpFile?.Exists ?? false) tmpFile.Delete();
				EndOperation();
			}
		}

		[RelayCommand]
		public async Task ExportMaterial()
		{
			if (_tilesetConnection?.CurrentFile == null) return;
			if (SelectedTile == null) return;
			if (string.IsNullOrWhiteSpace(SelectedTile.MaterialName)) return;

			FileInfo? tmpFile = null;
			BeginOperation();

			try
			{
				var material = new Material(SelectedTile.MaterialName, () => Tiles.Select(t => t.Value));
				MaterialExporter? exporter = null;
				foreach (var type in Assembly.GetAssembly(typeof(MaterialExporter))!.GetTypes()
					.Where(t => typeof(MaterialExporter).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract))
				{
					var constructor = type.GetConstructor([typeof(string), typeof(ITilesetConnection), typeof(ITilesetRenderingService)])
						?? throw new Exception($"No valid constructor found for type: {type.FullName}");
					exporter = (MaterialExporter)constructor.Invoke([material.Name, _tilesetConnection]);
					if (exporter.Type != material.Type) continue;
				}

				if (exporter == null) throw new Exception($"No exporter found for material type: {material.Type}");

				tmpFile = new FileInfo(Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.png"));

				if (_windowProvider == null)
				{
					await exporter.ExportAsync(tmpFile, TilePixelSize);
				}
				else await _windowProvider.PopupService.ShowProgressOnTaskAsync("Exporting...", isIndeterminate: true, async progress =>
				{
					await exporter.ExportAsync(tmpFile, TilePixelSize);
				});

				using var fs = tmpFile.OpenRead();
				_ = await _fileSaver.SaveAsync($"{material.Name}.png", fs);
			}
			finally
			{
				if (tmpFile?.Exists ?? false) tmpFile.Delete();
				EndOperation();
			}
		}

		public async ValueTask DisposeAsync()
		{
			GC.SuppressFinalize(this);

			_tilesetConnection?.TilesetChanged -= HandleTilesetChanged;
			_disposeCts.Cancel();
			var debounceCts = Interlocked.Exchange(ref _tilesetChangedDebouceCts, null);
			debounceCts?.Cancel();
			_windowProvider = null;
			CanvasNeedsRedraw = delegate { };

			if (Interlocked.Exchange(ref _disposeState, DISPOSAL) != ACTIVE)
			{
				return;
			}

			if (Volatile.Read(ref _activeOperationCount) != 0)
			{
				await _disposeCompletion.Task.ConfigureAwait(false);
			}

			if (_tilesetConnection != null)
			{
				await _tilesetConnection.DisposeAsync();
				_tilesetConnection = null;
			}


			// Avoid disposing Skia resources during WinUI shutdown.
			// The canvas may still be tearing down, and aggressive disposal here can cause native access violations.
			_renderedBitmap = null;
			_tileBitmaps.Clear();

			debounceCts?.Dispose();
			_disposeCts.Dispose();
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
