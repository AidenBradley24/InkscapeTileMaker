using InkscapeTileMaker.Models;
using InkscapeTileMaker.Utility;
using InkscapeTileMaker.ViewModels;
using System.Diagnostics;
using System.Xml.Linq;

namespace InkscapeTileMaker.Services;

public partial class InkscapeSvgConnection : ITilesetConnection
{
	private readonly IServiceProvider _services;
	private readonly IWindowProvider _windowProvider;

	private FileInfo? _file;

	public ITileset? Tileset { get; private set; }

	public ITilesetRenderingService RenderingService { get; set; }

	public FileInfo? CurrentFile => _file;

	public event Action<ITilesetConnection> TilesetChanged = delegate { };

	private InkscapeSvg? _svg;
	private FileSystemWatcher? _fileWatcher;
	private bool _isLoading = false;

	public InkscapeSvgConnection(IServiceProvider services, IWindowProvider windowProvider)
	{
		_services = services;
		_windowProvider = windowProvider;

		var inkscapeService = services.GetRequiredService<IInkscapeService>();
		var tmpDirService = services.GetRequiredService<ITempDirectoryService>();
		RenderingService = new SvgRenderingService(inkscapeService, tmpDirService);
	}

	public async Task LoadAsync(FileInfo file)
	{
		if (Interlocked.Exchange(ref _isLoading, true) == true) return;

		try
		{
			_fileWatcher?.Dispose();

			const int maxRetries = 5;
			const int delayMs = 200;
			Exception? lastException = null;

			for (int attempt = 0; attempt < maxRetries; attempt++)
			{
				try
				{
					await using var stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
					_svg = new InkscapeSvg(stream);
					_file = file;
					Tileset = new InkscapeSvgTileset(this);
					RaiseTilesetChanged();

					SetupWatcher(file);
					return;
				}
				catch (IOException ex)
				{
					lastException = ex;
					await Task.Delay(delayMs);
				}
			}

			throw new IOException($"Failed to load SVG file after {maxRetries} attempts.", lastException);
		}
		catch (Exception ex)
		{
			Trace.TraceError("An unknown error occured during loading!");
			Trace.TraceError(ex.Message);
		}
		finally
		{
			Interlocked.Exchange(ref _isLoading, false);
		}
	}

	private void SetupWatcher(FileInfo file)
	{
		_fileWatcher?.Dispose();

		_fileWatcher = new FileSystemWatcher(file.DirectoryName!, file.Name)
		{
			NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.Attributes | NotifyFilters.FileName,
			EnableRaisingEvents = true,
			IncludeSubdirectories = false
		};

		_fileWatcher.Deleted += async (_, _) =>
		{
			if (!File.Exists(file.FullName))
			{
				await MainThread.InvokeOnMainThreadAsync(async () =>
				{
					await _windowProvider.NavPage.DisplayAlertAsync(
						"File Deleted",
						$"The file {file.FullName} has been deleted. The designer will now be cleared.",
						"OK");

					_svg = null;
					_file = null;
					Tileset = null;
					RaiseTilesetChanged();
				});
			}
		};

		_fileWatcher.Renamed += async (_, e) =>
		{
			// If the original file name was changed, treat it as a deletion of the current file
			if (string.Equals(e.OldFullPath, file.FullName, StringComparison.OrdinalIgnoreCase))
			{
				if (!File.Exists(file.FullName))
				{
					await MainThread.InvokeOnMainThreadAsync(async () =>
					{
						await _windowProvider.NavPage.DisplayAlertAsync(
							"File Deleted",
							$"The file {file.FullName} has been deleted. The designer will now be cleared.",
							"OK");

						_svg = null;
						_file = null;
						Tileset = null;
						RaiseTilesetChanged();
					});
				}
			}
		};

		_fileWatcher.Changed += async (_, _) =>
		{
			await Task.Delay(200);
			try
			{
				await LoadAsync(file);
			}
			catch (IOException)
			{
				Trace.WriteLine($"File {file.FullName} is currently inaccessible. Changes will be loaded when the file becomes available.");
				await MainThread.InvokeOnMainThreadAsync(async () =>
				{
					await _windowProvider.NavPage.DisplayAlertAsync(
						"File Inaccessible",
						$"File {file.FullName} is currently inaccessible. Changes will be loaded when the file becomes available.",
						"OK");
				});
			}
		};
	}

