using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using InkscapeTileMaker.Services;

namespace InkscapeTileMaker.ViewModels.Popups
{
	public partial class TextPopupViewModel : ObservableObject, IAppPopupViewModel
	{
		[ObservableProperty]
		public partial string Text { get; set; } = "(text)";

		internal Popup? View { get; set; }

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
