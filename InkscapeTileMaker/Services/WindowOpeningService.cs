using InkscapeTileMaker.ViewModels;
using InkscapeTileMaker.Views;

namespace InkscapeTileMaker.Services;

public class WindowOpeningService : IWindowOpeningService
{
	private readonly IServiceProvider _services;

	public WindowOpeningService(IServiceProvider services)
	{
		_services = services;
	}

	public async Task OpenDesignerWindowAsync(FileInfo? file = null)
	{
		var app = Application.Current;
		if (app is null) return;
		var designerWindow = _services.GetRequiredService<DesignerWindow>();
		await OpenWindowAsync(designerWindow);

		if (file is not null)
		{
			var newViewModel = (DesignerViewModel)designerWindow!.BindingContext;
			ITilesetConnection connection;
			if (file.Extension.Equals(".svg", StringComparison.OrdinalIgnoreCase))
			{
				connection = new InkscapeSvgConnection(_services, designerWindow);
			}
			else
			{
				// Unsupported file type
				return;
			}

			newViewModel.SetTilesetConnection(connection);
			await connection.LoadAsync(file);
		}
	}

	public async Task OpenLandingWindowAsync()
	{
		var app = Application.Current;
		if (app is null) return;
		var landingWindow = _services.GetRequiredService<LandingWindow>();
		await OpenWindowAsync(landingWindow);
	}

	private static Task OpenWindowAsync(Window window)
	{
		var app = Application.Current;
		var taskCompletionSource = new TaskCompletionSource();
		window.Activated += (_, _) =>
		{
			taskCompletionSource.TrySetResult();
		};

		app!.OpenWindow(window);
		return taskCompletionSource.Task;
	}
}