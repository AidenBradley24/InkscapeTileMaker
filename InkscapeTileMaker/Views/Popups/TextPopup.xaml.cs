using CommunityToolkit.Maui.Views;
using InkscapeTileMaker.Services;
using InkscapeTileMaker.ViewModels;

namespace InkscapeTileMaker.Views;

public partial class TextPopup : Popup, IPopupCloser
{
	public TextPopup(TextPopupViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}

	public async Task RequestClose()
	{
		await CloseAsync();
	}
}