using System;
using System.Collections;
using System.Collections.Generic;

namespace InkscapeTileMaker.Models
{
	/// <summary>
	/// Manages a grid of materials for use in placing material tiles on a duel grid.
	/// </summary>
	public class MaterialTilemap : IEnumerable<(int x, int y, Material material)>
	{
		private readonly Material?[,] _grid;

		public int Width { get; }
		public int Height { get; }

		public Material? this[int x, int y]
		{
			get
			{
				if (x < 0 || x >= Width || y < 0 || y >= Height)
					return null;
				return _grid[x, y];
			}
			set
			{
				if (x < 0 || x >= Width || y < 0 || y >= Height)
					return;
				_grid[x, y] = value;
				DuelGridAreaChanged((y, x, y + 1, x + 1));
			}
		}

		public event Action<(int top, int left, int bottom, int right)> DuelGridAreaChanged = delegate { };

		public MaterialTilemap(int width, int height)
		{
			Width = width;
			Height = height;
			_grid = new Material[width, height];
		}

		/// <summary>
		/// Returns all tiles to compose a material tile on the duel grid at the given coordinates on the material grid in draw order (bottom first).
		/// </summary>
		public IReadOnlyList<TileData> GetTilesOnDuelGrid(int x, int y)
		{
			var tiles = new List<TileData>();

			Material? topRight = _grid[x, y];
			Material? topLeft = _grid[x - 1, y];
			Material? bottomRight = _grid[x, y - 1];
			Material? bottomLeft = _grid[x - 1, y - 1];

			// Rules:

			// 0. No two rules can apply per corner, so once a rule is applied for a corner, no other rules can apply for that corner.
			bool topRightUsed = false;
			bool topLeftUsed = false;
			bool bottomRightUsed = false;
			bool bottomLeftUsed = false;

			// 1. If a material is present in all four corners, and that material has a tile of MatCore tile type, draw it.
			if (topRight != null && topLeft != null && bottomRight != null && bottomLeft != null &&
				topRight.Equals(topLeft) && topRight.Equals(bottomRight) && topRight.Equals(bottomLeft) && bottomLeft.Equals(bottomRight))
			{
				if (topRight.TryGetTileData(TileType.MatCore, TileAlignment.Core, out var tileData))
				{
					tiles.Add(tileData.Value);
					topRightUsed = true;
					topLeftUsed = true;
					bottomRightUsed = true;
					bottomLeftUsed = true;
				}
			}

			// 2. If a material is present in three corners, and that material has a tile of MatInnerCorner tile type for the inner corner, draw it.

			// Top-right corner filled (missing bottom-left)
			if (!topRightUsed && !topLeftUsed && !bottomRightUsed && topRight != null && topLeft != null && bottomRight != null &&
				topRight.Equals(topLeft) && topRight.Equals(bottomRight))
			{
				if (topRight.TryGetTileData(TileType.MatInnerCorner, TileAlignment.TopRightInnerCorner, out var tileData))
				{
					tiles.Add(tileData.Value);
					topRightUsed = true;
					topLeftUsed = true;
					bottomRightUsed = true;
					bottomLeftUsed = true;
				}
			}
			// Top-left corner filled (missing bottom-right)
			if (!topRightUsed && !topLeftUsed && !bottomLeftUsed && topLeft != null && topRight != null && bottomLeft != null &&
				topLeft.Equals(topRight) && topLeft.Equals(bottomLeft))
			{
				if (topLeft.TryGetTileData(TileType.MatInnerCorner, TileAlignment.TopLeftInnerCorner, out var tileData))
				{
					tiles.Add(tileData.Value);
					topRightUsed = true;
					topLeftUsed = true;
					bottomRightUsed = true;
					bottomLeftUsed = true;
				}
			}
			// Bottom-right corner filled (missing top-left)
			if (!topLeftUsed && !bottomRightUsed && !bottomLeftUsed && bottomRight != null && topRight != null && bottomLeft != null &&
				bottomRight.Equals(topRight) && bottomRight.Equals(bottomLeft))
			{
				if (bottomRight.TryGetTileData(TileType.MatInnerCorner, TileAlignment.BottomRightInnerCorner, out var tileData))
				{
					tiles.Add(tileData.Value);
					topRightUsed = true;
					topLeftUsed = true;
					bottomRightUsed = true;
					bottomLeftUsed = true;
				}
			}
			// Bottom-left corner filled (missing top-right)
			if (!topRightUsed && !bottomRightUsed && !bottomLeftUsed && bottomLeft != null && topLeft != null && bottomRight != null &&
				bottomLeft.Equals(topLeft) && bottomLeft.Equals(bottomRight))
			{
				if (bottomLeft.TryGetTileData(TileType.MatInnerCorner, TileAlignment.BottomLeftInnerCorner, out var tileData))
				{
					tiles.Add(tileData.Value);
					topRightUsed = true;
					topLeftUsed = true;
					bottomRightUsed = true;
					bottomLeftUsed = true;
				}
			}

			// 3. If a material is present in two diagonally opposite corners, and that material has a tile of MatDiagonal tile type for that diagonal, draw it.
			if (!topRightUsed && !bottomLeftUsed && topRight != null && bottomLeft != null && topRight.Equals(bottomLeft))
			{
				if (topRight.TryGetTileData(TileType.MatDiagonal, TileAlignment.DiagonalTopLeftToBottomRight, out var tileData))
				{
					tiles.Add(tileData.Value);
					topRightUsed = true;
					bottomLeftUsed = true;
				}
			}
			if (!topLeftUsed && !bottomRightUsed && topLeft != null && bottomRight != null && topLeft.Equals(bottomRight))
			{
				if (topLeft.TryGetTileData(TileType.MatDiagonal, TileAlignment.DiagonalTopRightToBottomLeft, out var tileData))
				{
					tiles.Add(tileData.Value);
					topLeftUsed = true;
					bottomRightUsed = true;
				}
			}

			// 4. If a material is present in two adjacent corners, and that material has a tile of MatEdge tile type for that edge, draw it.

			// Top edge: TopLeft + TopRight
			if (!topRightUsed && !topLeftUsed && topRight != null && topLeft != null && topRight.Equals(topLeft))
			{
				if (topRight.TryGetTileData(TileType.MatEdge, TileAlignment.TopEdge, out var tileData))
				{
					tiles.Add(tileData.Value);
					topRightUsed = true;
					topLeftUsed = true;
				}
			}

			// Bottom edge: BottomLeft + BottomRight
			if (!bottomLeftUsed && !bottomRightUsed && bottomLeft != null && bottomRight != null && bottomLeft.Equals(bottomRight))
			{
				if (bottomLeft.TryGetTileData(TileType.MatEdge, TileAlignment.BottomEdge, out var tileData))
				{
					tiles.Add(tileData.Value);
					bottomLeftUsed = true;
					bottomRightUsed = true;
				}
			}

			// Left edge: TopLeft + BottomLeft
			if (!topLeftUsed && !bottomLeftUsed && topLeft != null && bottomLeft != null && topLeft.Equals(bottomLeft))
			{
				if (topLeft.TryGetTileData(TileType.MatEdge, TileAlignment.LeftEdge, out var tileData))
				{
					tiles.Add(tileData.Value);
					topLeftUsed = true;
					bottomLeftUsed = true;
				}
			}

			// Right edge: TopRight + BottomRight
			if (!topRightUsed && !bottomRightUsed && topRight != null && bottomRight != null && topRight.Equals(bottomRight))
			{
				if (topRight.TryGetTileData(TileType.MatEdge, TileAlignment.RightEdge, out var tileData))
				{
					tiles.Add(tileData.Value);
					topRightUsed = true;
					bottomRightUsed = true;
				}
			}

			// 5. If a material is present in a single corner, and that material has a tile of MatOuterCorner tile type for that corner, draw it.

			// Top-right corner
			if (!topRightUsed && topRight != null && topRight.TryGetTileData(TileType.MatOuterCorner, TileAlignment.TopRightOuterCorner, out var topRightCornerTileData))
			{
				tiles.Add(topRightCornerTileData.Value);
				topRightUsed = true;
			}

			// Top-left corner
			if (!topLeftUsed && topLeft != null && topLeft.TryGetTileData(TileType.MatOuterCorner, TileAlignment.TopLeftOuterCorner, out var topLeftCornerTileData))
			{
				tiles.Add(topLeftCornerTileData.Value);
				topLeftUsed = true;
			}

			// Bottom-right corner
			if (!bottomRightUsed && bottomRight != null && bottomRight.TryGetTileData(TileType.MatOuterCorner, TileAlignment.BottomRightOuterCorner, out var bottomRightCornerTileData))
			{
				tiles.Add(bottomRightCornerTileData.Value);
				bottomRightUsed = true;
			}

			// Bottom-left corner
			if (!bottomLeftUsed && bottomLeft != null && bottomLeft.TryGetTileData(TileType.MatOuterCorner, TileAlignment.BottomLeftOuterCorner, out var bottomLeftCornerTileData))
			{
				tiles.Add(bottomLeftCornerTileData.Value);
				bottomLeftUsed = true;
			}

			return tiles;
		}

		public IEnumerator<(int x, int y, Material material)> GetEnumerator()
		{
			for (int y = 0; y < Height; y++)
			{
				for (int x = 0; x < Width; x++)
				{
					yield return (x, y, _grid[x, y]);
				}
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public void AddMaterialSample(int x, int y, Material material)
		{
			this[x, y] = material;
		}
	}
}
