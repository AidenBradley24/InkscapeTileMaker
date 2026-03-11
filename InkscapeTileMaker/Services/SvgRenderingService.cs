using InkscapeTileMaker.Models;
using InkscapeTileMaker.Utility;
using SkiaSharp;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Xml.Linq;

namespace InkscapeTileMaker.Services
{
	public sealed partial class SvgRenderingService : ITilesetRenderingService, IAsyncDisposable
	{
		private readonly IInkscapeService _inkscapeService;
		private readonly ITempDirectoryService _tempDirService;

		private readonly ConcurrentDictionary<int, Task<FileInfo>> _renderedFiles;
		private readonly ConcurrentDictionary<string, ImmutableHashSet<int>> _renderHashes;
		private readonly Dictionary<string, DateTime> _cacheUpdates; // access only in CheckCache

		private readonly SemaphoreSlim _fileHashSemaphore;
		private readonly CancellationTokenSource _disposeCancellation = new();
		private readonly TaskCompletionSource<object?> _disposeCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);

		private const int ACTIVE = 1;
		private const int DISPOSAL = 2;

		private int _disposeState = ACTIVE;
		private int _activeOperationCount = 0;

		public SvgRenderingService(IInkscapeService inkscapeService, ITempDirectoryService tempDirService)
		{
			_inkscapeService = inkscapeService;
			_tempDirService = tempDirService;
			_renderedFiles = new ConcurrentDictionary<int, Task<FileInfo>>();
			_renderHashes = new ConcurrentDictionary<string, ImmutableHashSet<int>>();
			_cacheUpdates = new Dictionary<string, DateTime>();
			_fileHashSemaphore = new SemaphoreSlim(1, 1);
		}

		private void ThrowIfDisposed()
		{
			bool disposed = Volatile.Read(ref _disposeState) == DISPOSAL;
			ObjectDisposedException.ThrowIf(disposed, this);
		}

		private void BeginOperation()
		{
			ThrowIfDisposed();

			Interlocked.Increment(ref _activeOperationCount);

			if (Volatile.Read(ref _disposeState) == DISPOSAL)
			{
				EndOperation();
				throw new ObjectDisposedException(nameof(SvgRenderingService));
			}
		}

		private void EndOperation()
		{
			if (Interlocked.Decrement(ref _activeOperationCount) == 0 &&
				Volatile.Read(ref _disposeState) == DISPOSAL)
			{
				_disposeCompletion.TrySetResult(null);
			}
		}

		public async Task<Stream> RenderFileAsync(
			FileInfo file,
			string extension,
			CancellationToken cancellationToken = default)
		{
			BeginOperation();
			try
			{
				var exportType = NormalizeExportType(extension);
				var requestHash = HashCode.Combine(
					file.FullName,
					exportType,
					await ComputeFileHashAsync(file, cancellationToken).ConfigureAwait(false));

				CheckAndAddToCache(file, requestHash);

				var renderTask = _renderedFiles.GetOrAdd(
					requestHash,
					_ => RenderFileCoreAsync(file, exportType, _disposeCancellation.Token));

				var exportFile = await renderTask.WaitAsync(cancellationToken).ConfigureAwait(false);

				if (exportType == "svg")
				{
					using var svgStream = exportFile.OpenRead();
					return StripAppSvgData(svgStream);
				}

				return exportFile.OpenRead();
			}
			finally
			{
				EndOperation();
			}
		}

