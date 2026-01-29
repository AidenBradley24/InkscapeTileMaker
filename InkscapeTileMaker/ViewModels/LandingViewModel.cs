using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InkscapeTileMaker.Services;
using InkscapeTileMaker.Utility;
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

		[ObservableProperty]
		public partial TemplateRecord? SelectedTemplate { get; set; }

		[ObservableProperty]
		public partial int TileSizeX { get; set; } = 256;

		[ObservableProperty]
		public partial int TileSizeY { get; set; } = 256;

		[ObservableProperty]
		public partial int TileSetSizeX { get; set; } = 4;

		[ObservableProperty]
		public partial int TileSetSizeY { get; set; } = 4;

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
		public async Task CreateWithTemplate()
		{
			if (SelectedTemplate == null) return;
			using var ms = new MemoryStream();
			using (var stream = await _templateService.OpenTemplateStreamAsync(SelectedTemplate))
			{
				var svg = new InkscapeSvg(stream);
				svg.SetTileSize(new Scale(TileSizeX, TileSizeY));
				svg.SetSvgSize(new Scale(TileSetSizeX * TileSizeX, TileSetSizeY * TileSizeY));
				await svg.SaveToStreamAsync(ms);
			}
			ms.Position = 0;
			var result = await _fileSaver.SaveAsync($"new {SelectedTemplate.Name}.svg", ms);
			if (result != null && !string.IsNullOrEmpty(result.FilePath))
			{
				var svgFile = new FileInfo(result.FilePath);
				_windowService.OpenDesignerWindow(svgFile);
			}
		}

		partial void OnSelectedTemplateChanged(TemplateRecord? value)
		{
			if (value == null) return;
			TileSizeX = value.TileSize.width;
			TileSizeY = value.TileSize.height;
			TileSetSizeX = value.TilesetSize.width / TileSizeX;
			TileSetSizeY = value.TilesetSize.height / TileSizeY;
		}
	}
}
