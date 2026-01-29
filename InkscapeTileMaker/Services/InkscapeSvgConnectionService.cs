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

	public InkscapeSvgConnectionService(IServiceProvider services)
	{
		_services = services;
	}

	public void Load(FileInfo file)
	{
		_file = file;
		_svg = new InkscapeSvg(file.OpenRead());
		Tileset = new InkscapeSvgTileset(this);
		TilesetChanged.Invoke(Tileset);
	}

	public void Save(FileInfo file)
	{
		if (_file is null || _svg is null) return;
		_svg.SaveToStreamAsync(file.Open(FileMode.Create, FileAccess.Write)).Wait();
		_file = file;
		TilesetChanged.Invoke(Tileset!);
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
		XElement collectionElement = _svg.GetOrCreateTileCollectionElement()!;
		var tile = TileExtensions.GetTileFromXElement(element);
		return new TileViewModel(tile, designerViewModel, (tile) =>
		{
			var tileElement = _svg.GetTileElement(tile.Row, tile.Column);
			if (tileElement is not null)
			{
				tileElement.ReplaceWith(tile.ToXElement());
				TilesetChanged.Invoke(Tileset!);
			}
		});
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
		return Tiles.Select(tile =>
		{
			return new TileViewModel(tile, designerViewModel, (tile) =>
			{
				var tileElement = _svg.GetTileElement(tile.Row, tile.Column);
				if (tileElement is not null)
				{
					tileElement.ReplaceWith(tile.ToXElement());
					TilesetChanged.Invoke(Tileset!);
				}
			});
		}).ToArray();
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
		if (_svg?.Grid is null) throw new InvalidOperationException("SVG Document is not loaded.");
		var spacingX = _svg.Grid.Attribute(XName.Get("spacingx"))?.Value;
		var spacingY = _svg.Grid.Attribute(XName.Get("spacingy"))?.Value;
		var empSpacing = _svg.Grid.Attribute(XName.Get("empspacing"))?.Value;
		if (int.TryParse(spacingX, out int width) && int.TryParse(spacingY, out int height) && int.TryParse(empSpacing, out int unitsPerTile))
		{
			return new Scale() { width = width * unitsPerTile, height = height * unitsPerTile };
		}
		throw new InvalidDataException("Tile size information is missing or invalid in the SVG grid.");
	}

	public Scale GetSvgSize()
	{
		int width = Convert.ToInt32(_svg?.SvgRoot?.Attribute(XName.Get("width"))?.Value ?? "1");
		int height = Convert.ToInt32(_svg?.SvgRoot?.Attribute(XName.Get("height"))?.Value ?? "1");
		return new Scale() { width = width, height = height };
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
