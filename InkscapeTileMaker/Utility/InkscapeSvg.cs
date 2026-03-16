using InkscapeTileMaker.Models;
using System.Xml.Linq;

namespace InkscapeTileMaker.Utility
{
	public class InkscapeSvg
	{
		public XDocument Document { get; }
		public XElement? SvgRoot => Document?.Root;
		public XElement? NamedView => SvgRoot?.Element(XName.Get("namedview", SodipodiNamespace.NamespaceName));
		public XElement? Grid => NamedView?.Element(XName.Get("grid", InkscapeNamespace.NamespaceName));
		public XElement? Defs => SvgRoot?.Element(XName.Get("defs", SvgNamespace.NamespaceName));

		public const string AppNamespacePrefix = "tilemaker";

		public static readonly XNamespace AppNamespace = "https://github.com/AidenBradley24/InkscapeTileMaker";
		public static readonly XNamespace InkscapeNamespace = "http://www.inkscape.org/namespaces/inkscape";
		public static readonly XNamespace SodipodiNamespace = "http://sodipodi.sourceforge.net/DTD/sodipodi-0.dtd";
		public static readonly XNamespace SvgNamespace = "http://www.w3.org/2000/svg";

		public static readonly XName AppDefsName = AppNamespace + "tilemakerdefs";
		public static readonly XName TileCollectionName = AppNamespace + "tiles";
		public static readonly XName TileName = AppNamespace + "tile";

		public InkscapeSvg(Stream srcStream)
		{
			Document = XDocument.Load(srcStream);
			if (SvgRoot is not null)
			{
				var existingNs = SvgRoot.GetNamespaceOfPrefix(AppNamespacePrefix);
				if (existingNs == null || existingNs != AppNamespace)
				{
					SvgRoot.SetAttributeValue(XNamespace.Xmlns + AppNamespacePrefix, AppNamespace.NamespaceName);
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
			return collectionElement.Elements(TileName)
				.FirstOrDefault(t => t.Attribute(XName.Get("row"))?.Value == row.ToString() && t.Attribute(XName.Get("column"))?.Value == col.ToString());
		}

		public XElement GetOrCreateAppElement()
		{
			if (Defs is null) throw new Exception("SVG Document isn't loaded or missing <defs> element.");
			var appElement = Defs.Element(AppDefsName);
			if (appElement is null)
			{
				appElement = new XElement(AppDefsName);
				Defs.Add(appElement);
			}
			return appElement;
		}

		public XElement GetOrCreateTileCollectionElement()
		{
			XElement appElement = GetOrCreateAppElement();
			var collectionElement = appElement.Element(TileCollectionName);
			if (collectionElement is null)
			{
				collectionElement = new XElement(TileCollectionName);
				appElement.Add(collectionElement);
			}
			return collectionElement
				?? throw new InvalidOperationException("Unable to get or create tile collection element in SVG.");
		}

		public IEnumerable<XElement> GetAllTileElements()
		{
			XElement collectionElement = GetOrCreateTileCollectionElement();
			return collectionElement.Elements(TileName);
		}

		public Scale GetTileSize()
		{
			if (Grid is null) throw new InvalidOperationException("Grid not in svg");
			var spacingX = Grid.Attribute(XName.Get("spacingx"))?.Value ?? "4";
			var spacingY = Grid.Attribute(XName.Get("spacingy"))?.Value ?? "4";
			var empSpacing = Grid.Attribute(XName.Get("empspacing"))?.Value ?? "2";
			if (int.TryParse(spacingX, out int width) && int.TryParse(spacingY, out int height) && int.TryParse(empSpacing, out int unitsPerTile))
			{
				return new Scale(width * unitsPerTile, height * unitsPerTile);
			}

			throw new InvalidDataException("Tile size information is missing or invalid in the SVG grid.");
		}

		public Scale GetSvgSize()
		{
			int width = Convert.ToInt32(SvgRoot?.Attribute(XName.Get("width"))?.Value ?? "1");
			int height = Convert.ToInt32(SvgRoot?.Attribute(XName.Get("height"))?.Value ?? "1");
			return new Scale(width, height);
		}

		public void SetTileSize(Scale size)
		{
			if (Grid is null) throw new InvalidOperationException("Grid not in svg");
			Grid.SetAttributeValue(XName.Get("spacingx"), (size.Width / 2).ToString());
			Grid.SetAttributeValue(XName.Get("spacingy"), (size.Height / 2).ToString());
			Grid.SetAttributeValue(XName.Get("empspacing"), 2);
		}

		public void SetSvgSize(Scale size)
		{
			if (SvgRoot is null) throw new InvalidOperationException("SVG Document is not loaded.");
			SvgRoot.SetAttributeValue(XName.Get("width"), size.Width.ToString());
			SvgRoot.SetAttributeValue(XName.Get("height"), size.Height.ToString());
		}

		public InkscapeSvg Clone()
		{
			if (Document is null) throw new InvalidOperationException("SVG Document is not loaded.");
			using var ms = new MemoryStream();
			Document.Save(ms);
			ms.Position = 0;
			return new InkscapeSvg(ms);
		}

		public InkscapeSvg Crop(Models.Rect rect)
		{
			if (Defs is null) throw new Exception("SVG Document isn't loaded or missing <defs> element.");

			var clone = Clone();
			var clip = new XElement(XName.Get("{svg}clipPath"), new XAttribute(XName.Get("id"), "crop"));
			var cropRect = new XElement(XName.Get("{svg}rect"),
				new XAttribute(XName.Get("x"), rect.Left),
				new XAttribute(XName.Get("y"), rect.Top),
				new XAttribute(XName.Get("width"), rect.Width),
				new XAttribute(XName.Get("height"), rect.Height));
			clip.Add(cropRect);
			clone.Defs!.Add(clip);

			clone.SvgRoot!.SetAttributeValue("width", rect.Width);
			clone.SvgRoot.SetAttributeValue("height", rect.Height);
			clone.SvgRoot.SetAttributeValue("viewBox", $"{rect.Left} {rect.Top} {rect.Width} {rect.Height}");

			foreach (var element in clone.SvgRoot.Descendants())
			{
				if (element.Name.LocalName != "g") continue; // Only apply clipping to groups, to avoid interfering with defs and other elements
				var existingClip = element.Attribute(XName.Get("clip-path"))?.Value;
				if (existingClip != null)
				{
					element.SetAttributeValue(XName.Get("clip-path"), $"url(#crop) {existingClip}");
				}
				else
				{
					element.SetAttributeValue(XName.Get("clip-path"), "url(#crop)");
				}
			}

			return clone;
		}
	}
}
