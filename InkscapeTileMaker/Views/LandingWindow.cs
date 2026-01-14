using InkscapeTileMaker.Pages;
using InkscapeTileMaker.ViewModels;

namespace InkscapeTileMaker.Views;

public class LandingWindow : Window
{
	public LandingWindow(LandingViewModel vm)
	{
		Width = 600;
		Height = 400;
		MinimumWidth = 600;
		MinimumHeight = 400;

		Page = new LandingPage(vm);
	}
}