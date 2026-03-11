using InkscapeTileMaker.Models;

namespace InkscapeTileMaker.Services
{
	public interface ITileset : ICollection<Tile>
	{
		public string Name { get; }
		public Scale TilePixelSize { get; }
		public Scale ImagePixelSize { get; }

		public Tile? GetTileAt(int row, int column);

		public Tile[] GetAllTiles();

		public void Update(Tile tile);
	}
}
