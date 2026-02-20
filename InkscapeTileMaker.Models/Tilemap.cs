using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace InkscapeTileMaker.Models
{
	/// <summary>
	/// Represents a grid of tiles. Multiple tiles can occupy the same position. Tiles are returned bottom first.
	/// </summary>
	public sealed class Tilemap : ITilemap
	{
		private readonly Dictionary<(int x, int y), List<TileData>> _tiles;
		private readonly Rect _rect;

		public Tilemap(int width, int height)
		{
			if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width), "Width must be greater than 0.");
			if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height), "Height must be greater than 0.");

			_rect = new Rect(0, 0, width - 1, height - 1);
			_tiles = new Dictionary<(int x, int y), List<TileData>>();
		}

		public Rect TileGridRect => _rect;

		public event Action<Rect> TilesInAreaChanged = delegate { };

		public IEnumerator<(IReadOnlyList<TileData> overlappingTiles, (int x, int y) position)> GetEnumerator()
		{
			return _tiles.Select(kvp => (overlappingTiles: (IReadOnlyList<TileData>)kvp.Value, position: kvp.Key)).GetEnumerator();
		}

		public IReadOnlyList<TileData> GetTilesAt(int x, int y)
		{
			return _tiles.TryGetValue((x, y), out var tiles) ? (IReadOnlyList<TileData>)tiles : Array.Empty<TileData>();
		}

		public IEnumerable<IReadOnlyList<TileData>> GetTilesInArea(Rect area)
		{
			foreach (var (x, y) in area.GetPositions())
			{
				yield return GetTilesAt(x, y);
			}
		}

		/// <summary>
		/// Sets the tile at the given position, replacing any existing tiles at that position.
		/// </summary>
		public void SetTileAt(int x, int y, TileData tile)
		{
			_tiles[(x, y)] = new List<TileData> { tile };
			TilesInAreaChanged(new Rect(x, y, x, y));
		}

		/// <summary>
		/// Adds a tile at the given position, keeping any existing tiles. New tiles are added on top.
		/// </summary>
		public void AddTileAt(int x, int y, TileData tile)
		{
			if (!_tiles.TryGetValue((x, y), out var tiles))
			{
				tiles = new List<TileData>();
				_tiles[(x, y)] = tiles;
			}

			tiles.Add(tile);
			TilesInAreaChanged(new Rect(x, y, x, y));
		}

		/// <summary>
		/// Removes a specific tile at the given position.
		/// </summary>
		/// <returns>True if the tile was removed; otherwise false.</returns>
		public bool RemoveTileAt(int x, int y, TileData tile)
		{
			if (!_tiles.TryGetValue((x, y), out var tiles))
			{
				return false;
			}

			var removed = tiles.Remove(tile);
			if (!removed)
			{
				return false;
			}

			if (tiles.Count == 0)
			{
				_tiles.Remove((x, y));
			}

			TilesInAreaChanged(new Rect(x, y, x, y));
			return true;
		}

		/// <summary>
		/// Removes all tiles at the given position.
		/// </summary>
		public void ClearTilesAt(int x, int y)
		{
			if (_tiles.Remove((x, y)))
			{
				TilesInAreaChanged(new Rect(x, y, x, y));
			}
		}

		/// <summary>
		/// Clears all tiles from the tilemap.
		/// </summary>
		public void Clear()
		{
			if (_tiles.Count == 0)
			{
				return;
			}

			_tiles.Clear();
			TilesInAreaChanged(TileGridRect);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
