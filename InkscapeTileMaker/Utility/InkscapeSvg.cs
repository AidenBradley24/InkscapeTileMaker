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
	}
}
