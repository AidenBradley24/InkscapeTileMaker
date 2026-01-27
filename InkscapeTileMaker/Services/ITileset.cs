using InkscapeTileMaker.Models;
using InkscapeTileMaker.Utility;
using InkscapeTileMaker.ViewModels;

namespace InkscapeTileMaker.Services
{
	public interface ITileset : ICollection<Tile>
	{
		public Scale TileSize { get; }
		public Scale Size { get; }

		public TileViewModel? GetTileViewModelAt(int row, int column, DesignerViewModel designerViewModel);

		public TileViewModel[] GetAllTileViewModels(DesignerViewModel designerViewModel);

		public Task FillTilesAsync(TilesetFillSettings settings);
	}

	[Flags]
	public enum TilesetFillSettings
	{
		None = 0,
		FillEmptyTiles = 1 << 0,
		ReplaceExisting = 1 << 1,
	}
}
