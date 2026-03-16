namespace InkscapeTileMaker.Services;

public interface IWindowOpeningService
{
	public Task OpenDesignerWindowAsync(FileInfo? svgFile = null);
	public Task OpenLandingWindowAsync();
}