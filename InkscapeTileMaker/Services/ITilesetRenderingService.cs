namespace InkscapeTileMaker.Services
{
	public interface ITilesetRenderingService
	{
		public Task<Stream> RenderFile(FileInfo file, CancellationToken cancellationToken);

		public Task<Stream> RenderSegment(FileInfo file, int left, int top, int right, int bottom, CancellationToken cancellationToken);
	}
}
