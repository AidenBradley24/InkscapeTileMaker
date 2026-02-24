using InkscapeTileMaker.Models;
using InkscapeTileMaker.ViewModels;

namespace InkscapeTileMaker.Services
{
	public interface ITileset : ICollection<Tile>
	{
		public string Name { get; }
		public Scale TilePixelSize { get; }
		public Scale ImagePixelSize { get; }

		public TileViewModel? GetTileViewModelAt(int row, int column, DesignerViewModel designerViewModel);

		public TileViewModel[] GetAllTileViewModels(DesignerViewModel designerViewModel);

		public Task FillTilesAsync(TilesetFillSettings settings, IProgress<double>? progressReporter = default);
	}

	[Flags]
	public enum TilesetFillSettings
	{
		None = 0,
		FillEmptyTiles = 1 << 0,
		ReplaceExisting = 1 << 1,
	}
}
