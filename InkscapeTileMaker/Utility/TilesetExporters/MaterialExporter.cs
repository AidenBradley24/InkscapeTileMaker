using InkscapeTileMaker.Models;
using InkscapeTileMaker.Services;

namespace InkscapeTileMaker.Utility.TilesetExporters
{
	public abstract class MaterialExporter
	{
		private readonly ITilesetConnection _tilesetConnection;

		public MaterialExporter(string materialName, ITilesetConnection tilesetConnection)
		{
			_tilesetConnection = tilesetConnection;
			if (_tilesetConnection.Tileset == null) throw new ArgumentException("Tileset connection must have a tileset.", nameof(tilesetConnection));
			Material = new Material(materialName, () => _tilesetConnection.Tileset);
		}

		public abstract TileType Type { get; }
		public abstract Scale TilesetSize { get; }

		public Material Material { get; }

		public async Task<Tile?[]> ExportAsync(FileInfo destinationFile, Scale tilePixelSize, CancellationToken cancellationToken = default)
		{
			var tilemap = new Tilemap(TilesetSize.Width, TilesetSize.Height);
			var positions = new Models.Rect(0, 0, TilesetSize.Width - 1, TilesetSize.Height - 1).GetPositions();
			var orderedTiles = GetOrderedTiles();
			foreach (var (position, dstTile) in positions.Zip(orderedTiles, (position, tile) => (position, tile)))
			{
				if (dstTile is null) continue;
				if (dstTile.Variant == TileVariant.Void)
				{
					tilemap.SetTileAt(position.x, position.y, new TileData() { Tile = dstTile, Transformation = TileTransformation.None });
					continue;
				}
				var srcTile = Material.GetTile(dstTile.Variant, dstTile.Alignment)
					?? throw new Exception($"Missing tile alignment: {dstTile.Alignment}");
				var transformation = TileTransformationHelpers.GetTransformationForAlignment(srcTile.Alignment, dstTile.Alignment);
				tilemap.SetTileAt(position.x, position.y, new TileData() { Tile = srcTile, Transformation = transformation });
			}

			using var fs = destinationFile.OpenWrite();
			var tilemapPackager = new TilemapPackager(_tilesetConnection);
			await tilemapPackager.ExportTilemap(tilemap, tilePixelSize, fs, cancellationToken);
			return orderedTiles;
		}

		protected abstract Tile?[] GetOrderedTiles();
	}
}
