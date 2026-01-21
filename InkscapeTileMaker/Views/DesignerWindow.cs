using InkscapeTileMaker.ViewModels;

namespace InkscapeTileMaker.Views;

public partial class DesignerWindow : Window
{
	public DesignerWindow(DesignerViewModel vm)
	{
		Page = new NavigationPage(new DesignerPage(vm));
		BindingContext = vm;

		var titleBar = new TitleBar();
		TitleBar = titleBar;
		titleBar.SetBinding(Microsoft.Maui.Controls.TitleBar.TitleProperty, new Binding(nameof(vm.Title)));
	}
}