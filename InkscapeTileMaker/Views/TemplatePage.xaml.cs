using InkscapeTileMaker.ViewModels;

namespace InkscapeTileMaker.Views;

public partial class TemplatePage : ContentPage
{
	public TemplatePage(LandingViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}