	public async Task SaveAsync(FileInfo file)
	{
		if (_file is null || _svg is null) return;
		using var fs = file.Open(FileMode.Create, FileAccess.Write, FileShare.None);
		await _svg.SaveToStreamAsync(fs);
		_file = file;
	}

	public async Task SaveToStreamAsync(Stream stream)
	{
		if (_svg is null) return;
		await _svg.SaveToStreamAsync(stream);
	}

	public TileViewModel? GetTile(int row, int col, DesignerViewModel designerViewModel)
	{
		if (_svg is null) return null;
		lock (_svg)
		{
			var element = _svg.GetTileElement(row, col);
			if (element is null) return null;
			var tile = TileExtensions.GetTileFromXElement(element);
			return GetTileViewModel(tile, designerViewModel);
		}
	}

	public TileViewModel[] GetAllTiles(DesignerViewModel designerViewModel)
	{
		if (_svg is null) throw new InvalidOperationException("SVG Document is not loaded.");
		return [.. GetTiles().Select((t) => GetTileViewModel(t, designerViewModel))];
	}

	private TileViewModel GetTileViewModel(Tile tile, DesignerViewModel designerViewModel)
	{
		return new TileViewModel(tile, designerViewModel, (tile) =>
		{
			ArgumentNullException.ThrowIfNull(_svg, nameof(_svg));
			var tileElement = _svg.GetTileElement(tile.Row, tile.Column);
			ArgumentNullException.ThrowIfNull(tileElement, "Tile element not found in SVG.");
			tileElement.ReplaceWith(tile.ToXElement());
			// note that sync doesn't change the state of the tileset, so we don't invoke TilesetChanged here
		});
	}

	public Tile[] GetTiles()
	{
		if (_svg is null) return [];
		lock (_svg)
		{
			return [.. _svg.GetAllTileElements().Select(TileExtensions.GetTileFromXElement)];
		}
	}

	public bool AddTile(Tile tile)
	{
		if (_svg is null) throw new InvalidOperationException("SVG Document is not loaded.");
		lock (_svg)
		{
			XElement collectionElement = _svg.GetOrCreateTileCollectionElement() ?? throw new InvalidOperationException("Unable to get or create tile collection element in SVG.");
			var element = _svg.GetTileElement(tile.Row, tile.Column);
			if (element is not null)
			{
				// Tile already exists
				return false;
			}
			element = tile.ToXElement();
			collectionElement.Add(element);
		}

		RaiseTilesetChanged();
		return true;
	}

	public void AddOrReplaceTile(Tile tile)
	{
		if (_svg is null) throw new InvalidOperationException("SVG Document is not loaded.");
		lock (_svg)
		{
			XElement collectionElement = _svg!.GetOrCreateTileCollectionElement();
			var element = _svg.GetTileElement(tile.Row, tile.Column);
			if (element is not null)
			{
				element.ReplaceWith(tile.ToXElement());
				return;
			}

			element = tile.ToXElement();
			collectionElement.Add(element);
		}
		RaiseTilesetChanged();
	}

	public bool RemoveTile(Tile tile)
	{
		if (_svg is null) throw new InvalidOperationException("SVG Document is not loaded.");
		lock (_svg)
		{
			var element = _svg.GetTileElement(tile.Row, tile.Column);
			if (element is null)
			{
				// Tile does not exist
				return false;
			}
			element.Remove();
		}

		RaiseTilesetChanged();
		return true;
	}

	public void ClearTiles()
	{
		if (_svg is null) throw new InvalidOperationException("SVG Document is not loaded.");
		lock (_svg)
		{
			XElement collectionElement = _svg.GetOrCreateTileCollectionElement();
			collectionElement.RemoveAll();
		}
		RaiseTilesetChanged();
	}

	public Scale GetTileSize()
	{
		if (_svg is null) throw new InvalidOperationException("SVG Document is not loaded.");
		lock (_svg)
		{
			return _svg.GetTileSize();
		}
	}

	public Scale GetSvgSize()
	{
		if (_svg is null) throw new InvalidOperationException("SVG Document is not loaded.");
		lock (_svg)
		{
			return _svg.GetSvgSize();
		}
	}

