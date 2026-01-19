using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InkscapeTileMaker.Services;

namespace InkscapeTileMaker.ViewModels
{
	public partial class LandingViewModel : ObservableObject
	{
		private readonly IWindowService _windowService;

		public LandingViewModel(IWindowService windowService)
		{
			_windowService = windowService;
		}

		[RelayCommand]
		public async Task CreateNewDesign()
		{
			await Task.Yield();
			_windowService.OpenDesignerWindow();
		}

		[RelayCommand]
		public async Task OpenExistingDesign()
		{
			var result = await FilePicker.PickAsync(new PickOptions
			{
				PickerTitle = "Open SVG design",
				FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
				{
					{ DevicePlatform.iOS, new[] { "public.svg" } },
					{ DevicePlatform.Android, new[] { "image/svg+xml" } },
					{ DevicePlatform.WinUI, new[] { ".svg" } },
					{ DevicePlatform.MacCatalyst, new[] { "public.svg" } },
				})
			});

			if (result == null)
			{
				return;
			}

			if (!string.Equals(Path.GetExtension(result.FullPath), ".svg", StringComparison.OrdinalIgnoreCase))
			{
				return;
			}

			var svgFile = new FileInfo(result.FullPath);
			_windowService.OpenDesignerWindow(svgFile);
		}
	}
}
