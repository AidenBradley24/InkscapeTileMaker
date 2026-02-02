namespace InkscapeTileMaker.Services;

public interface IWindowOpeningService
{
	void OpenDesignerWindow(FileInfo? svgFile = null);
}