using InkscapeTileMaker.Pages;
using InkscapeTileMaker.ViewModels;

namespace InkscapeTileMaker.Views;

public partial class DesignerWindow : Window
{
	public DesignerWindow(DesignerViewModel vm)
	{
		Page = new NavigationPage(new DesignerPage(vm));
		BindingContext = vm;

		TitleBar = new TitleBar
		{
			Title = "Inkscape Tile Maker - Designer",
		};


	}
}