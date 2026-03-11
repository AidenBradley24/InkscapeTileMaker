using InkscapeTileMaker.Models;
using InkscapeTileMaker.Utility;
using System.Diagnostics;
using System.Xml.Linq;

namespace InkscapeTileMaker.Services;

public partial class InkscapeSvgConnection : ITilesetConnection
{
	private readonly IServiceProvider _services;
	private IWindowProvider? _windowProvider;

	private FileInfo? _file;

	public ITileset? Tileset { get; private set; }

	public ITilesetRenderingService RenderingService { get; }

	public FileInfo? CurrentFile => _file;

	public event Action<ITilesetConnection> TilesetChanged = delegate { };

	private readonly AsyncReaderWriterLock _stateLock = new(); 
	private readonly TaskCompletionSource<object?> _disposeCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);

	private InkscapeSvg? _svg;
	private FileSystemWatcher? _fileWatcher;
	const int ACTIVE = 1, DISPOSAL = 2;
	private int _disposeState = ACTIVE;
	private int _activeOperationCount = 0;

	public InkscapeSvgConnection(IServiceProvider services, IWindowProvider windowProvider)
	{
		_services = services;
		_windowProvider = windowProvider;

		var inkscapeService = services.GetRequiredService<IInkscapeService>();
		var tmpDirService = services.GetRequiredService<ITempDirectoryService>();
		RenderingService = new SvgRenderingService(inkscapeService, tmpDirService);
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

	private bool TryBeginOperation()
	{
		if (Volatile.Read(ref _disposeState) == DISPOSAL)
		{
			EndOperation();
			return false;
		}

		Interlocked.Increment(ref _activeOperationCount);
		return true;
	}

	private void EndOperation()
	{
		if (Interlocked.Decrement(ref _activeOperationCount) == 0 &&
			Volatile.Read(ref _disposeState) == DISPOSAL)
		{
			_disposeCompletion.TrySetResult(null);
		}
	}

	public async Task LoadAsync(FileInfo file)
	{
		BeginOperation();
		var stateLock = await _stateLock.EnterWriteLockAsync();

		try
		{
			await LoadInternalAsync(file);
		}
		catch (Exception ex)
		{
			Trace.TraceError("An unknown error occured during loading!");
			Trace.TraceError(ex.Message);
		}
		finally
		{
			stateLock.Dispose();
			EndOperation();
		}
	}

	private async Task LoadInternalAsync(FileInfo file)
	{
		_fileWatcher?.Dispose();

		const int maxRetries = 5;
		const int delayMs = 200;
		Exception? lastException = null;

		for (int attempt = 0; attempt < maxRetries; attempt++)
		{
			try
			{
				await using var stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
				_svg = new InkscapeSvg(stream);
				_file = file;
				Tileset = new InkscapeSvgTileset(this);
				RaiseTilesetChanged();

				SetupWatcher(file);
				return;
			}
			catch (IOException ex)
			{
				lastException = ex;
				await Task.Delay(delayMs);
			}
		}

		throw new IOException($"Failed to load SVG file after {maxRetries} attempts.", lastException);
	}

	private void SetupWatcher(FileInfo file)
	{
		ThrowIfDisposed();

		_fileWatcher?.Dispose();

		_fileWatcher = new FileSystemWatcher(file.DirectoryName!, file.Name)
		{
			NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.Attributes | NotifyFilters.FileName,
			EnableRaisingEvents = true,
			IncludeSubdirectories = false
		};

		_fileWatcher.Deleted += async (_, _) =>
		{
			if (!TryBeginOperation()) return;
			var stateLock = await _stateLock.EnterWriteLockAsync();
			try
			{
				if (file.Exists) return;
				_svg = null;
				_file = null;
				Tileset = null;
				RaiseTilesetChanged();
				if (_windowProvider == null) return;
				await _windowProvider.NavPage.Dispatcher.DispatchAsync(async () =>
				{
					await _windowProvider.NavPage.DisplayAlertAsync(
						"File Deleted",
						$"The file {file.FullName} has been deleted. The designer will now be cleared.",
						"OK");
				});
			}
			finally
			{
				EndOperation();
				stateLock.Dispose();
			}
		};

		_fileWatcher.Renamed += async (_, e) =>
		{
			if (!TryBeginOperation()) return;
			var stateLock = await _stateLock.EnterWriteLockAsync();
			try
			{
				if (!string.Equals(e.OldFullPath, file.FullName, StringComparison.OrdinalIgnoreCase) || file.Exists)
					return;
				_svg = null;
				_file = null;
				Tileset = null;
				RaiseTilesetChanged();
				await _windowProvider!.NavPage.Dispatcher.DispatchAsync(async () =>
				{
					await _windowProvider.NavPage.DisplayAlertAsync(
						"File Deleted",
						$"The file {file.FullName} has been deleted. The designer will now be cleared.",
						"OK");
				});
			}
			finally
			{
				EndOperation();
				stateLock.Dispose();
			}
		};

		_fileWatcher.Changed += async (_, _) =>
		{
			if (!TryBeginOperation()) return;
			var stateLock = await _stateLock.EnterWriteLockAsync();
			await Task.Delay(200);

			try
			{
				await LoadInternalAsync(file);
			}
			catch (IOException)
			{
				Trace.WriteLine($"File {file.FullName} is currently inaccessible. Changes will be loaded when the file becomes available.");
				if (_windowProvider == null) return;
				await _windowProvider.NavPage.Dispatcher.DispatchAsync(async () =>
				{
					await _windowProvider.NavPage.DisplayAlertAsync(
						"File Inaccessible",
						$"File {file.FullName} is currently inaccessible. Changes will be loaded when the file becomes available.",
						"OK");
				});
			}
			finally
			{
				EndOperation();
				stateLock.Dispose();
			}
		};
	}

	public async Task SaveAsync(FileInfo file)
	{
		BeginOperation();
		var stateLock = await _stateLock.EnterReadLockAsync();
		try
		{
			if (_file is null || _svg is null) return;
			using var fs = file.Open(FileMode.Create, FileAccess.Write, FileShare.None);
			await _svg.SaveToStreamAsync(fs);
			_file = file;
		}
		finally
		{
			stateLock.Dispose();
			EndOperation();
		}
	}

	public async Task SaveToStreamAsync(Stream stream)
	{
		BeginOperation();
		var stateLock = await _stateLock.EnterReadLockAsync();
		try
		{
			if (_svg is null) return;
			await _svg.SaveToStreamAsync(stream);
		}
		finally
		{
			stateLock.Dispose();
			EndOperation();
		}
	}

	public Tile? GetTileAt(int row, int col)
	{
		BeginOperation();
		var stateLock = _stateLock.EnterReadLock();
		try
		{
			if (_svg is null) return null;
			var element = _svg.GetTileElement(row, col);
			if (element is null) return null;
			return TileExtensions.GetTileFromXElement(element);
		}
		finally
		{
			stateLock.Dispose();
			EndOperation();
		}
	}

	public Tile[] GetAllTiles()
	{
		BeginOperation();
		var stateLock = _stateLock.EnterReadLock();

		try
		{
			if (_svg is null) return [];
			return _svg.GetAllTileElements().Select(TileExtensions.GetTileFromXElement).ToArray();
		}
		finally
		{
			EndOperation();
			stateLock.Dispose();
		}
	}

	public bool CheckContainsTile(Tile tile)
	{
		BeginOperation();
		var stateLock = _stateLock.EnterReadLock();
		try
		{
			if (_svg is null) return false;
			var element = _svg.GetTileElement(tile.Row, tile.Column);
			return element is not null && element.Attribute(TileXNames.Name)?.Value == tile.Name;
		}
		finally
		{
			EndOperation();
			stateLock.Dispose();
		}
	}

	public bool AddTile(Tile tile)
	{
		BeginOperation();
		var stateLock = _stateLock.EnterWriteLock();
		try
		{
			if (_svg is null) throw new InvalidOperationException("SVG Document is not loaded.");
			XElement collectionElement = _svg.GetOrCreateTileCollectionElement();
			var element = _svg.GetTileElement(tile.Row, tile.Column);
			if (element is not null)
			{
				// Tile already exists
				return false;
			}
			element = tile.ToXElement();
			collectionElement.Add(element);
			return true;
		}
		finally
		{
			EndOperation();
			stateLock.Dispose();
			RaiseTilesetChanged();
		}
	}

	public void AddOrReplaceTile(Tile tile)
	{
		BeginOperation();
		var stateLock = _stateLock.EnterWriteLock();
		try
		{
			if (_svg is null) throw new InvalidOperationException("SVG Document is not loaded.");
			XElement collectionElement = _svg!.GetOrCreateTileCollectionElement();
			var element = _svg.GetTileElement(tile.Row, tile.Column);
			if (element is not null)
			{
				element.ReplaceWith(tile.ToXElement());
				return;
			}

			element = tile.ToXElement();
			collectionElement.Add(element);
		}
		finally
		{
			EndOperation();
			stateLock.Dispose();
			RaiseTilesetChanged();
		}
	}

	public void AddOrReplaceTiles(IEnumerable<Tile> tiles)
	{
		BeginOperation(); 
		var stateLock = _stateLock.EnterWriteLock();
		try
		{
			if (_svg is null) throw new InvalidOperationException("SVG Document is not loaded.");
			XElement collectionElement = _svg.GetOrCreateTileCollectionElement();
			foreach (var tile in tiles)
			{
				var element = _svg.GetTileElement(tile.Row, tile.Column);
				if (element is not null)
				{
					element.ReplaceWith(tile.ToXElement());
					continue;
				}
				element = tile.ToXElement();
				collectionElement.Add(element);
			}
		}
		finally
		{
			EndOperation();
			stateLock.Dispose();
			RaiseTilesetChanged();
		}
	}

	public bool RemoveTile(Tile tile)
	{
		BeginOperation();
		var stateLock = _stateLock.EnterWriteLock();
		try
		{
			if (_svg is null) throw new InvalidOperationException("SVG Document is not loaded.");
			var element = _svg.GetTileElement(tile.Row, tile.Column);
			if (element is null)
			{
				// Tile does not exist
				return false;
			}
			element.Remove();
			return true;
		}
		finally
		{
			EndOperation();
			stateLock.Dispose();
			RaiseTilesetChanged();
		}
	}

	public void ClearTiles()
	{
		BeginOperation(); 
		var stateLock = _stateLock.EnterWriteLock();
		try
		{
			if (_svg is null) throw new InvalidOperationException("SVG Document is not loaded.");
			XElement collectionElement = _svg.GetOrCreateTileCollectionElement();
			collectionElement.RemoveAll();
		}
		finally
		{
			EndOperation();
			stateLock.Dispose();
			RaiseTilesetChanged();
		}
	}

	public Scale GetTileSize()
	{
		BeginOperation();
		var stateLock = _stateLock.EnterReadLock();
		try
		{
			if (_svg is null) throw new InvalidOperationException("SVG Document is not loaded.");
			return _svg.GetTileSize();
		}
		finally
		{
			EndOperation();
			stateLock.Dispose();
		}
	}

	public Scale GetSvgSize()
	{
		BeginOperation();
		var stateLock = _stateLock.EnterReadLock();
		try
		{
			if (_svg is null) throw new InvalidOperationException("SVG Document is not loaded.");
			return _svg.GetSvgSize();
		}
		finally
		{
			EndOperation();
			stateLock.Dispose();
		}
	}
	public int GetTileCount()
	{
		BeginOperation();
		var stateLock = _stateLock.EnterReadLock();
		try
		{
			if (_svg is null) throw new InvalidOperationException("SVG Document is not loaded.");
			return _svg.GetAllTileElements().Count();
		}
		finally
		{
			EndOperation();
			stateLock.Dispose();
		}
	}

	public void OpenInExternalEditor()
	{
		BeginOperation();
		var stateLock = _stateLock.EnterReadLock();
		try
		{
			if (CurrentFile == null) return;
			_services.GetRequiredService<IInkscapeService>().OpenFileInInkscape(CurrentFile);
		}
		finally
		{
			EndOperation();
			stateLock.Dispose();
		}
	}

	public async Task<Stream> RenderFileAsync(string extension, CancellationToken cancellationToken = default)
	{
		BeginOperation();
		var stateLock = _stateLock.EnterReadLock();
		try
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (_file == null) throw new InvalidOperationException("No file loaded to render.");
			if (RenderingService == null) throw new InvalidOperationException("Rendering service is not set.");
			return await RenderingService.RenderFileAsync(_file, extension, cancellationToken);
		}
		finally
		{
			stateLock.Dispose();
			EndOperation();
		}
	}

	public async Task<Stream> RenderSegmentAsync(string extension, int left, int top, int right, int bottom, Scale? exportScale = null, CancellationToken cancellationToken = default)
	{
		BeginOperation();
		var stateLock = _stateLock.EnterReadLock();
		try
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (_file == null) throw new InvalidOperationException("No file loaded to render.");
			if (RenderingService == null) throw new InvalidOperationException("Rendering service is not set.");
			return await RenderingService.RenderSegmentAsync(_file, extension, left, top, right, bottom, exportScale, cancellationToken);
		}
		finally
		{
			stateLock.Dispose();
			EndOperation();
		}
	}

	public async Task<bool> IsSegmentEmptyAsync(int left, int top, int right, int bottom, CancellationToken cancellationToken = default)
	{
		BeginOperation();
		var stateLock = _stateLock.EnterReadLock();
		try
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (_file == null) throw new InvalidOperationException("No file loaded to render.");
			if (RenderingService == null) throw new InvalidOperationException("Rendering service is not set.");
			return await RenderingService.IsSegmentEmptyAsync(_file, left, top, right, bottom, cancellationToken);
		}
		finally
		{
			stateLock.Dispose();
			EndOperation();
		}
	}
	private void RaiseTilesetChanged()
	{
		var handlers = TilesetChanged;
		foreach (var handler in handlers.GetInvocationList())
		{
			try
			{
				handler.DynamicInvoke(this);
			}
			catch (Exception ex)
			{
				Trace.TraceError($"Error invoking TilesetChanged handler: {ex}");
			}
		}
	}

	public void Dispose()
	{
		GC.SuppressFinalize(this);
		DisposeAsync().AsTask().GetAwaiter().GetResult();
	}

	public async ValueTask DisposeAsync()
	{
		GC.SuppressFinalize(this);

		_fileWatcher?.Dispose();
		_fileWatcher = null;

		if (Interlocked.Exchange(ref _disposeState, DISPOSAL) != ACTIVE)
		{
			return;
		}

		if (Volatile.Read(ref _activeOperationCount) != 0)
		{
			await _disposeCompletion.Task.ConfigureAwait(false);
		}

		_svg = null;
		_file = null;
		Tileset = null;
		_windowProvider = null;

		await RenderingService.DisposeAsync().ConfigureAwait(false);
	}
}
