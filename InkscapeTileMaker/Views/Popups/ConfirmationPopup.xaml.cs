using CommunityToolkit.Maui.Views;
using InkscapeTileMaker.Services;
using InkscapeTileMaker.ViewModels;

namespace InkscapeTileMaker.Views;

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