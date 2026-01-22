using InkscapeTileMaker.ViewModels;
using SkiaSharp.Views.Maui;

namespace InkscapeTileMaker.Views
{
	public partial class DesignerPage : ContentPage
	{
		Point dragPoint;
		bool isPanning;

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

			viewModel.RenderCanvas(canvas, width, height);
		}

		private void OnCanvasViewPointerMoved(object sender, PointerEventArgs e)
		{
			if (BindingContext is not DesignerViewModel viewModel) return;

			// Panning with middle mouse
			if (isPanning)
			{
				// Work in screen space and then scale delta by inverse zoom
				var currentPoint = e.GetPosition(PreviewCanvasView);
				if (currentPoint is null) return;
				var deltaScreenX = currentPoint.Value.X - dragPoint.X;
				var deltaScreenY = currentPoint.Value.Y - dragPoint.Y;

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
				dragPoint = currentPoint.Value;

				return;
			}

			// Hover logic
			var pos = PointToPosition(e.GetPosition(PreviewCanvasView));
			viewModel.HoveredTile = pos;
		}

		private void OnCanvasViewPointerPressed(object sender, PointerEventArgs e)
		{
			if (BindingContext is not DesignerViewModel viewModel) return;

			// Start panning on middle button
			if (e.Button == ButtonsMask.Secondary)
			{
				isPanning = true;
				// Store start point in *screen* coordinates, independent of zoom
				dragPoint = e.GetPosition(PreviewCanvasView)!.Value;
				return;
			}

			// Normal selection for non-middle buttons
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
			var tileSize = viewModel.SvgConnectionService.TileSize;
			if (tileSize is null) return null;

			var previewRect = viewModel.GetImageRect();
			if (previewRect is null) return null;
			var rect = previewRect.Value;

			var zoom = (double)viewModel.SelectedZoomLevel;
			if (zoom <= 0) zoom = 1.0;

			// 1) Convert from screen coordinates to coordinates relative to the preview rect
			var px = point.Value.X - rect.Left + viewModel.PreviewOffset.X / 2;
			var py = point.Value.Y - rect.Top + viewModel.PreviewOffset.Y / 2;

			// 2) If outside the preview rect, ignore (no tile)
			if (px < 0 || py < 0 || px > rect.Width || py > rect.Height)
				return null;

			// 3) Convert preview-relative screen coords to logical canvas coords
			var logicalX = px / zoom * 2;
			var logicalY = py / zoom * 2;

			var tileWidth = (double)tileSize.Value.width;
			var tileHeight = (double)tileSize.Value.height;

			if (logicalX < 0 || logicalY < 0)
				return null;

			int row = (int)Math.Floor((logicalY - tileHeight / 2) / tileHeight);
			int col = (int)Math.Floor((logicalX - tileWidth / 2) / tileWidth);
			return (row, col);
		}

		private void PreviewModePickerChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (BindingContext is not DesignerViewModel viewModel) return;
			viewModel.ResetView();
		}
	}
}