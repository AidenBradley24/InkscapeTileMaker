using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;

namespace InkscapeTileMaker.Services
{
	public class SvgRenderingService : ISvgRenderingService
	{
		private readonly IInkscapeService _inkscapeService;
		private readonly ITempDirectoryService _tempDirService;

		private readonly ConcurrentDictionary<int, Task<FileInfo>> _renderedFiles;
		private readonly ConcurrentDictionary<string, ImmutableHashSet<int>> _renderHashes;
		private readonly Dictionary<string, DateTime> _cacheUpdates; // access only in CheckCache

		public SvgRenderingService(IInkscapeService inkscapeService, ITempDirectoryService tempDirService)
		{
			_inkscapeService = inkscapeService;
			_tempDirService = tempDirService;
			_renderedFiles = new ConcurrentDictionary<int, Task<FileInfo>>();
			_renderHashes = new ConcurrentDictionary<string, ImmutableHashSet<int>>();
			_cacheUpdates = new Dictionary<string, DateTime>();
		}

		public async Task<Stream> RenderSvgFile(FileInfo svgFile, CancellationToken cancellationToken)
		{
			var requestHash = HashCode.Combine(svgFile.FullName, svgFile.LastWriteTimeUtc);
			CheckAndAddToCache(svgFile, requestHash);

			var renderTask = _renderedFiles.GetOrAdd(requestHash, async _ =>
			{
				var startInfo = _inkscapeService.GetProcessStartInfo();
				var exportFile = new FileInfo(Path.Combine(_tempDirService.TempDir.FullName, $"{Guid.NewGuid()}.png"));
				startInfo.Arguments = $"--export-type=\"png\" --export-filename=\"{exportFile.FullName}\" \"{svgFile.FullName}\"";
				var process = Process.Start(startInfo) ?? throw new Exception("Failed to start Inkscape process.");
				try
				{
					await process.WaitForExitAsync(cancellationToken);
				}
				catch (OperationCanceledException) when (!process.HasExited)
				{
					try { process.Kill(entireProcessTree: true); } catch { }
					throw;
				}

				if (process.ExitCode != 0)
					throw new Exception($"Inkscape failed with exit code {process.ExitCode}.");

				if (!exportFile.Exists)
					throw new Exception("Exported file doesn't exist!");

				return exportFile;
			});

			var exportFile = await renderTask.ConfigureAwait(false);
			cancellationToken.ThrowIfCancellationRequested();
			return exportFile.OpenRead();
		}

		public async Task<Stream> RenderSvgSegment(FileInfo svgFile, int left, int top, int right, int bottom, CancellationToken cancellationToken)
		{
			var requestHash = HashCode.Combine(svgFile.FullName, left, top, right, bottom, svgFile.LastWriteTimeUtc);
			CheckAndAddToCache(svgFile, requestHash);

			var renderTask = _renderedFiles.GetOrAdd(requestHash, async _ =>
			{
				var startInfo = _inkscapeService.GetProcessStartInfo();
				var exportFile = new FileInfo(Path.Combine(_tempDirService.TempDir.FullName, $"{Guid.NewGuid()}.png"));

				startInfo.Arguments =
					$"--export-area={left}:{top}:{right}:{bottom} " +
					$"--export-type=\"png\" " +
					$"--export-filename=\"{exportFile.FullName}\" \"{svgFile.FullName}\"";

				var process = Process.Start(startInfo) ?? throw new Exception("Failed to start Inkscape process.");
				try
				{
					await process.WaitForExitAsync(cancellationToken);
				}
				catch (OperationCanceledException) when (!process.HasExited)
				{
					try { process.Kill(entireProcessTree: true); } catch { }
					throw;
				}

				if (process.ExitCode != 0)
					throw new Exception($"Inkscape failed with exit code {process.ExitCode}.");

				if (!exportFile.Exists)
					throw new Exception("Exported file doesn't exist!");

				return exportFile;
			});

			var exportFile = await renderTask.ConfigureAwait(false);
			cancellationToken.ThrowIfCancellationRequested();
			return exportFile.OpenRead();
		}

		private void CheckAndAddToCache(FileInfo svgFile, int requestHash)
		{
			// remove failed or canceled tasks
			_renderedFiles.TryGetValue(requestHash, out var existingTask);
			if (existingTask != null && (existingTask.IsFaulted || existingTask.IsCanceled))
			{
				_renderedFiles.TryRemove(requestHash, out _);
			}

			// check for file updates
			List<FileInfo>? filesToDelete = null;
			lock (_cacheUpdates)
			{
				if (_cacheUpdates.TryGetValue(svgFile.FullName, out var lastUpdate) &&
					lastUpdate == svgFile.LastWriteTimeUtc)
				{
					AddHashToCache(svgFile, requestHash);
					return;
				}

				_cacheUpdates[svgFile.FullName] = svgFile.LastWriteTimeUtc;
				if (_renderHashes.TryRemove(svgFile.FullName, out var hashes))
				{
					foreach (var hash in hashes)
					{
						if (_renderedFiles.TryRemove(hash, out var renderTask) && renderTask.IsCompletedSuccessfully)
						{
							filesToDelete ??= [];
							filesToDelete.Add(renderTask.Result);
						}
					}
				}

				AddHashToCache(svgFile, requestHash);
			}
			
			if (filesToDelete == null) return;
			foreach (var file in filesToDelete)
			{
				try
				{
					file.Delete();
				}
				catch
				{
					// best-effort cleanup
				}
			}
		}

		private void AddHashToCache(FileInfo svgFile, int requestHash)
		{
			_renderHashes.AddOrUpdate(svgFile.FullName, _ => [requestHash], (_, list) =>
			{
				if (list.Contains(requestHash)) return list;
				return list.Add(requestHash);
			});
		}
	}
}
