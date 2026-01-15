using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InkscapeTileMaker.Services;
using Microsoft.Maui.ApplicationModel;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace InkscapeTileMaker.ViewModels
{
	public partial class DesignerViewModel : ObservableObject
	{
		private readonly SvgConnectionService _svgConnectionService;
		private readonly IWindowService _windowService;
		private readonly ISvgRenderingService _svgRenderingService;

		[ObservableProperty]
		private string fileName;

		private SKBitmap? _renderedBitmap;

		public SvgConnectionService SvgConnectionService => _svgConnectionService;

		public event Action CanvasNeedsRedraw = delegate { };

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
			_renderedBitmap?.Dispose();
			_renderedBitmap = null;

			var svgFile = _svgConnectionService.SvgFile;
			if (svgFile == null) return;

			Task.Run(async () =>
			{
				using var stream = await _svgRenderingService.RenderSvgFile(svgFile);
				if (stream.CanSeek) stream.Position = 0;
				_renderedBitmap = SKBitmap.Decode(stream);
				await MainThread.InvokeOnMainThreadAsync(CanvasNeedsRedraw);
			});
		}

		public void RenderCanvas(SKCanvas canvas, int width, int height) // note that this must run synchronously
		{
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

			if (_renderedBitmap == null)
			{
				using var errorPaint = new SKPaint
				{
					Color = SKColors.Red,
					IsStroke = true,
					StrokeWidth = 3
				};
				canvas.DrawRect(new SKRect(10, 10, width - 10, height - 10), errorPaint);
				return;
			}

			var destPoint = new SKPoint((width - _renderedBitmap.Width) / 2f, (height - _renderedBitmap.Height) / 2f);
			canvas.DrawBitmap(_renderedBitmap, destPoint);
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
				return;
			}

			var svgFile = new FileInfo(result.FullPath);
			_windowService.OpenDesignerWindow(svgFile);
		}
	}
}
