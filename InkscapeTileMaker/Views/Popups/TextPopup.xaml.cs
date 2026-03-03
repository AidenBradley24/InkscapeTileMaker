using CommunityToolkit.Maui.Views;
using InkscapeTileMaker.Services;
using InkscapeTileMaker.ViewModels.Popups;

namespace InkscapeTileMaker.Views.Popups;

public partial class TextPopup : Popup, IAppPopup
{
	public TextPopup(TextPopupViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}

	public async Task ClosePopup()
	{
		await CloseAsync();
	}
}