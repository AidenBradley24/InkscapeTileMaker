using InkscapeTileMaker.Views;

namespace InkscapeTileMaker
{
	public partial class App : Application
	{
		private readonly IServiceProvider _services;

		public App(IServiceProvider services)
		{
			InitializeComponent();
			_services = services;
		}

		protected override Window CreateWindow(IActivationState? activationState)
		{
			var landingWindow = _services.GetRequiredService<LandingWindow>();
			return landingWindow;
		}
	}
}