using System.Xml.Linq;

namespace InkscapeTileMaker.Utility
{
	public class InkscapeSvg
	{
		public XDocument Document { get; }
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

		public InkscapeSvg(Stream srcStream)
		{
			Document = XDocument.Load(srcStream);
			if (SvgRoot is not null)
			{
				var existingNs = SvgRoot.GetNamespaceOfPrefix(appNamespacePrefix);
				if (existingNs == null || existingNs != appNamespace)
				{
					SvgRoot.SetAttributeValue(XNamespace.Xmlns + appNamespacePrefix, appNamespace.NamespaceName);
				}
			}
			srcStream.Dispose();
		}

		public async Task SaveToStreamAsync(Stream stream)
		{
			if (Document is null) return;
			await Document.SaveAsync(stream, SaveOptions.None, CancellationToken.None);
		}

		public XElement? GetTileElement(int row, int col)
		{
			if (Document is null) throw new InvalidOperationException("SVG Document is not loaded.");
			XElement collectionElement = GetOrCreateTileCollectionElement();
			return collectionElement.Elements(tileName)
				.FirstOrDefault(t => t.Attribute(XName.Get("row"))?.Value == row.ToString() && t.Attribute(XName.Get("column"))?.Value == col.ToString());
		}

		public XElement GetOrCreateAppElement()
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

		public XElement GetOrCreateTileCollectionElement()
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

		public IEnumerable<XElement> GetAllTileElements()
		{
			XElement collectionElement = GetOrCreateTileCollectionElement();
			return collectionElement.Elements(tileName);
		}

		public Scale GetTileSize()
		{
			if (Grid is null) throw new InvalidOperationException("Grid not in svg");
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

		public void SetTileSize(Scale size)
		{
			if (Grid is null) throw new InvalidOperationException("Grid not in svg");
			Grid.SetAttributeValue(XName.Get("spacingx"), (size.width / 2).ToString());
			Grid.SetAttributeValue(XName.Get("spacingy"), (size.height / 2).ToString());
			Grid.SetAttributeValue(XName.Get("empspacing"), 2);
		}

		public void SetSvgSize(Scale size)
		{
			if (SvgRoot is null) throw new InvalidOperationException("SVG Document is not loaded.");
			SvgRoot.SetAttributeValue(XName.Get("width"), size.width.ToString());
			SvgRoot.SetAttributeValue(XName.Get("height"), size.height.ToString());
		}
	}
}
