using System.Diagnostics;

namespace InkscapeTileMaker.Services
{
	public class SvgRenderingService : ISvgRenderingService
	{
		private readonly IInkscapeService _inkscapeService;
		private readonly ITempDirectoryService _tempDirService;

		public SvgRenderingService(IInkscapeService inkscapeService, ITempDirectoryService tempDirService)
		{
			_inkscapeService = inkscapeService;
			_tempDirService = tempDirService;
		}

		public async Task<Stream> RenderSvgFile(FileInfo svgFile)
		{
			// open file in inkscape
			var startInfo = _inkscapeService.GetProcessStartInfo();
			FileInfo exportFile = new FileInfo(Path.Combine(_tempDirService.TempDir.FullName, $"{Guid.NewGuid()}.png"));
			startInfo.Arguments = $"--export-type=png --export-filename={exportFile.FullName} {svgFile.FullName}";

			// start inkscape process
			var process = Process.Start(startInfo) ?? throw new Exception("Failed to start Inkscape process.");

			// wait for process to exit
			await process.WaitForExitAsync();

			if (!exportFile.Exists) throw new Exception("Failed svg rendering.");

			// return the exported file stream
			return exportFile.OpenRead();
		}
	}
}
