using CommunityToolkit.Maui;
using InkscapeTileMaker.Services;
using InkscapeTileMaker.ViewModels;

namespace InkscapeTileMaker.Views;

public class LandingWindow : Window, ILandingNavigation, IWindowProvider
{
	private readonly NavigationPage _nav;
	private readonly LandingViewModel _vm;
	private readonly AppPopupService _popupService;

	public LandingWindow(LandingViewModel vm, IServiceProvider serviceProvider)
	{
		Width = 600;
		Height = 400;
		MinimumWidth = 600;
		MinimumHeight = 400;
		MaximumHeight = 600;
		MinimumHeight = 400;

		_nav = new NavigationPage(new LandingPage(vm));
		_vm = vm;
		Page = _nav;
		vm.LandingNavigation = this;
		vm.RegisterWindow(this);

		_popupService = new AppPopupService(serviceProvider.GetRequiredService<IPopupService>(), this);
	}

	public IAppPopupService PopupService => _popupService;

	public Page CurrentPage => _nav.CurrentPage;

	public void CloseWindow()
	{
		throw new NotImplementedException();
	}

	public async Task GotoLandingPage()
	{
		await _nav.PopToRootAsync();
	}

	public async Task GotoTemplatePage()
	{
		await _nav.PushAsync(new TemplatePage(_vm));
	}
}

public interface ILandingNavigation
{
	Task GotoLandingPage();
	Task GotoTemplatePage();
}