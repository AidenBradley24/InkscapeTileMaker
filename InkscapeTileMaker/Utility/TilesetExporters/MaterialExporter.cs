using InkscapeTileMaker.Models;
using InkscapeTileMaker.Services;

namespace InkscapeTileMaker.Utility.TilesetExporters
{
	public abstract class MaterialExporter
	{
		private readonly ITilesetConnection _tilesetConnection;
		private readonly ITilesetRenderingService _tilesetRenderingService;

		public MaterialExporter(string materialName, ITilesetConnection tilesetConnection, ITilesetRenderingService tilesetRenderingService)
		{
			_tilesetConnection = tilesetConnection;
			_tilesetRenderingService = tilesetRenderingService;
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
				var srcTile = Material.GetTile(dstTile.Variant, dstTile.Allignment) 
					?? throw new Exception($"Missing tile alignment: {dstTile.Allignment}");
				var transformation = TileTransformationHelpers.GetTransformationForAlignment(srcTile.Allignment, dstTile.Allignment);
				tilemap.SetTileAt(position.x, position.y, new TileData() { tile = srcTile, transformation = transformation });
			}

			using var fs = destinationFile.OpenWrite();
			var tilemapPackager = new TilemapPackager(_tilesetConnection, _tilesetRenderingService);
			await tilemapPackager.ExportTilemap(tilemap, tilePixelSize, fs, cancellationToken);
			return orderedTiles;
		}

		protected abstract Tile?[] GetOrderedTiles();
	}
}
