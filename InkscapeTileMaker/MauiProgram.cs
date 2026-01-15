using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Storage;
using InkscapeTileMaker.Pages;
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
            builder.Services.AddTransient<SvgConnectionService>();
			builder.Services.AddSingleton<IWindowService, WindowService>();

			
			builder.Services.AddSingleton<IInkscapeService, InkscapeService>();
			builder.Services.AddSingleton<ISettingsService, SettingsService>();
			builder.Services.AddSingleton<ISvgRenderingService, SvgRenderingService>();
			builder.Services.AddSingleton<ITempDirectoryService, TempDirectoryService>();
            builder.Services.AddSingleton<IFileSaver>(FileSaver.Default);

			// Windows + pages + viewmodels
			builder.Services.AddTransient<LandingWindow>()
                .AddTransient<LandingPage>()
                .AddTransient<LandingViewModel>();

            builder.Services.AddTransient<DesignerWindow>()
                .AddTransient<DesignerPage>()
                .AddTransient<DesignerViewModel>();

            return builder.Build();
        }
    }
}
