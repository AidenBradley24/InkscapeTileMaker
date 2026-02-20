using System;
using System.Collections.Generic;

namespace InkscapeTileMaker.Models
{
	/// <summary>
	/// Represents a grid of tiles. Multiple tiles can occupy the same position.
	/// Tiles are returned bottom first.
	/// </summary>
	public interface ITilemap : IEnumerable<(IReadOnlyList<TileData> overlappingTiles, (int x, int y) position)>
	{
		public Rect TileGridRect { get; }

		public IReadOnlyList<TileData> GetTilesAt(int x, int y);

		public IEnumerable<IReadOnlyList<TileData>> GetTilesInArea(Rect area);

		public event Action<Rect> TilesInAreaChanged;
	}
}
