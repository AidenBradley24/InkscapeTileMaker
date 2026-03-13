using CommunityToolkit.Maui.Views;
using InkscapeTileMaker.ViewModels.Popups;

namespace InkscapeTileMaker.Views.Popups;

public partial class ConfirmationPopup : Popup
{
	public ConfirmationPopup(ConfirmationPopupViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
		vm.View = this;
	}
}