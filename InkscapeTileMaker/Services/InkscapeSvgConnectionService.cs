using InkscapeTileMaker.Models;
using InkscapeTileMaker.Utility;
using InkscapeTileMaker.ViewModels;
using System.Xml.Linq;

namespace InkscapeTileMaker.Services;

public partial class InkscapeSvgConnectionService : ITilesetConnection
{
	private readonly IServiceProvider _services;

	private FileInfo? _file;

	public ITileset? Tileset { get; private set; }

	public FileInfo? CurrentFile => _file;

	public event Action<ITileset> TilesetChanged = delegate { };

	private InkscapeSvg? _svg;

	private FileSystemWatcher? _fileWatcher;

	private bool isLoading = false;

	public InkscapeSvgConnectionService(IServiceProvider services)
	{
		_services = services;
	}

	public async Task LoadAsync(FileInfo file)
	{
		if (Interlocked.Exchange(ref isLoading, true) == true) return;

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
					TilesetChanged.Invoke(Tileset);

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
		finally
		{
			Interlocked.Exchange(ref isLoading, false);
		}
	}

	private void SetupWatcher(FileInfo file)
	{
		_fileWatcher = new FileSystemWatcher(file.DirectoryName!, file.Name)
		{
			NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.Attributes,
			EnableRaisingEvents = true
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
				// log / swallow / schedule another retry
			}
		};
	}

	public async Task SaveAsync(FileInfo file)
	{
		if (_file is null || _svg is null) return;
		await _svg.SaveToStreamAsync(file.Open(FileMode.Create, FileAccess.Write));
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
		var element = _svg.GetTileElement(row, col);
		if (element is null) return null;
		var tile = TileExtensions.GetTileFromXElement(element);
		return GetTileViewModel(tile, designerViewModel);
	}

	public IEnumerable<Tile> Tiles
	{
		get
		{
			if (_svg is null) return [];
			return _svg.GetAllTileElements().Select(TileExtensions.GetTileFromXElement);
		}
	}

	public TileViewModel[] GetAllTiles(DesignerViewModel designerViewModel)
	{
		if (_svg is null) throw new InvalidOperationException("SVG Document is not loaded.");
		return [.. Tiles.Select((t) => GetTileViewModel(t, designerViewModel))];
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

	public bool AddTile(Tile tile)
	{
		if (_svg is null) throw new InvalidOperationException("SVG Document is not loaded.");
		XElement collectionElement = _svg.GetOrCreateTileCollectionElement() ?? throw new InvalidOperationException("Unable to get or create tile collection element in SVG.");
		var element = _svg.GetTileElement(tile.Row, tile.Column);
		if (element is not null)
		{
			// Tile already exists
			return false;
		}
		element = tile.ToXElement();
		collectionElement.Add(element);
		TilesetChanged.Invoke(Tileset!);
		return true;
	}

	public bool AddOrReplaceTile(Tile tile)
	{
		if (_svg is null) throw new InvalidOperationException("SVG Document is not loaded.");
		XElement collectionElement = _svg.GetOrCreateTileCollectionElement() ?? throw new InvalidOperationException("Unable to get or create tile collection element in SVG.");
		var element = _svg.GetTileElement(tile.Row, tile.Column);
		if (element is not null)
		{
			element.ReplaceWith(tile.ToXElement());
		}
		else
		{
			element = tile.ToXElement();
			collectionElement.Add(element);
		}
		TilesetChanged.Invoke(Tileset!);
		return true;
	}

	public bool RemoveTile(Tile tile)
	{
		if (_svg is null) throw new InvalidOperationException("SVG Document is not loaded.");
		XElement collectionElement = _svg.GetOrCreateTileCollectionElement() ?? throw new InvalidOperationException("Unable to get or create tile collection element in SVG.");
		var element = _svg.GetTileElement(tile.Row, tile.Column);
		if (element is null)
		{
			// Tile does not exist
			return false;
		}
		element.Remove();
		TilesetChanged.Invoke(Tileset!);
		return true;
	}

	public void ClearTiles()
	{
		if (_svg is null) throw new InvalidOperationException("SVG Document is not loaded.");
		XElement collectionElement = _svg.GetOrCreateTileCollectionElement();
		collectionElement.RemoveAll();
		TilesetChanged.Invoke(Tileset!);
	}

	public Scale GetTileSize()
	{
		if (_svg is null) throw new InvalidOperationException("SVG Document is not loaded.");
		return _svg.GetTileSize();
	}

	public Scale GetSvgSize()
	{
		if (_svg is null) throw new InvalidOperationException("SVG Document is not loaded.");
		return _svg.GetSvgSize();
	}

	public int GetTileCount()
	{
		if (_svg is null) throw new InvalidOperationException("SVG Document is not loaded.");
		return _svg.GetAllTileElements().Count();
	}

	public async Task FillTilesAsync(TilesetFillSettings settings)
	{
		if (_svg is null) throw new InvalidOperationException("SVG Document is not loaded.");
		if (Tileset is null) throw new InvalidOperationException("Tileset is not loaded.");

		int maxRow = Tileset.Size.height / Tileset.TileSize.height - 1;
		int maxCol = Tileset.Size.width / Tileset.TileSize.width - 1;

		foreach (var tileElement in _svg.GetAllTileElements())
		{
			var tile = TileExtensions.GetTileFromXElement(tileElement);
			if (tile.Row > maxRow) maxRow = tile.Row;
			if (tile.Column > maxCol) maxCol = tile.Column;
		}

		for (int row = 0; row <= maxRow; row++)
		{
			for (int col = 0; col <= maxCol; col++)
			{
				var element = _svg.GetTileElement(row, col);
				if (settings.HasFlag(TilesetFillSettings.ReplaceExisting) || element is null)
				{
					var newTile = new Tile
					{
						Name = $"Tile {col},{row}",
						Type = TileType.Singular,
						Allignment = TileAlignment.Core,
						Row = row,
						Column = col
					};

					if (!settings.HasFlag(TilesetFillSettings.FillEmptyTiles))
					{
						ITilesetRenderingService renderingService = _services.GetRequiredService<ITilesetRenderingService>();
						bool isEmpty = await renderingService.IsSegmentEmptyAsync(_file!, col * Tileset.TileSize.width, row * Tileset.TileSize.height,
							(col + 1) * Tileset.TileSize.width, (row + 1) * Tileset.TileSize.height, CancellationToken.None);
						if (isEmpty) continue;
					}

					AddOrReplaceTile(newTile);
				}
			}
		}

		TilesetChanged.Invoke(Tileset!);
	}
}
