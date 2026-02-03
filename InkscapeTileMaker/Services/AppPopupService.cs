using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Extensions;

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
			var vm = new ViewModels.ConfirmationPopupViewModel()
			{
				Title = title,
				Message = message,
				ConfirmButtonText = confirmText,
				CancelButtonText = cancelText
			};

			var view = new Views.ConfirmationPopup(vm);

			var opts = new PopupOptions()
			{
				CanBeDismissedByTappingOutsideOfPopup = false
			};

			await PopupExtensions.ShowPopupAsync(_windowProvider.Navigation, view, opts);

			return vm.Result;
		}

		public async Task ShowTextAsync(string text)
		{
			var vm = new ViewModels.TextPopupViewModel()
			{
				Text = text
			};

			var view = new Views.TextPopup(vm);
			await PopupExtensions.ShowPopupAsync(_windowProvider.Navigation, view, PopupOptions.Empty);
		}
	}
}
