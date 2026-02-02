using CommunityToolkit.Maui.Extensions;
using InkscapeTileMaker.Services;
using InkscapeTileMaker.ViewModels;
using SkiaSharp.Views.Maui;

namespace InkscapeTileMaker.Views
{
	public partial class DesignerPage : ContentPage
	{
		Point dragPoint;
		bool isPanning;
		int _canvasPixelWidth;
		int _canvasPixelHeight;

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
			if (BindingContext is not DesignerViewModel viewModel) return;

			var canvas = e.Surface.Canvas;
			var width = e.Info.Width;
			var height = e.Info.Height;
			_canvasPixelWidth = width;
			_canvasPixelHeight = height;
			viewModel.RenderCanvas(canvas, width, height);
		}

		private void OnCanvasViewPointerMoved(object sender, PointerEventArgs e)
		{
			if (BindingContext is not DesignerViewModel viewModel) return;

			if (isPanning)
			{
				var currentPoint = e.GetPosition(PreviewCanvasView);
				if (currentPoint is null) return;
				var deltaScreenX = currentPoint.Value.X - dragPoint.X;
				var deltaScreenY = currentPoint.Value.Y - dragPoint.Y;

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

				dragPoint = currentPoint.Value;
				return;
			}

			var pos = PointToPosition(e.GetPosition(PreviewCanvasView));
			viewModel.HoveredTile = pos;
		}

		private void OnCanvasViewPointerPressed(object sender, PointerEventArgs e)
		{
			if (BindingContext is not DesignerViewModel viewModel) return;

			if (e.Button == ButtonsMask.Secondary)
			{
				isPanning = true;
				dragPoint = e.GetPosition(PreviewCanvasView)!.Value;
				return;
			}

			var pos = PointToPosition(e.GetPosition(PreviewCanvasView));
			if (pos is null)
			{
				viewModel.SelectTileFromPreviewAt(-1, -1);
				return;
			}
			viewModel.SelectTileFromPreviewAt(pos.Value.row, pos.Value.col);
		}

		private void OnCanvasViewPointerReleased(object sender, PointerEventArgs e)
		{
			// Stop panning when middle button is released
			if (e.Button == ButtonsMask.Secondary && isPanning)
			{
				isPanning = false;
			}
		}

		private (int row, int col)? PointToPosition(Point? point)
		{
			if (BindingContext is not DesignerViewModel viewModel) return null;
			if (point is null) return null;
			var tileSize = viewModel.TileSize;

			var previewRect = viewModel.GetPreviewRect();
			if (previewRect is null) return null;

			var unscaledRect = viewModel.GetUnscaledPreviewRect();
			if (unscaledRect is null) return null;

			var zoom = (double)viewModel.SelectedZoomLevel;
			if (zoom <= 0) zoom = 1.0;

			if (PreviewCanvasView.Width <= 0 || PreviewCanvasView.Height <= 0 || _canvasPixelWidth <= 0 || _canvasPixelHeight <= 0)
			{
				return null;
			}

			double scaleX = _canvasPixelWidth / PreviewCanvasView.Width;
			double scaleY = _canvasPixelHeight / PreviewCanvasView.Height;
			double canvasX = point.Value.X * scaleX;
			double canvasY = point.Value.Y * scaleY;
			double px = (canvasX - previewRect.Value.Left) / previewRect.Value.Width;
			double py = (canvasY - previewRect.Value.Top) / previewRect.Value.Height;
			double logicalX = px * previewRect.Value.Width / zoom;
			double logicalY = py * previewRect.Value.Height / zoom;

			var tileWidth = (double)tileSize.width;
			var tileHeight = (double)tileSize.height;

			int row = (int)Math.Round((logicalY - tileHeight / 2) / tileHeight);
			int col = (int)Math.Round((logicalX - tileWidth / 2) / tileWidth);

			return (row, col);
		}

		private void PreviewModePickerChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (BindingContext is not DesignerViewModel viewModel) return;
			viewModel.ResetView();
		}
	}
}