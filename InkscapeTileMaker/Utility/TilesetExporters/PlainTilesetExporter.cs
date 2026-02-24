using InkscapeTileMaker.Models;
using InkscapeTileMaker.Services;

namespace InkscapeTileMaker.Utility.TilesetExporters
{
	public class PlainTilesetExporter
	{
		private readonly ITilesetConnection _tilesetConnection;
		private readonly ITilesetRenderingService _tilesetRenderingService;

		public Scale TilesetSize { get; set; } = new(4, 4);

		public PlainTilesetExporter(ITilesetConnection tilesetConnection, ITilesetRenderingService tilesetRenderingService)
		{
			_tilesetConnection = tilesetConnection;
			_tilesetRenderingService = tilesetRenderingService;
		}

		public async Task<Tile?[]> ExportAsync(FileInfo destinationFile, TileData?[] srcTiles, Scale tilePixelSize, CancellationToken cancellationToken = default)
		{
			if (srcTiles.Length != TilesetSize.Width * TilesetSize.Height)
			{
				throw new ArgumentException($"Source tiles array length must be equal to the number of tiles in the tileset ({TilesetSize.Width * TilesetSize.Height}).", nameof(srcTiles));
			}

			var tilemap = new Tilemap(TilesetSize.Width, TilesetSize.Height);
			var positions = new Models.Rect(0, 0, TilesetSize.Width - 1, TilesetSize.Height - 1).GetPositions();

			Tile?[] orderedTiles = new Tile?[srcTiles.Length];
			foreach (var (position, tileData) in positions.Zip(srcTiles, (position, tile) => (position, tile)))
			{
				if (!tileData.HasValue) continue;
				tilemap.SetTileAt(position.x, position.y, tileData.Value);
				var tile = tileData.Value.Tile;
				orderedTiles[position.y * TilesetSize.Width + position.x] =
				new Tile()
				{
					Name = tile.Name,
					Type = TileType.Singular,
					Allignment = TileAlignment.Core,
					Row = position.y,
					Column = position.x
				};
			}

			using var fs = destinationFile.OpenWrite();
			var tilemapPackager = new TilemapPackager(_tilesetConnection, _tilesetRenderingService);
			await tilemapPackager.ExportTilemap(tilemap, tilePixelSize, fs, cancellationToken);
			return orderedTiles;
		}
	}
}