	public int GetTileCount()
	{
		if (_svg is null) throw new InvalidOperationException("SVG Document is not loaded.");
		lock (_svg)
		{
			return _svg.GetAllTileElements().Count();
		}
	}

	public async Task FillTilesAsync(TilesetFillSettings settings, IProgress<double>? progressReporter = default)
	{
		if (_svg is null) throw new InvalidOperationException("SVG Document is not loaded.");
		if (Tileset is null) throw new InvalidOperationException("Tileset is not loaded.");
		if (RenderingService == null) throw new InvalidOperationException("Rendering service is not set.");

		int maxRow = Tileset.ImagePixelSize.Height / Tileset.TilePixelSize.Height - 1;
		int maxCol = Tileset.ImagePixelSize.Width / Tileset.TilePixelSize.Width - 1;

		foreach (var tileElement in _svg.GetAllTileElements())
		{
			var tile = TileExtensions.GetTileFromXElement(tileElement);
			if (tile.Row > maxRow) maxRow = tile.Row;
			if (tile.Column > maxCol) maxCol = tile.Column;
		}

		int totalRows = maxRow + 1;
		int totalCols = maxCol + 1;
		int totalTiles = totalRows * totalCols;

		for (int row = 0; row < totalRows; row++)
		{
			for (int col = 0; col < totalCols; col++)
			{
				int index = row * totalCols + col;
				progressReporter?.Report(index / (double)totalTiles);

				var element = _svg.GetTileElement(row, col);
				if (settings.HasFlag(TilesetFillSettings.ReplaceExisting) || element is null)
				{
					var newTile = new Tile
					{
						Name = $"Tile {col},{row}",
						Type = TileType.Singular,
						Variant = TileVariant.Core,
						Alignment = TileAlignment.Core,
						Row = row,
						Column = col
					};

					if (!settings.HasFlag(TilesetFillSettings.FillEmptyTiles))
					{
						bool isEmpty = await RenderingService.IsSegmentEmptyAsync(_file!, col * Tileset.TilePixelSize.Width, row * Tileset.TilePixelSize.Height,
							(col + 1) * Tileset.TilePixelSize.Width, (row + 1) * Tileset.TilePixelSize.Height, CancellationToken.None);
						if (isEmpty) continue;
					}

					AddOrReplaceTile(newTile);
				}
			}
		}
	}

	public void OpenInExternalEditor()
	{
		if (CurrentFile == null) return;
		_services.GetRequiredService<IInkscapeService>().OpenFileInInkscape(CurrentFile);
	}

	public Task<Stream> RenderFileAsync(string extension, CancellationToken cancellationToken = default)
	{
		if (_file == null) throw new InvalidOperationException("No file loaded to render.");
		if (RenderingService == null) throw new InvalidOperationException("Rendering service is not set.");
		return RenderingService.RenderFileAsync(_file, extension, cancellationToken);
	}

	public Task<Stream> RenderSegmentAsync(string extension, int left, int top, int right, int bottom, Scale? exportScale = null, CancellationToken cancellationToken = default)
	{
		if (_file == null) throw new InvalidOperationException("No file loaded to render.");
		if (RenderingService == null) throw new InvalidOperationException("Rendering service is not set.");
		return RenderingService.RenderSegmentAsync(_file, extension, left, top, right, bottom, exportScale, cancellationToken);
	}

	public Task<bool> IsSegmentEmptyAsync(int left, int top, int right, int bottom, CancellationToken cancellationToken = default)
	{
		if (_file == null) throw new InvalidOperationException("No file loaded to render.");
		if (RenderingService == null) throw new InvalidOperationException("Rendering service is not set.");
		return RenderingService.IsSegmentEmptyAsync(_file, left, top, right, bottom, cancellationToken);
	}

	private void RaiseTilesetChanged()
	{
		var handlers = TilesetChanged;
		foreach (var handler in handlers.GetInvocationList())
		{
			try
			{
				handler.DynamicInvoke(this);
			}
			catch (Exception ex)
			{
				Trace.TraceError($"Error invoking TilesetChanged handler: {ex}");
			}
		}
	}
}
