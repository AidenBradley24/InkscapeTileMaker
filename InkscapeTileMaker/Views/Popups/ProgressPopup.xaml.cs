using CommunityToolkit.Maui.Views;
using InkscapeTileMaker.Services;
using InkscapeTileMaker.ViewModels.Popups;

namespace InkscapeTileMaker.Views.Popups;

public partial class ProgressPopup : Popup, IPopupCloser
{
	public ProgressPopup(ProgressPopupViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}

	public async Task RequestClose()
	{
		await CloseAsync();
	}
}