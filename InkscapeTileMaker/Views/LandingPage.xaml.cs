using InkscapeTileMaker.ViewModels;

namespace InkscapeTileMaker.Views;

public partial class LandingPage : ContentPage
{
	public LandingPage(LandingViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}