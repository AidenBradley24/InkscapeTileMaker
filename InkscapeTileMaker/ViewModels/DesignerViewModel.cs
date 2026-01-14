using CommunityToolkit.Mvvm.ComponentModel;
using InkscapeTileMaker.Services;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using CommunityToolkit.Mvvm.Input;

namespace InkscapeTileMaker.ViewModels
{
	public partial class DesignerViewModel : ObservableObject
	{
		private readonly SvgConnectionService _svgConnectionService;
		private readonly IWindowService _windowService;
		private readonly ISvgRenderingService _svgRenderingService;

		[ObservableProperty]
		private string fileName;

		public SvgConnectionService SvgConnectionService => _svgConnectionService;

		public DesignerViewModel(SvgConnectionService svgConnectionService, IWindowService windowService, ISvgRenderingService svgRenderingService)
		{
			_svgConnectionService = svgConnectionService;
			_svgConnectionService.DocumentLoaded += UpdateSVG;
			_windowService = windowService;
			_svgRenderingService = svgRenderingService;
		}

		~DesignerViewModel()
		{
			_svgConnectionService.DocumentLoaded -= UpdateSVG;
		}

		private void UpdateSVG(XDocument obj)
		{
			
		}

		public async Task Render(SKCanvas canvas, int width, int height)
		{
			// All drawing logic goes here; canvas is provided by SKCanvasView.

			const int tileSize = 20;

			using (var lightPaint = new SKPaint { Color = new SKColor(0xEE, 0xEE, 0xEE) })
			using (var darkPaint = new SKPaint { Color = new SKColor(0xCC, 0xCC, 0xCC) })
			{
				for (var y = 0; y < height; y += tileSize)
				{
					for (var x = 0; x < width; x += tileSize)
					{
						var useDark = ((x / tileSize) + (y / tileSize)) % 2 == 0;
						var rect = new SKRect(x, y, x + tileSize, y + tileSize);
						canvas.DrawRect(rect, useDark ? darkPaint : lightPaint);
					}
				}
			}

			if (_svgConnectionService.SvgFile == null)
			{
				using var textPaint = new SKPaint
				{
					Color = SKColors.Black,
					IsAntialias = true,
				};

				using var font = SKTypeface.FromFamilyName("Arial");
				using var skFont = new SKFont(font, 24);

				var message = "No SVG file loaded";
				var x = width / 2f;
				var y = height / 2f;

				canvas.DrawText(message, x, y, SKTextAlign.Center, skFont, textPaint);
				return;
			}

			using var stream = await _svgRenderingService.RenderSvgFile(_svgConnectionService.SvgFile);
			var picture = SKPicture.Deserialize(stream);
			canvas.DrawPicture(picture);
		}

		[RelayCommand]
		public async Task NewDesign()
		{
			_windowService.OpenDesignerWindow(svgFile: null);
		}

		[RelayCommand]
		public async Task OpenDesign()
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
				// Optionally handle non-SVG selection here (toast, alert, etc.)
				return;
			}

			var svgFile = new FileInfo(result.FullPath);
			_windowService.OpenDesignerWindow(svgFile);
		}
	}
}
