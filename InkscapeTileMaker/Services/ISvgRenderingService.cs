namespace InkscapeTileMaker.Services
{
	public interface ISvgRenderingService
	{
		public Task<Stream> RenderSvgFile(FileInfo svgFile, CancellationToken cancellationToken);

		public Task<Stream> RenderSvgSegment(FileInfo svgFile, int left, int top, int right, int bottom, CancellationToken cancellationToken);
	}
}
