using CommunityToolkit.Maui.Views;
using InkscapeTileMaker.ViewModels.Popups;

namespace InkscapeTileMaker.Views.Popups;

public partial class ProgressPopup : Popup
{
	public ProgressPopup(ProgressPopupViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
		vm.View = this;
	}
}