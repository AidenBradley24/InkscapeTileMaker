using InkscapeTileMaker.Models;

namespace InkscapeTileMaker.Services
{
	/// <summary>
	/// While ITilesetConnection is thread-safe, these methods are not, and should only be called from the UI thread.
	/// </summary>
	public static class TilesetConnectionExtensions
	{
		public static async Task FillTilesAsync(
			this ITilesetConnection connection,
			TilesetFillSettings settings,
			IProgress<double>? progressReporter = default,
			CancellationToken cancellationToken = default)
		{
			ArgumentNullException.ThrowIfNull(connection);
			var tileset = connection.Tileset
				?? throw new InvalidOperationException("Tileset is not loaded.");
			var file = connection.CurrentFile
				?? throw new InvalidOperationException("No file is currently loaded.");

			int maxRow = tileset.ImagePixelSize.Height / tileset.TilePixelSize.Height - 1;
			int maxCol = tileset.ImagePixelSize.Width / tileset.TilePixelSize.Width - 1;

			foreach (var tile in tileset)
			{
				if (tile.Row > maxRow) maxRow = tile.Row;
				if (tile.Column > maxCol) maxCol = tile.Column;
			}

			int totalRows = maxRow + 1;
			int totalCols = maxCol + 1;
			int totalTiles = totalRows * totalCols;

			var additions = new List<Tile>();
			for (int row = 0; row < totalRows; row++)
			{
				for (int col = 0; col < totalCols; col++)
				{
					cancellationToken.ThrowIfCancellationRequested();
					int index = row * totalCols + col;
					progressReporter?.Report(index / (double)totalTiles);

					var tile = tileset.GetTileAt(row, col);
					if (settings.HasFlag(TilesetFillSettings.ReplaceExisting) || tile is null)
					{
						var newTile = new Tile
						{
							Name = $"Tile {col},{row}",
							Type = TileType.Singular,
							Variant = TileVariant.Core,
							Alignment = TileAlignment.Core,
							Row = row,
							Column = col
						};

						if (!settings.HasFlag(TilesetFillSettings.FillEmptyTiles))
						{
							bool isEmpty = await connection.RenderingService.IsSegmentEmptyAsync(file, col * tileset.TilePixelSize.Width, row * tileset.TilePixelSize.Height,
								(col + 1) * tileset.TilePixelSize.Width, (row + 1) * tileset.TilePixelSize.Height, cancellationToken);
							if (isEmpty) continue;
						}

						additions.Add(newTile);
					}
				}
			}

			foreach (var tile in additions)
			{
				if (tileset.Contains(tile))
				{
					tileset.Update(tile);
				}
				else
				{
					tileset.Add(tile);
				}
			}
		}
	}

	[Flags]
	public enum TilesetFillSettings
	{
		None = 0,
		FillEmptyTiles = 1 << 0,
		ReplaceExisting = 1 << 1,
	}
}
