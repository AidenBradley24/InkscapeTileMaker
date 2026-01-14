using InkscapeTileMaker.ViewModels;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace InkscapeTileMaker.Pages
{
	public partial class DesignerPage : ContentPage
	{
		public DesignerPage(DesignerViewModel vm)
		{
			InitializeComponent();
			BindingContext = vm;
		}

		private void OnPreviewCanvasViewPaintSurface(object sender, SKPaintSurfaceEventArgs e)
		{
			if (BindingContext is not DesignerViewModel viewModel)
			{
				return;
			}

			var canvas = e.Surface.Canvas;
			var width = e.Info.Width;
			var height = e.Info.Height;

			MainThread.InvokeOnMainThreadAsync(() => viewModel.Render(canvas, width, height));
		}
	}
}