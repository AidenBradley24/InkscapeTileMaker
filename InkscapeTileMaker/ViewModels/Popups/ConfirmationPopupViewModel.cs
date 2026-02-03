using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InkscapeTileMaker.Services;

namespace InkscapeTileMaker.ViewModels
{
	public partial class ConfirmationPopupViewModel : ObservableObject
	{
		public IPopupCloser? PopupView { get; set; }

		[ObservableProperty] public partial string Title { get; set; } = "Confirm";
		[ObservableProperty] public partial string Message { get; set; } = "(message)";
		[ObservableProperty] public partial string ConfirmButtonText { get; set; } = "OK";
		[ObservableProperty] public partial string CancelButtonText { get; set; } = "Cancel";

		public bool Result { get; private set; } = false;

		[RelayCommand]
		public void Confirm()
		{
			Result = true;
			PopupView?.RequestClose();
		}

		[RelayCommand]
		public void Cancel()
		{
			Result = false;
			PopupView?.RequestClose();
		}
	}
}
