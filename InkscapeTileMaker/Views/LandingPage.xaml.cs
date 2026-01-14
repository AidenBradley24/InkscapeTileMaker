using InkscapeTileMaker.ViewModels;

namespace InkscapeTileMaker.Pages;

public partial class LandingPage : ContentPage
{
    public LandingPage(LandingViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}