using CommunityToolkit.Maui.Views;
using InkscapeTileMaker.Services;
using InkscapeTileMaker.ViewModels.Popups;

namespace InkscapeTileMaker.Views.Popups;

public partial class ConfirmationPopup : Popup, IAppPopup
{
	public ConfirmationPopup(ConfirmationPopupViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
		vm.PopupView = this;
	}

	public async Task ClosePopup()
	{
		await CloseAsync();
	}
}