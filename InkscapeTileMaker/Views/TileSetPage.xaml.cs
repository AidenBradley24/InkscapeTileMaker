using InkscapeTileMaker.ViewModels;

namespace InkscapeTileMaker.Pages;

public partial class TileSetPage : ContentPage
{
	public TileSetPage()
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