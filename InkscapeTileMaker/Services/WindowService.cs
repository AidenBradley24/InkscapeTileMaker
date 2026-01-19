using InkscapeTileMaker.ViewModels;
using InkscapeTileMaker.Views;

namespace InkscapeTileMaker.Services;

public class WindowService : IWindowService
{
	private readonly IServiceProvider _services;

	public WindowService(IServiceProvider services)
	{
		_services = services;
	}

	public void CloseCurrentWindow()
	{
		var app = Application.Current;
		if (app is null) return;

		// TODO implement
	}

	public void OpenDesignerWindow(FileInfo? svgFile = null)
	{
		var app = Application.Current;
		if (app is null) return;
		var designerWindow = _services.GetRequiredService<DesignerWindow>();
		app.OpenWindow(designerWindow);
		if (svgFile is not null)
		{
			var newViewModel = (DesignerViewModel)designerWindow!.BindingContext;
			newViewModel!.SvgConnectionService.LoadSvg(svgFile);
		}
	}
}