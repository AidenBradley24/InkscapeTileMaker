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

	public void OpenDesignerWindow(FileInfo? file = null)
	{
		var app = Application.Current;
		if (app is null) return;
		var designerWindow = _services.GetRequiredService<DesignerWindow>();
		app.OpenWindow(designerWindow);
		if (file is not null)
		{
			var newViewModel = (DesignerViewModel)designerWindow!.BindingContext;
			ITilesetConnection connection;
			if (file.Extension.Equals(".svg", StringComparison.OrdinalIgnoreCase))
			{
				connection = new InkscapeSvgConnectionService(_services);
			}
			else
			{
				// Unsupported file type
				return;
			}

			newViewModel.SetTilesetConnection(connection);
			_ = connection.LoadAsync(file);
		}
	}
}