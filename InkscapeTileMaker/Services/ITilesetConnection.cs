using InkscapeTileMaker.Models;

namespace InkscapeTileMaker.Services
{
	/// <summary>
	/// Tileset connections are a thread safe view on a tileset and its file.
	/// </summary>
	public interface ITilesetConnection : IAsyncDisposable, IDisposable
	{
		public ITileset? Tileset { get; }

		public ITilesetRenderingService RenderingService { get; }

		public FileInfo? CurrentFile { get; }

		public event Action<ITilesetConnection> TilesetChanged;

		public Task SaveAsync(FileInfo file);

		public Task SaveToStreamAsync(Stream stream);

		public Task LoadAsync(FileInfo file);

		public void OpenInExternalEditor();

		public Task<Stream> RenderFileAsync(string extension, CancellationToken cancellationToken = default);

		public Task<Stream> RenderSegmentAsync(string extension, int left, int top, int right, int bottom, Scale? exportScale = null, CancellationToken cancellationToken = default);

		public Task<bool> IsSegmentEmptyAsync(int left, int top, int right, int bottom, CancellationToken cancellationToken = default);
	}
}
