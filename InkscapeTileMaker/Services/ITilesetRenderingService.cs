namespace InkscapeTileMaker.Services
{
	public interface ITilesetRenderingService
	{
		public Task<Stream> RenderFileAsync(FileInfo file, CancellationToken cancellationToken);

		public Task<Stream> RenderSegmentAsync(FileInfo file, int left, int top, int right, int bottom, CancellationToken cancellationToken);

		public Task<bool> IsSegmentEmptyAsync(FileInfo file, int left, int top, int right, int bottom, CancellationToken cancellationToken);
	}
}
