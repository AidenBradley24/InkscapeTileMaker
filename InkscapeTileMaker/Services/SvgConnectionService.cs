using System;
using System.Collections.Generic;
using System.Text;
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

	public event Action<XDocument> DocumentLoaded = delegate { };

	public static readonly XNamespace appNamespace = "https://github.com/AidenBradley24/InkscapeTileMaker";
	public static readonly XNamespace inkscapeNamespace = "http://www.inkscape.org/namespaces/inkscape";
	public static readonly XNamespace sodipodiNamespace = "http://sodipodi.sourceforge.net/DTD/sodipodi-0.dtd";

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
}
