using CommunityToolkit.Mvvm.ComponentModel;

namespace InkscapeTileMaker.ViewModels.Popups
{
	public partial class ProgressPopupViewModel : ObservableObject
	{
		[ObservableProperty] public partial string Message { get; set; } = "...";
		[ObservableProperty] public partial double Progress { get; set; } = 0.0;
		[ObservableProperty] public partial bool IsIndeterminate { get; set; } = true;

		private readonly Progress<double> _progressReporter;
		public IProgress<double> ProgressReporter => _progressReporter;

		public ProgressPopupViewModel()
		{
			_progressReporter = new Progress<double>(value => Progress = value);
		}
	}
}
