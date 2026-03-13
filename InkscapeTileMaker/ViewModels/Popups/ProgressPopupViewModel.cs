using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using InkscapeTileMaker.Services;

namespace InkscapeTileMaker.ViewModels.Popups
{
	public partial class ProgressPopupViewModel : ObservableObject, IAppPopupViewModel
	{
		[ObservableProperty] public partial string Message { get; set; } = "...";
		[ObservableProperty] public partial double Progress { get; set; } = 0.0;
		[ObservableProperty] public partial bool IsIndeterminate { get; set; } = true;

		private readonly Progress<double> _progressReporter;
		public IProgress<double> ProgressReporter => _progressReporter;

		public Popup? View { get; set; }

		public ProgressPopupViewModel()
		{
			_progressReporter = new Progress<double>(value => Progress = value);
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