		private async Task<FileInfo> RenderFileCoreAsync(FileInfo file, string exportType, CancellationToken cancellationToken)
		{
			var startInfo = _inkscapeService.GetProcessStartInfo();
			var exportFile = new FileInfo(Path.Combine(_tempDirService.TempDir.FullName, $"{Guid.NewGuid()}.{exportType}"));
			startInfo.Arguments = $"--export-type=\"{exportType}\" --export-filename=\"{exportFile.FullName}\" \"{file.FullName}\""
				+ (exportType == "svg" ? " --export-plain-svg" : "");

			var process = Process.Start(startInfo) ?? throw new Exception("Failed to start Inkscape process.");
			try
			{
				await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
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
		}

		public async Task<Stream> RenderSegmentAsync(
			FileInfo file,
			string extension,
			int left,
			int top,
			int right,
			int bottom,
			Scale? exportScale = null,
			CancellationToken cancellationToken = default)
		{
			BeginOperation();
			try
			{
				var exportType = NormalizeExportType(extension);

				if (exportType == "svg")
				{
					throw new NotSupportedException("Segment rendering to SVG is not yet supported.");
					// TODO crop SVG content to specified area
					// inkscape export area only works for raster formats
				}

				var requestHash = HashCode.Combine(
					file.FullName,
					exportType,
					left, top, right, bottom,
					exportScale,
					await ComputeFileHashAsync(file, cancellationToken).ConfigureAwait(false));

				CheckAndAddToCache(file, requestHash);

				var renderTask = _renderedFiles.GetOrAdd(
					requestHash,
					_ => RenderSegmentCoreAsync(file, exportType, left, top, right, bottom, exportScale, _disposeCancellation.Token));

				var exportFile = await renderTask.WaitAsync(cancellationToken).ConfigureAwait(false);
				return exportFile.OpenRead();
			}
			finally
			{
				EndOperation();
			}
		}

		private async Task<FileInfo> RenderSegmentCoreAsync(
			FileInfo file,
			string exportType,
			int left,
			int top,
			int right,
			int bottom,
			Scale? exportScale,
			CancellationToken cancellationToken)
		{
			var startInfo = _inkscapeService.GetProcessStartInfo();
			var exportFile = new FileInfo(Path.Combine(_tempDirService.TempDir.FullName, $"{Guid.NewGuid()}.{exportType}"));

			startInfo.Arguments =
				$"--export-area={left}:{top}:{right}:{bottom} " +
				$"--export-type=\"{exportType}\" " +
				$"--export-filename=\"{exportFile.FullName}\" \"{file.FullName}\""
					+ (exportType == "svg" ? " --export-plain-svg" : "")
					+ (exportScale.HasValue ? $" --export-width={exportScale.Value.Width} --export-height={exportScale.Value.Height}" : "");

			var process = Process.Start(startInfo) ?? throw new Exception("Failed to start Inkscape process.");
			try
			{
				await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
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
		}

		public async Task<bool> IsSegmentEmptyAsync(FileInfo file, int left, int top, int right, int bottom, CancellationToken cancellationToken = default)
		{
			BeginOperation();
			try
			{
				using var pngStream = await RenderSegmentAsync(
					file,
					"png",
					left, top, right, bottom,
					new Scale(16, 16),
					cancellationToken)
					.ConfigureAwait(false);

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
			finally
			{
				EndOperation();
			}
		}

		private void CheckAndAddToCache(FileInfo svgFile, int requestHash)
		{
			_renderedFiles.TryGetValue(requestHash, out var existingTask);
			if (existingTask != null && (existingTask.IsFaulted || existingTask.IsCanceled))
			{
				_renderedFiles.TryRemove(requestHash, out _);
			}

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
				.Element(XName.Get("defs", InkscapeSvg.SvgNamespace.NamespaceName))?
				.Element(InkscapeSvg.AppDefsName);
			appDefs?.Remove();

			var existingNs = root.GetNamespaceOfPrefix(InkscapeSvg.AppNamespacePrefix);
			if (existingNs != null)
			{
				var attr = root.Attributes()
					.FirstOrDefault(a => a.IsNamespaceDeclaration &&
										 a.Name.LocalName == InkscapeSvg.AppNamespacePrefix &&
										 a.Value == existingNs.NamespaceName);
				attr?.Remove();
			}

			var outputStream = new MemoryStream();
			document.Save(outputStream);
			outputStream.Position = 0;
			return outputStream;
		}

		private async Task<int> ComputeFileHashAsync(FileInfo file, CancellationToken cancellationToken)
		{
			byte[]? hashBytes = null;
			using var hashAlg = SHA256.Create();

			await _fileHashSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
			try
			{
				for (int i = 0; i < 5; i++)
				{
					cancellationToken.ThrowIfCancellationRequested();

					try
					{
						using var stream = file.OpenRead();
						hashBytes = await hashAlg.ComputeHashAsync(stream, cancellationToken).ConfigureAwait(false);
						break;
					}
					catch (OperationCanceledException)
					{
						throw;
					}
					catch when (i < 4)
					{
						await Task.Delay(100 + i * 200, cancellationToken).ConfigureAwait(false);
					}
				}
			}
			finally
			{
				_fileHashSemaphore.Release();
			}

			if (hashBytes == null)
			{
				throw new Exception("Failed to compute file hash after multiple attempts.");
			}

			return BitConverter.ToInt32(hashBytes, 0);
		}

		private static string NormalizeExportType(string extension)
		{
			if (string.IsNullOrWhiteSpace(extension))
			{
				return "png";
			}

			extension = extension.Trim();

			if (extension.StartsWith('.'))
			{
				extension = extension[1..];
			}

			return extension.ToLowerInvariant();
		}

		public void Dispose()
		{
			DisposeAsync().AsTask().GetAwaiter().GetResult();
		}

		public async ValueTask DisposeAsync()
		{
			GC.SuppressFinalize(this);

			if (Interlocked.Exchange(ref _disposeState, DISPOSAL) != ACTIVE)
			{
				return;
			}

			_disposeCancellation.Cancel();

			if (Volatile.Read(ref _activeOperationCount) != 0)
			{
				await _disposeCompletion.Task.ConfigureAwait(false);
			}

			await AwaitTrackedRenderTasksAsync().ConfigureAwait(false);
			DeleteCachedFiles();

			_disposeCancellation.Dispose();
			_fileHashSemaphore.Dispose();
		}

		private async Task AwaitTrackedRenderTasksAsync()
		{
			if (_renderedFiles.IsEmpty)
			{
				return;
			}

			var renderTasks = new Task<FileInfo>[_renderedFiles.Count];
			_renderedFiles.Values.CopyTo(renderTasks, 0);

			try
			{
				await Task.WhenAll(renderTasks).ConfigureAwait(false);
			}
			catch
			{
				// best-effort drain before cleanup
			}
		}

		private void DeleteCachedFiles()
		{
			foreach (var task in _renderedFiles.Values)
			{
				if (!task.IsCompletedSuccessfully)
				{
					continue;
				}

				try
				{
					task.Result.Delete();
				}
				catch
				{
					// best-effort cleanup
				}
			}

			_renderedFiles.Clear();
			_renderHashes.Clear();

			lock (_cacheUpdates)
			{
				_cacheUpdates.Clear();
			}
		}
	}
}
