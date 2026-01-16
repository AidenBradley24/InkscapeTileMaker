using InkscapeTileMaker.ViewModels;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace InkscapeTileMaker.Pages
{
	public partial class DesignerPage : ContentPage
	{
		Point dragPoint;

		public DesignerPage(DesignerViewModel vm)
		{
			InitializeComponent();
			BindingContext = vm;
			vm.CanvasNeedsRedraw += PreviewCanvasView.InvalidateSurface;
		}

		~DesignerPage()
		{
			if (BindingContext is DesignerViewModel viewModel)
			{
				viewModel.CanvasNeedsRedraw -= PreviewCanvasView.InvalidateSurface;
			}
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

			viewModel.RenderCanvas(canvas, width, height);
		}

		private void OnCanvasViewPanUpdated(object sender, PanUpdatedEventArgs e)
		{
			if (BindingContext is not DesignerViewModel viewModel)
			{
				return;
			}

			switch (e.StatusType)
			{
				case GestureStatus.Started:
					// Store start point in *screen* coordinates, independent of zoom
					dragPoint = new Point(e.TotalX, e.TotalY);
					break;

				case GestureStatus.Running:
					// Work in screen space and then scale delta by inverse zoom
					var currentPoint = new Point(e.TotalX, e.TotalY);
					var deltaScreenX = currentPoint.X - dragPoint.X;
					var deltaScreenY = currentPoint.Y - dragPoint.Y;

					// Convert to canvas space respecting zoom
					var zoom = (float)viewModel.SelectedZoomLevel;
					if (zoom <= 0)
					{
						zoom = 1f;
					}

					var deltaCanvasX = (float)(deltaScreenX / zoom);
					var deltaCanvasY = (float)(deltaScreenY / zoom);

					viewModel.PreviewOffset = new PointF(
						viewModel.PreviewOffset.X + deltaCanvasX,
						viewModel.PreviewOffset.Y + deltaCanvasY
					);

					// Update dragPoint so movement is continuous
					dragPoint = currentPoint;
					break;
			}
		}
	}
}