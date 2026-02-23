namespace InkscapeTileMaker.Services;

public interface IWindowOpeningService
{
	public void OpenDesignerWindow(FileInfo? svgFile = null);
	public void OpenLandingWindow();
}