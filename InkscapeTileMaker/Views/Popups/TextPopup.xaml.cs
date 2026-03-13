using CommunityToolkit.Maui.Views;
using InkscapeTileMaker.ViewModels.Popups;

namespace InkscapeTileMaker.Views.Popups;

public partial class TextPopup : Popup
{
	public TextPopup(TextPopupViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
		vm.View = this;
	}
}