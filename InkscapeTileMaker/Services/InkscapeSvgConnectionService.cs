using InkscapeTileMaker.Models;
using InkscapeTileMaker.Utility;
using InkscapeTileMaker.ViewModels;
using System.Xml.Linq;

namespace InkscapeTileMaker.Services;

public partial class InkscapeSvgConnectionService : ITilesetConnection
{
	private readonly IServiceProvider _services;

	private FileInfo? _svgFile;

	public ITileset? Tileset { get; private set; }

	public FileInfo? CurrentFile => _svgFile;

	public event Action<ITileset> TilesetChanged = delegate { };

	public InkscapeSvgConnectionService(IServiceProvider services)
	{
		_services = services;
	}


	#region XML Elements

	public XDocument? Document { get; private set; }
	public XElement? SvgRoot => Document?.Root;
	public XElement? NamedView => SvgRoot?.Element(XName.Get("namedview", sodipodiNamespace.NamespaceName));
	public XElement? Grid => NamedView?.Element(XName.Get("grid", inkscapeNamespace.NamespaceName));
	public XElement? Defs => SvgRoot?.Element(XName.Get("defs", svgNamespace.NamespaceName));

	public const string appNamespacePrefix = "tilemaker";

	public static readonly XNamespace appNamespace = "https://github.com/AidenBradley24/InkscapeTileMaker";
	public static readonly XNamespace inkscapeNamespace = "http://www.inkscape.org/namespaces/inkscape";
	public static readonly XNamespace sodipodiNamespace = "http://sodipodi.sourceforge.net/DTD/sodipodi-0.dtd";
	public static readonly XNamespace svgNamespace = "http://www.w3.org/2000/svg";

	public static readonly XName appDefsName = appNamespace + "tilemakerdefs";
	public static readonly XName tileCollectionName = appNamespace + "tiles";
	public static readonly XName tileName = appNamespace + "tile";

	#endregion

	public void Load(FileInfo svgFile)
	{
		_svgFile = svgFile;
		Document = XDocument.Load(svgFile.FullName);

		if (SvgRoot is not null)
		{
			var existingNs = SvgRoot.GetNamespaceOfPrefix(appNamespacePrefix);
			if (existingNs == null || existingNs != appNamespace)
			{
				SvgRoot.SetAttributeValue(XNamespace.Xmlns + appNamespacePrefix, appNamespace.NamespaceName);
			}
		}

		Tileset = new InkscapeSvgTileset(this);
		TilesetChanged.Invoke(Tileset);
	}

	public void Save(FileInfo file)
	{
		if (_svgFile is null || Document is null) return;
		Document.Save(file.FullName);
		_svgFile = file;
		TilesetChanged.Invoke(Tileset!);
	}

	public async Task SaveToStreamAsync(Stream stream)
	{
		if (Document is null) return;
		await Document.SaveAsync(stream, SaveOptions.None, CancellationToken.None);
	}

