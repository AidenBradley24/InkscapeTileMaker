using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InkscapeTileMaker.Services;

namespace InkscapeTileMaker.ViewModels.Popups
{
	public partial class ConfirmationPopupViewModel : ObservableObject, IAppPopupViewModel
	{
		public Popup? View { get; set; }

		[ObservableProperty] public partial string Title { get; set; } = "Confirm";
		[ObservableProperty] public partial string Message { get; set; } = "(message)";
		[ObservableProperty] public partial string ConfirmButtonText { get; set; } = "OK";
		[ObservableProperty] public partial string CancelButtonText { get; set; } = "Cancel";

		public bool Result { get; private set; } = false;

		[RelayCommand]
		public async Task Confirm()
		{
			Result = true;
			if (View != null)
			{
				await ClosePopup();
			}
		}

		[RelayCommand]
		public async Task Cancel()
		{
			Result = false;
			if (View != null)
			{
				await ClosePopup();
			}
		}

		public async Task ClosePopup()
		{
			await MainThread.InvokeOnMainThreadAsync(async () =>
			{
				if (View != null && View.IsLoaded)
				{
					await View.CloseAsync();
				}
			}).ConfigureAwait(false);
		}
	}
}
