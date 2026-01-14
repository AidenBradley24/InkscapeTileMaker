namespace InkscapeTileMaker.Services
{
	public interface ISvgRenderingService
	{
		public Task<Stream> RenderSvgFile(FileInfo svgFile);
	}
}