	public TileViewModel? GetTile(int row, int col, DesignerViewModel designerViewModel)
	{
		var element = GetTileElement(row, col);
		if (element is null) return null;
		XElement collectionElement = GetOrCreateTileCollectionElement()!;
		var tile = TileExtensions.GetTileFromXElement(element);
		return new TileViewModel(tile, designerViewModel, (tile) =>
		{
			var tileElement = GetTileElement(tile.Row, tile.Column);
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
			if (Document is null) return [];
			XElement collectionElement = GetOrCreateTileCollectionElement();
			return collectionElement.Elements(tileName).Select(tileElement => TileExtensions.GetTileFromXElement(tileElement));
		}
	}

	public TileViewModel[] GetAllTiles(DesignerViewModel designerViewModel)
	{
		if (Document is null) throw new InvalidOperationException("SVG Document is not loaded.");
		XElement collectionElement = GetOrCreateTileCollectionElement() ?? throw new InvalidOperationException("Unable to get or create tile collection element in SVG.");
		return [.. collectionElement
			.Elements(tileName)
			.Select(tileElement =>
			{
				var tile = TileExtensions.GetTileFromXElement(tileElement);
				return new TileViewModel(tile, designerViewModel, (tile) =>
				{
					var tileElement = GetTileElement(tile.Row, tile.Column);
					if (tileElement is not null)
					{
						tileElement.ReplaceWith(tile.ToXElement());
						TilesetChanged.Invoke(Tileset!);
					}
				});
			})];
	}

	public bool AddTile(Tile tile)
	{
		if (Document is null) throw new InvalidOperationException("SVG Document is not loaded.");
		XElement collectionElement = GetOrCreateTileCollectionElement() ?? throw new InvalidOperationException("Unable to get or create tile collection element in SVG.");
		var element = GetTileElement(tile.Row, tile.Column);
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
		if (Document is null) throw new InvalidOperationException("SVG Document is not loaded.");
		XElement collectionElement = GetOrCreateTileCollectionElement() ?? throw new InvalidOperationException("Unable to get or create tile collection element in SVG.");
		var element = GetTileElement(tile.Row, tile.Column);
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
		if (Document is null) throw new InvalidOperationException("SVG Document is not loaded.");
		XElement collectionElement = GetOrCreateTileCollectionElement() ?? throw new InvalidOperationException("Unable to get or create tile collection element in SVG.");
		var element = GetTileElement(tile.Row, tile.Column);
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
		if (Document is null) throw new InvalidOperationException("SVG Document is not loaded.");
		XElement collectionElement = GetOrCreateTileCollectionElement();
		collectionElement.RemoveAll();
		TilesetChanged.Invoke(Tileset!);
	}

	public Scale GetTileSize()
	{
		if (Grid is null) throw new InvalidOperationException("SVG Document is not loaded.");
		var spacingX = Grid.Attribute(XName.Get("spacingx"))?.Value;
		var spacingY = Grid.Attribute(XName.Get("spacingy"))?.Value;
		var empSpacing = Grid.Attribute(XName.Get("empspacing"))?.Value;
		if (int.TryParse(spacingX, out int width) && int.TryParse(spacingY, out int height) && int.TryParse(empSpacing, out int unitsPerTile))
		{
			return new Scale() { width = width * unitsPerTile, height = height * unitsPerTile };
		}
		throw new InvalidDataException("Tile size information is missing or invalid in the SVG grid.");
	}

	public Scale GetSvgSize()
	{
		int width = Convert.ToInt32(SvgRoot?.Attribute(XName.Get("width"))?.Value ?? "1");
		int height = Convert.ToInt32(SvgRoot?.Attribute(XName.Get("height"))?.Value ?? "1");
		return new Scale() { width = width, height = height };
	}

	public int GetTileCount()
	{
		XElement collectionElement = GetOrCreateTileCollectionElement();
		return collectionElement.Elements(tileName).Count();
	}

	public async Task FillTilesAsync(TilesetFillSettings settings)
	{
		if (Document is null) throw new InvalidOperationException("SVG Document is not loaded.");
		if (Tileset is null) throw new InvalidOperationException("Tileset is not loaded.");
		XElement collectionElement = GetOrCreateTileCollectionElement();

		int maxRow = Tileset.Size.height / Tileset.TileSize.height - 1;
		int maxCol = Tileset.Size.width / Tileset.TileSize.width - 1;

		foreach (var tileElement in collectionElement.Elements(tileName))
		{
			var tile = TileExtensions.GetTileFromXElement(tileElement);
			if (tile.Row > maxRow) maxRow = tile.Row;
			if (tile.Column > maxCol) maxCol = tile.Column;
		}

		for (int row = 0; row <= maxRow; row++)
		{
			for (int col = 0; col <= maxCol; col++)
			{
				var element = GetTileElement(row, col);
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
						bool isEmpty = await renderingService.IsSegmentEmptyAsync(_svgFile!, col * Tileset.TileSize.width, row * Tileset.TileSize.height,
							(col + 1) * Tileset.TileSize.width, (row + 1) * Tileset.TileSize.height, CancellationToken.None);
						if (isEmpty) continue;
					}

					AddOrReplaceTile(newTile);
				}
			}
		}

		TilesetChanged.Invoke(Tileset!);
	}

	private XElement GetOrCreateAppElement()
	{
		if (Defs is null) throw new Exception("SVG Document isn't loaded or missing <defs> element.");
		var appElement = Defs.Element(appDefsName);
		if (appElement is null)
		{
			appElement = new XElement(appDefsName);
			Defs.Add(appElement);
		}
		return appElement;
	}

	private XElement GetOrCreateTileCollectionElement()
	{
		XElement appElement = GetOrCreateAppElement();
		var collectionElement = appElement.Element(tileCollectionName);
		if (collectionElement is null)
		{
			collectionElement = new XElement(tileCollectionName);
			appElement.Add(collectionElement);
		}
		return collectionElement;
	}

	private XElement? GetTileElement(int row, int col)
	{
		if (Document is null) throw new InvalidOperationException("SVG Document is not loaded.");
		XElement collectionElement = GetOrCreateTileCollectionElement();
		return collectionElement.Elements(tileName)
			.FirstOrDefault(t => t.Attribute(XName.Get("row"))?.Value == row.ToString() && t.Attribute(XName.Get("column"))?.Value == col.ToString());
	}
}
