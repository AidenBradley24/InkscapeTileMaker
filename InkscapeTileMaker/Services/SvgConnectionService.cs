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

	public event Action<XDocument> DocumentLoaded = delegate { };

	static readonly XNamespace appNamespace = "https://github.com/AidenBradley24/InkscapeTileMaker";

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
	
}
