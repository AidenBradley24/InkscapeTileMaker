using InkscapeTileMaker.Services;
using InkscapeTileMaker.ViewModels;

#if WINDOWS
using Microsoft.UI;
using Microsoft.UI.Windowing;
#endif

namespace InkscapeTileMaker.Views;

public partial class DesignerWindow : Microsoft.Maui.Controls.Window, IWindowProvider
{
	private readonly NavigationPage _nav;
	private readonly AppPopupService _popupService;

	public IAppPopupService PopupService => _popupService;

	public Page CurrentPage => _nav.CurrentPage;

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

		vm.CloseRequested += OnCloseRequested;

#if WINDOWS
		WireUpNativeClose();
#endif
	}

	private async void OnCloseRequested()
	{
		if (BindingContext is DesignerViewModel viewModel && viewModel.HasUnsavedChanges)
		{
			bool shouldClose = await Page!.DisplayAlertAsync(
				"Close Designer",
				"Are you sure you want to close the designer? Unsaved changes will be lost.",
				"Yes",
				"No"
			);
			if (!shouldClose) return;
		}

		Microsoft.Maui.Controls.Application.Current?.CloseWindow(this);
	}

	protected override void OnDestroying()
	{
		base.OnDestroying();
		if (BindingContext is DesignerViewModel viewModel)
		{
			viewModel.CloseRequested -= OnCloseRequested;
		}
	}

	public INavigation GetNavigation()
	{
		return _nav.Navigation;
	}

	public Page GetCurrentPage()
	{
		return _nav.CurrentPage;
	}

	public void CloseWindow()
	{
		OnCloseRequested();
	}

#if WINDOWS
	private void WireUpNativeClose()
	{
		var mauiWindow = this.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
		if (mauiWindow == null)
		{
			// Delay until handler is created
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
		var appWindow = AppWindow.GetFromWindowId(windowId);

		appWindow.Closing += async (AppWindow sender, AppWindowClosingEventArgs args) =>
		{
			args.Cancel = true;
			OnCloseRequested();
		};
	}
#endif
}
