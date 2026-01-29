namespace InkscapeTileMaker.Services
{
	public interface ITilesetRenderingService
	{
		public Task<Stream> RenderFileAsync(FileInfo file, string extension, CancellationToken cancellationToken = default);

		public Task<Stream> RenderSegmentAsync(FileInfo file, string extension, int left, int top, int right, int bottom, CancellationToken cancellationToken = default);

		public Task<bool> IsSegmentEmptyAsync(FileInfo file, int left, int top, int right, int bottom, CancellationToken cancellationToken = default);
	}
}
