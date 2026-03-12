using InkscapeTileMaker.Services;
using InkscapeTileMaker.ViewModels;

#if WINDOWS
using Microsoft.UI;
using Microsoft.UI.Windowing;
using System.Diagnostics;
#endif

namespace InkscapeTileMaker.Views;

public partial class DesignerWindow : Microsoft.Maui.Controls.Window, IWindowProvider
{
	private readonly NavigationPage _nav;
	private readonly AppPopupService _popupService;
	private bool _isClosePromptActive;
	private bool _isClosing;

#if WINDOWS
	private AppWindow? _appWindow;
	private bool _allowNativeClose;
#endif

	public IAppPopupService PopupService => _popupService;

	public Page CurrentPage => _nav.CurrentPage;

	public NavigationPage NavPage => _nav;

	public DesignerWindow(DesignerViewModel vm)
	{
		_nav = new NavigationPage(new DesignerPage(vm));
		Page = _nav;
		BindingContext = vm;

		_popupService = new AppPopupService(this);
		vm.RegisterWindow(this);

		var titleBar = new TitleBar();
		TitleBar = titleBar;
		titleBar.SetBinding(Microsoft.Maui.Controls.TitleBar.TitleProperty, new Binding(nameof(vm.Title)));


#if WINDOWS
		WireUpNativeClose();
#endif
	}

	private async Task RequestCloseAsync()
	{
		if (_isClosing || _isClosePromptActive)
		{
			return;
		}

		var dispatcher = Dispatcher ?? Page?.Dispatcher;
		if (dispatcher == null)
		{
			return;
		}

		await dispatcher.DispatchAsync(async () =>
		{
			if (_isClosing || _isClosePromptActive)
			{
				return;
			}

			try
			{
				_isClosePromptActive = true;

				if (BindingContext is DesignerViewModel viewModel && viewModel.HasUnsavedChanges)
				{
					bool shouldClose = await Page!.DisplayAlertAsync(
						"Close Designer",
						"Are you sure you want to close the designer? Unsaved changes will be lost.",
						"Yes",
						"No"
					);

					if (!shouldClose)
					{
						return;
					}
				}

				_isClosing = true;

#if WINDOWS
				_allowNativeClose = true;
#endif
				await MainThread.InvokeOnMainThreadAsync(() => Microsoft.Maui.Controls.Application.Current?.CloseWindow(this));
			}
			catch (Exception ex)
			{
				_isClosing = false;
#if WINDOWS
				_allowNativeClose = false;
#endif
				Trace.WriteLine($"Error during close request: {ex.Message}");
				Trace.WriteLine(ex);
			}
			finally
			{
				_isClosePromptActive = false;
			}
		});
	}

	protected override void OnDestroying()
	{
		base.OnDestroying();

		if (BindingContext is DesignerViewModel viewModel)
		{
			viewModel.DisposeAsync().AsTask().Wait();
		}

#if WINDOWS
		HandlerChanged -= OnHandlerChangedForWindows;

		if (_appWindow != null)
		{
			_appWindow.Closing -= OnAppWindowClosing;
			_appWindow = null;
		}
#endif
	}

	public void CloseWindow()
	{
		Microsoft.Maui.Controls.Application.Current?.CloseWindow(this);
	}

#if WINDOWS
	private void WireUpNativeClose()
	{
		var mauiWindow = this.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
		if (mauiWindow == null)
		{
			HandlerChanged += OnHandlerChangedForWindows;
			return;
		}

		HookWindowClose(mauiWindow);
	}

	private void OnHandlerChangedForWindows(object? sender, EventArgs e)
	{
		var mauiWindow = this.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
		if (mauiWindow == null)
		{
			return;
		}

		HandlerChanged -= OnHandlerChangedForWindows;
		HookWindowClose(mauiWindow);
	}

	private void HookWindowClose(Microsoft.UI.Xaml.Window mauiWindow)
	{
		var windowId = Win32Interop.GetWindowIdFromWindow(WinRT.Interop.WindowNative.GetWindowHandle(mauiWindow));
		_appWindow = AppWindow.GetFromWindowId(windowId);
		_appWindow.Closing -= OnAppWindowClosing;
		_appWindow.Closing += OnAppWindowClosing;
	}

	private void OnAppWindowClosing(AppWindow sender, AppWindowClosingEventArgs args)
	{
		if (_allowNativeClose)
		{
			return;
		}

		args.Cancel = true;
		MainThread.InvokeOnMainThreadAsync(async () => await RequestCloseAsync());
	}
#endif
}
