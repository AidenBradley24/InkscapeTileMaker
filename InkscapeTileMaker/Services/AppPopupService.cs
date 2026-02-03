using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Extensions;

using InkscapeTileMaker.ViewModels.Popups;
using InkscapeTileMaker.Views.Popups;

namespace InkscapeTileMaker.Services
{
	public class AppPopupService : IAppPopupService
	{
		private readonly IWindowProvider _windowProvider;

		public AppPopupService(IWindowProvider windowProvider)
		{
			_windowProvider = windowProvider;
		}

		public async Task<bool> ShowConfirmationAsync(string title, string message, string confirmText = "OK", string cancelText = "Cancel")
		{
			var vm = new ConfirmationPopupViewModel()
			{
				Title = title,
				Message = message,
				ConfirmButtonText = confirmText,
				CancelButtonText = cancelText
			};

			var view = new ConfirmationPopup(vm);
			var opts = new PopupOptions()
			{
				CanBeDismissedByTappingOutsideOfPopup = false
			};

			await PopupExtensions.ShowPopupAsync(_windowProvider.Navigation, view, opts);
			return vm.Result;
		}

		public async Task ShowTextAsync(string text)
		{
			var vm = new TextPopupViewModel()
			{
				Text = text
			};

			var view = new TextPopup(vm);
			await PopupExtensions.ShowPopupAsync(_windowProvider.Navigation, view, PopupOptions.Empty);
		}

		public async Task ShowProgressOnTaskAsync(string message, bool isIndeterminate, Func<IProgress<double>, Task> progressAction)
		{
			var vm = new ProgressPopupViewModel()
			{
				Message = message,
				IsIndeterminate = isIndeterminate
			};

			var view = new ProgressPopup(vm);
			var opts = new PopupOptions()
			{
				CanBeDismissedByTappingOutsideOfPopup = false
			};

			var popupTask = PopupExtensions.ShowPopupAsync(_windowProvider.Navigation, view, opts);
			var workTask = progressAction.Invoke(vm.ProgressReporter);
			await workTask.ConfigureAwait(continueOnCapturedContext: false);
			await MainThread.InvokeOnMainThreadAsync(view.RequestClose);
			await popupTask.ConfigureAwait(continueOnCapturedContext: false);
		}
	}
}
