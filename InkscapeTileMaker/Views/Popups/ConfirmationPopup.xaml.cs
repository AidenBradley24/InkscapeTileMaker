using CommunityToolkit.Maui.Views;
using InkscapeTileMaker.Services;
using InkscapeTileMaker.ViewModels.Popups;

namespace InkscapeTileMaker.Views.Popups;

public partial class ConfirmationPopup : Popup, IPopupCloser
{
	public ConfirmationPopup(ConfirmationPopupViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
		vm.PopupView = this;
	}

	public async Task RequestClose()
	{
		await CloseAsync();
	}
}