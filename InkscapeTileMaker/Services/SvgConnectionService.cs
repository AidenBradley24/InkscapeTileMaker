using InkscapeTileMaker.Models;
using InkscapeTileMaker.ViewModels;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace InkscapeTileMaker.Services;

public partial class SvgConnectionService : IDisposable
{
	private FileInfo? _svgFile;
	public FileInfo? SvgFile => _svgFile;

	public XDocument? Document { get; private set; }
	public XElement? SvgRoot => Document?.Root;
	public XElement? NamedView => SvgRoot?.Element(XName.Get("namedview", sodipodiNamespace.NamespaceName));
	public XElement? Grid => NamedView?.Element(XName.Get("grid", inkscapeNamespace.NamespaceName));
	public XElement? Defs => SvgRoot?.Element(XName.Get("defs", svgNamespace.NamespaceName));

	public (int width, int height)? TileSize
	{
		get
		{
			if (Grid is null) return null;
			var spacingX = Grid.Attribute(XName.Get("spacingx"))?.Value;
			var spacingY = Grid.Attribute(XName.Get("spacingy"))?.Value;
			var empSpacing = Grid.Attribute(XName.Get("empspacing"))?.Value;
			if (int.TryParse(spacingX, out int width) && int.TryParse(spacingY, out int height) && int.TryParse(empSpacing, out int unitsPerTile))
			{
				return (width * unitsPerTile, height * unitsPerTile);
			}
			return null;
		}
	}

	public event Action<XDocument> DocumentLoaded = delegate { };

	public const string appNamespacePrefix = "tilemaker";
	public static readonly XNamespace appNamespace = "https://github.com/AidenBradley24/InkscapeTileMaker";
	public static readonly XNamespace inkscapeNamespace = "http://www.inkscape.org/namespaces/inkscape";
	public static readonly XNamespace sodipodiNamespace = "http://sodipodi.sourceforge.net/DTD/sodipodi-0.dtd";
	public static readonly XNamespace svgNamespace = "http://www.w3.org/2000/svg";

	public void LoadSvg(FileInfo svgFile)
	{
		_svgFile = svgFile;
		Document = XDocument.Load(svgFile.FullName);
		DocumentLoaded.Invoke(Document);
	}

	public void Dispose()
	{
		GC.SuppressFinalize(this);
		Document = null;
	}

	public void SaveSvg()
	{
		if (_svgFile is null || Document is null) return;
		Document.Save(_svgFile.FullName);
		DocumentLoaded.Invoke(Document);
	}

	public void SaveSvg(FileInfo saveLocation)
	{
		if (_svgFile is null || Document is null) return;
		Document.Save(saveLocation.FullName);
		DocumentLoaded.Invoke(Document);
	}

	public XElement? GetOrCreateAppElement()
	{
		if (Defs is null) return null;
		var appElement = Defs.Element(XName.Get(appNamespacePrefix, appNamespace.NamespaceName));
		if (appElement is null)
		{
			appElement = new XElement(XName.Get(appNamespacePrefix, appNamespace.NamespaceName));
			Defs.Add(appElement);
		}
		return appElement;
	}

	public XElement? GetOrCreateTileCollectionElement()
	{
		XElement appElement = GetOrCreateAppElement() ?? throw new InvalidOperationException("Unable to get or create application element in SVG.");
		var collectionElement = appElement.Element(XName.Get("tiles", appNamespace.NamespaceName));
		if (collectionElement is null)
		{
			collectionElement = new XElement(XName.Get("tiles", appNamespace.NamespaceName));
			appElement.Add(collectionElement);
		}
		return collectionElement;
	}

	private XElement? GetTileElement(int row, int col)
	{
		if (Document is null) throw new InvalidOperationException("SVG Document is not loaded.");

		XElement collectionElement = GetOrCreateTileCollectionElement() ?? throw new InvalidOperationException("Unable to get or create tile collection element in SVG.");
		return collectionElement.Elements(XName.Get($"tile", appNamespace.NamespaceName))
			.FirstOrDefault(t => t.Attribute(XName.Get("row"))?.Value == row.ToString() && t.Attribute(XName.Get("column"))?.Value == col.ToString()); ;
	}

	public TileViewModel? GetTile(int row, int col, DesignerViewModel designerViewModel)
	{
		var element = GetTileElement(row, col);
		if (element is null) return null;
		XElement collectionElement = GetOrCreateTileCollectionElement()!;
		return new TileViewModel(element, collectionElement, designerViewModel);
	}

	public IEnumerable<TileViewModel> GetAllTiles(DesignerViewModel designerViewModel)
	{
		if (Document is null) throw new InvalidOperationException("SVG Document is not loaded.");
		XElement collectionElement = GetOrCreateTileCollectionElement() ?? throw new InvalidOperationException("Unable to get or create tile collection element in SVG.");
		return collectionElement
			.Elements(XName.Get("tile", appNamespace.NamespaceName))
			.Select(tileElement => new TileViewModel(tileElement, collectionElement, designerViewModel));
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
		XmlSerializer serializer = new(typeof(Tile));
		using (var writer = new StringWriter())
		{
			serializer.Serialize(writer, tile);
			element = XElement.Parse(writer.ToString());
		}
		collectionElement.Add(element);
		DocumentLoaded.Invoke(Document);
		return true;
	}

	public bool RemoveTile(int row, int col)
	{
		if (Document is null) throw new InvalidOperationException("SVG Document is not loaded.");
		XElement collectionElement = GetOrCreateTileCollectionElement() ?? throw new InvalidOperationException("Unable to get or create tile collection element in SVG.");
		var element = collectionElement.Element(XName.Get($"tile", appNamespace.NamespaceName));
		if (element is null)
		{
			// Tile does not exist
			return false;
		}
		element.Remove();
		DocumentLoaded.Invoke(Document);
		return true;
	}

	public void FillTiles()
	{
		if (Document is null) throw new InvalidOperationException("SVG Document is not loaded.");
		XElement collectionElement = GetOrCreateTileCollectionElement() ?? throw new InvalidOperationException("Unable to get or create tile collection element in SVG.");
		if (TileSize is null) throw new InvalidOperationException("Tile size is not defined in the SVG grid.");

		int svgWidth = Convert.ToInt32(SvgRoot?.Attribute(XName.Get("width"))?.Value ?? "0");
		int svgHeight = Convert.ToInt32(SvgRoot?.Attribute(XName.Get("height"))?.Value ?? "0");
		if (svgWidth <= 0 || svgHeight <= 0) throw new InvalidOperationException("SVG width or height is not defined.");

		int maxRow = svgWidth / TileSize.Value.height - 1;
		int maxCol = svgHeight / TileSize.Value.width - 1;

		foreach (var tileElement in collectionElement.Elements(XName.Get("tile", appNamespace.NamespaceName)))
		{
			var serializer = new XmlSerializer(typeof(Tile));
			using var reader = tileElement.CreateReader();
			var tile = (Tile)serializer.Deserialize(reader)!;
			if (tile.Row > maxRow) maxRow = tile.Row;
			if (tile.Column > maxCol) maxCol = tile.Column;
		}

		// Fill in missing tiles
		for (int row = 0; row <= maxRow; row++)
		{
			for (int col = 0; col <= maxCol; col++)
			{
				var element = GetTileElement(row, col);
				if (element is null)
				{
					var newTile = new Tile
					{
						Name = $"Tile {col},{row}",
						Type = TileType.Singular,
						Rotation = RotationAlignment.None,
						Row = row,
						Column = col
					};
					AddTile(newTile);
				}
			}
		}
		DocumentLoaded.Invoke(Document);
	}
}
