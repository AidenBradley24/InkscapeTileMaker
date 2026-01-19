using InkscapeTileMaker.ViewModels;
using System.Xml.Linq;

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

	public TileViewModel GetTile(int x, int y)
	{
		if (Document is null) throw new InvalidOperationException("SVG Document is not loaded.");

		XElement collectionElement = GetOrCreateTileCollectionElement() ?? throw new InvalidOperationException("Unable to get or create tile collection element in SVG.");
		XElement? tileElement = collectionElement.Element(XName.Get($"tile-{x}-{y}", appNamespace.NamespaceName));
		if (tileElement is null)
		{
			tileElement = new XElement(XName.Get($"tile-{x}-{y}", appNamespace.NamespaceName));
			collectionElement.Add(tileElement);
		}
		var tileWrapper = new TileViewModel(tileElement, collectionElement);
		return tileWrapper;
	}

	public IEnumerable<TileViewModel> GetAllTiles()
	{
		if (Document is null) throw new InvalidOperationException("SVG Document is not loaded.");
		XElement collectionElement = GetOrCreateTileCollectionElement() ?? throw new InvalidOperationException("Unable to get or create tile collection element in SVG.");
		return collectionElement
			.Elements(XName.Get("tile", appNamespace.NamespaceName))
			.Select(tileElement => new TileViewModel(tileElement, collectionElement));
	}
}
