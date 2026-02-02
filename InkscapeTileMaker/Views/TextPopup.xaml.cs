using CommunityToolkit.Maui.Views;
using InkscapeTileMaker.ViewModels;

namespace InkscapeTileMaker.Views;

public partial class TextPopup : Popup
{
	public TextPopup(TextPopupViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}