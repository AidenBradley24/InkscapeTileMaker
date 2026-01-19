using System.Collections.Concurrent;
using System.Diagnostics;

namespace InkscapeTileMaker.Services
{
	public class SvgRenderingService : ISvgRenderingService
	{
		private readonly IInkscapeService _inkscapeService;
		private readonly ITempDirectoryService _tempDirService;

		private readonly ConcurrentDictionary<int, FileInfo> _renderedFiles;

		public SvgRenderingService(IInkscapeService inkscapeService, ITempDirectoryService tempDirService)
		{
			_inkscapeService = inkscapeService;
			_tempDirService = tempDirService;
			_renderedFiles = new ConcurrentDictionary<int, FileInfo>();
		}

		public async Task<Stream> RenderSvgFile(FileInfo svgFile, CancellationToken cancellationToken)
		{
			var requestHash = HashCode.Combine(svgFile.FullName, svgFile.LastWriteTimeUtc);
			if (_renderedFiles.TryGetValue(requestHash, out var cachedFile)) return cachedFile.OpenRead();
			var startInfo = _inkscapeService.GetProcessStartInfo();
			FileInfo exportFile = new FileInfo(Path.Combine(_tempDirService.TempDir.FullName, $"{Guid.NewGuid()}.png"));
			startInfo.Arguments = $"--export-type=\"png\" --export-filename=\"{exportFile.FullName}\" \"{svgFile.FullName}\"";
			var process = Process.Start(startInfo) ?? throw new Exception("Failed to start Inkscape process.");
			await process.WaitForExitAsync(cancellationToken);
			if (!exportFile.Exists) throw new Exception("Failed svg rendering.");
			cancellationToken.ThrowIfCancellationRequested();
			CacheResult(requestHash, exportFile);
			return exportFile.OpenRead();
		}

		public async Task<Stream> RenderSvgSegment(FileInfo svgFile, int left, int top, int right, int bottom, CancellationToken cancellationToken)
		{
			var requestHash = HashCode.Combine(svgFile, left, top, right, bottom, svgFile.LastWriteTimeUtc);
			if (_renderedFiles.TryGetValue(requestHash, out var cachedFile)) return cachedFile.OpenRead();
			var startInfo = _inkscapeService.GetProcessStartInfo();
			FileInfo exportFile = new FileInfo(Path.Combine(_tempDirService.TempDir.FullName, $"{Guid.NewGuid()}.png"));
			startInfo.Arguments = $"--export-area={left}:{top}:{right}:{bottom} --export-type=\"png\" --export-filename=\"{exportFile.FullName}\" \"{svgFile.FullName}\"";
			var process = Process.Start(startInfo) ?? throw new Exception("Failed to start Inkscape process.");
			await process.WaitForExitAsync(cancellationToken);
			cancellationToken.ThrowIfCancellationRequested();
			if (!exportFile.Exists) throw new Exception("Failed svg rendering.");
			CacheResult(requestHash, exportFile);
			return exportFile.OpenRead();
		}

		private void CacheResult(int requestHash, FileInfo renderedFile)
		{
			_renderedFiles[requestHash] = renderedFile;
		}
	}
}
