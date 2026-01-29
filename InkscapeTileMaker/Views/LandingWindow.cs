using InkscapeTileMaker.ViewModels;

namespace InkscapeTileMaker.Views;

public class LandingWindow : Window, ILandingNavigation
{
	private readonly NavigationPage _nav;
	private readonly LandingViewModel _vm;

	public LandingWindow(LandingViewModel vm)
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