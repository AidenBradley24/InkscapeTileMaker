using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InkscapeTileMaker.Services;
using InkscapeTileMaker.Views;
using System.Collections.ObjectModel;

namespace InkscapeTileMaker.ViewModels
{
	public partial class LandingViewModel : ObservableObject
	{
		private readonly IWindowService _windowService;
		private readonly ITemplateService _templateService;
		private readonly IFileSaver _fileSaver;

		public ILandingNavigation? LandingNavigation { get; set; }

		[ObservableProperty]
		public partial ObservableCollection<TemplateRecord> Templates { get; set; } = [];

		public LandingViewModel(IWindowService windowService, ITemplateService templateService, IFileSaver fileSaver)
		{
			_windowService = windowService;
			_templateService = templateService;
			_fileSaver = fileSaver;
		}

		[RelayCommand]
		public async Task CreateNewDesign()
		{
			if (LandingNavigation == null) return;
			if (Templates.Count == 0)
			{
				var templates = await _templateService.GetTemplatesAsync();
				foreach (var template in templates)
				{
					Templates.Add(template);
				}
			}
			await LandingNavigation.GotoTemplatePage();
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

		[RelayCommand]
		public async Task CreateWithTemplate(TemplateRecord template)
		{
			var stream = await _templateService.OpenTemplateStreamAsync(template);
			var result = await _fileSaver.SaveAsync($"new {template.Name}.svg", stream);
			if (result != null && !string.IsNullOrEmpty(result.FilePath))
			{
				var svgFile = new FileInfo(result.FilePath);
				_windowService.OpenDesignerWindow(svgFile);
			}
		}
	}
}
