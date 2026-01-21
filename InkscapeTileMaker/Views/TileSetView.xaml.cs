using InkscapeTileMaker.ViewModels;

namespace InkscapeTileMaker.Views;

public partial class TileSetView : ContentView
{
	public TileSetView()
	{
		InitializeComponent();
	}

	private void OnSelectedTileNameCompleted(object sender, EventArgs e)
	{
		if (BindingContext is DesignerViewModel vm && vm.SelectedTile is TileViewModel tileVm)
		{
			tileVm.IsEditingSelectedTileName = false;
		}
	}
}