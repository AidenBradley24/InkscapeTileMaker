using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Storage;
using InkscapeTileMaker.Services;
using InkscapeTileMaker.ViewModels;
using InkscapeTileMaker.Views;
using Microsoft.Extensions.Logging;
using SkiaSharp.Views.Maui.Controls.Hosting;

namespace InkscapeTileMaker
{
	public static class MauiProgram
	{
		public static MauiApp CreateMauiApp()
		{
			var builder = MauiApp.CreateBuilder();
			builder
				.UseMauiApp<App>()

				// Graphics / UI toolkits
				.UseSkiaSharp()
				.UseMauiCommunityToolkit()
				.ConfigureFonts(fonts =>
				{
					fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
					fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
				});

#if DEBUG
			builder.Logging.AddDebug();
#endif

			// Application services
			builder.Services.AddTransient<InkscapeSvgConnectionService>();
			builder.Services.AddSingleton<IWindowOpeningService, WindowOpeningService>();

			builder.Services.AddSingleton<IInkscapeService, InkscapeService>();
			builder.Services.AddSingleton<ISettingsService, SettingsService>();
			builder.Services.AddSingleton<ITilesetRenderingService, SvgRenderingService>();
			builder.Services.AddSingleton<ITempDirectoryService, TempDirectoryService>();
			builder.Services.AddSingleton<IFileSaver>(FileSaver.Default);
			builder.Services.AddSingleton<ITemplateService, TemplateService>();
			builder.Services.AddTransient<IAppPopupService, AppPopupService>();

			// Windows + pages + viewmodels
			builder.Services.AddTransient<LandingWindow>()
				.AddTransient<LandingPage, LandingViewModel>();

			builder.Services.AddTransient<DesignerWindow>()
				.AddTransient<DesignerPage, DesignerViewModel>();

			builder.Services.AddTransientPopup<Views.TextPopup, ViewModels.TextPopupViewModel>();

			return builder.Build();
		}
	}
}
