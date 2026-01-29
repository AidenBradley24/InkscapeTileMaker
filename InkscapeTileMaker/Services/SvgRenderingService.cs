using InkscapeTileMaker.Utility;
using SkiaSharp;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Xml.Linq;

namespace InkscapeTileMaker.Services
{
	public class SvgRenderingService : ITilesetRenderingService
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

		public async Task<Stream> RenderFileAsync(FileInfo file, string extension, CancellationToken cancellationToken = default)
		{
			var requestHash = HashCode.Combine(file.FullName, extension, file.LastWriteTimeUtc);
			CheckAndAddToCache(file, requestHash);

			if (!string.IsNullOrWhiteSpace(extension) && extension.StartsWith('.'))
			{
				extension = extension[1..].ToLowerInvariant();
			}

			var exportType = string.IsNullOrWhiteSpace(extension) ? "png" : extension;

			var renderTask = _renderedFiles.GetOrAdd(requestHash, async _ =>
			{
				var startInfo = _inkscapeService.GetProcessStartInfo();
				var exportFile = new FileInfo(Path.Combine(_tempDirService.TempDir.FullName, $"{Guid.NewGuid()}.{exportType}"));
				startInfo.Arguments = $"--export-type=\"{exportType}\" --export-filename=\"{exportFile.FullName}\" \"{file.FullName}\""
					+ (exportType == "svg" ? " --export-plain-svg" : "");
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

			if (exportType == "svg")
			{
				using var svgStream = exportFile.OpenRead();
				return StripAppSvgData(svgStream);
			}

			return exportFile.OpenRead();
		}

		public async Task<Stream> RenderSegmentAsync(FileInfo file, string extension, int left, int top, int right, int bottom, CancellationToken cancellationToken = default)
		{
			var requestHash = HashCode.Combine(file.FullName, extension, left, top, right, bottom, file.LastWriteTimeUtc);
			CheckAndAddToCache(file, requestHash);

			if (!string.IsNullOrWhiteSpace(extension) && extension.StartsWith('.'))
			{
				extension = extension[1..].ToLowerInvariant();
			}

			var exportType = string.IsNullOrWhiteSpace(extension) ? "png" : extension;

			if (exportType == "svg")
			{
				throw new NotSupportedException("Segment rendering to SVG is not yet supported.");
				// TODO crop SVG content to specified area
				// inkscape export area only works for raster formats
			}

			var renderTask = _renderedFiles.GetOrAdd(requestHash, async _ =>
			{
				var startInfo = _inkscapeService.GetProcessStartInfo();
				var exportFile = new FileInfo(Path.Combine(_tempDirService.TempDir.FullName, $"{Guid.NewGuid()}.{exportType}"));

				startInfo.Arguments =
					$"--export-area={left}:{top}:{right}:{bottom} " +
					$"--export-type=\"{exportType}\" " +
					$"--export-filename=\"{exportFile.FullName}\" \"{file.FullName}\""
						+ (exportType == "svg" ? " --export-plain-svg" : "");

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

			if (exportType == "svg")
			{
				using var svgStream = exportFile.OpenRead();
				return StripAppSvgData(svgStream);
			}

			return exportFile.OpenRead();
		}

		public async Task<bool> IsSegmentEmptyAsync(FileInfo file, int left, int top, int right, int bottom, CancellationToken cancellationToken = default)
		{
			using var pngStream = await RenderSegmentAsync(file, "png", left, top, right, bottom, cancellationToken);
			using var bitmap = SKBitmap.Decode(pngStream) ?? throw new Exception("Failed to decode rendered PNG.");
			for (int y = 0; y < bitmap.Height; y++)
			{
				for (int x = 0; x < bitmap.Width; x++)
				{
					cancellationToken.ThrowIfCancellationRequested();
					var pixel = bitmap.GetPixel(x, y);
					if (pixel.Alpha != 0)
					{
						return false;
					}
				}
			}
			return true;
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

		private static MemoryStream StripAppSvgData(Stream svgStream)
		{
			var document = XDocument.Load(svgStream);
			var root = document.Root ?? throw new Exception("Invalid SVG document.");
			var appDefs = root
				.Element(XName.Get("defs", InkscapeSvg.svgNamespace.NamespaceName))?
				.Element(InkscapeSvg.appDefsName);
			appDefs?.Remove();

			var existingNs = root.GetNamespaceOfPrefix(InkscapeSvg.appNamespacePrefix);
			if (existingNs != null)
			{
				var attr = root.Attributes()
					.FirstOrDefault(a => a.IsNamespaceDeclaration &&
										 a.Name.LocalName == InkscapeSvg.appNamespacePrefix &&
										 a.Value == existingNs.NamespaceName);
				attr?.Remove();
			}

			var outputStream = new MemoryStream();
			document.Save(outputStream);
			outputStream.Position = 0;
			return outputStream;
		}
	}
}
