using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Extensions;

namespace InkscapeTileMaker.Services
{
	public class AppPopupService : IAppPopupService
	{
		private readonly IPopupService _popupService;
		private readonly IWindowProvider _windowProvider;

		public AppPopupService(IPopupService popupService, IWindowProvider windowProvider)
		{
			_popupService = popupService;
			_windowProvider = windowProvider;
		}

		public Task<bool> ShowConfirmationAsync(string message)
		{
			throw new NotImplementedException();
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
