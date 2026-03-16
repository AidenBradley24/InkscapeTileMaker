using InkscapeTileMaker.Services;
using InkscapeTileMaker.ViewModels;

namespace InkscapeTileMaker.Views;

public partial class LandingWindow : Window, ILandingNavigation, IWindowProvider
{
	private readonly NavigationPage _nav;
	private readonly LandingViewModel _vm;
	private readonly IServiceProvider _services;
	private readonly AppPopupService _popupService;

	public LandingWindow(LandingViewModel vm, IServiceProvider services)
	{
		Width = 600;
		Height = 400;
		MinimumWidth = 600;
		MinimumHeight = 400;
		MaximumHeight = 600;
		MinimumHeight = 400;

		_nav = new NavigationPage(new LandingPage(vm));
		_vm = vm;
		_services = services;
		Page = _nav;
		vm.LandingNavigation = this;
		vm.RegisterWindow(this);

		_popupService = new AppPopupService(this);
	}

	public IAppPopupService PopupService => _popupService;

	public Page CurrentPage => _nav.CurrentPage;

	public NavigationPage NavPage => _nav;

	public void CloseWindow()
	{
		Application.Current?.CloseWindow(this);
	}

	public async Task GotoLandingPage()
	{
		await _nav.PopToRootAsync();
	}

	public async Task GotoTemplatePage()
	{
		await _nav.PushAsync(new TemplatePage(_vm));
	}

	public async Task GotoSettingsPage()
	{
		if (_nav.CurrentPage is SettingsPage)
		{
			return;
		}

		await _nav.PushAsync(_services.GetRequiredService<SettingsPage>());
	}
}

public interface ILandingNavigation
{
	Task GotoLandingPage();
	Task GotoTemplatePage();
	Task GotoSettingsPage();
}