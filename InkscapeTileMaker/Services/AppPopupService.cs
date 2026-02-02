using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Extensions;
using System.Diagnostics;

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

		public Task<bool> ShowConfirmation(string message)
		{
			throw new NotImplementedException();
		}

		public async Task ShowText(string text)
		{
			_popupService.ShowPopup<Views.TextPopup>(_windowProvider.Navigation);
		}
	}
}
