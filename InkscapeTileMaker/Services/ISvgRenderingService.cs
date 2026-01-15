namespace InkscapeTileMaker.Services
{
	public interface ISvgRenderingService
	{
		/// <summary>
		/// Return a rendered SVG file as a png stream
		/// </summary>
		public Task<Stream> RenderSvgFile(FileInfo svgFile);
	}
}
