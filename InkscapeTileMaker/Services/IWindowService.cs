namespace InkscapeTileMaker.Services;

public interface IWindowService
{
	void OpenDesignerWindow(FileInfo? svgFile = null);
	void CloseCurrentWindow();
}