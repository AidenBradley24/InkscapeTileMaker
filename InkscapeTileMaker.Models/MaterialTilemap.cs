using System;
using System.Collections;
using System.Collections.Generic;

namespace InkscapeTileMaker.Models
{
	/// <summary>
	/// Manages a grid of materials for use in placing material tiles on a duel grid.
	/// 
	/// Coordinates (x, y) where (1, 1) is bottom right.
	/// </summary>
	public class MaterialTilemap : IEnumerable<(int x, int y, Material? material)>
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

		public int Paint(Material material, IEnumerable<(int x, int y)> coordinates)
		{
			int count = 0;
			foreach (var (x, y) in coordinates)
			{
				if (x < 0 || x >= Width || y < 0 || y >= Height)
					continue;
				if (_grid[x, y] != null && _grid[x, y]!.Equals(material))
					continue;
				_grid[x, y] = material;
				count++;
			}
			return count;
		}

		/// <summary>
		/// Returns all tiles to compose a material tile on the duel grid at the given coordinates on the material grid in draw order (bottom first).
		/// </summary>
		public IReadOnlyList<TileData> GetTilesOnDuelGrid(int x, int y)
		{
			var tiles = new List<TileData>();

			Material? GetMaterialOrNull(int gx, int gy)
			{
				if (gx < 0 || gx >= Width || gy < 0 || gy >= Height)
					return null;
				return _grid[gx, gy];
			}

			Material? topLeft = GetMaterialOrNull(x, y);
			Material? topRight = GetMaterialOrNull(x + 1, y);
			Material? bottomLeft = GetMaterialOrNull(x, y + 1);
			Material? bottomRight = GetMaterialOrNull(x + 1, y + 1);

			// NOTE that some tile names are inverted from material quadrant names.
			// The tiles are named after how they appear visually (how they're facing) rather than how they're calculated.

			// No two rules can apply per quadrant, so once a rule is applied for a quadrant, no other rules can apply for that quadrant.
			bool topRightUsed = false;
			bool topLeftUsed = false;
			bool bottomRightUsed = false;
			bool bottomLeftUsed = false;

			CheckCoreRule(tiles, topRight, topLeft, bottomRight, bottomLeft, ref topRightUsed, ref topLeftUsed, ref bottomRightUsed, ref bottomLeftUsed);
			CheckInnerCornerRule(tiles, topRight, topLeft, bottomRight, bottomLeft, ref topRightUsed, ref topLeftUsed, ref bottomRightUsed, ref bottomLeftUsed);
			CheckDiagonalRule(tiles, topRight, topLeft, bottomRight, bottomLeft, ref topRightUsed, ref topLeftUsed, ref bottomRightUsed, ref bottomLeftUsed);
			CheckEdgeRule(tiles, topRight, topLeft, bottomRight, bottomLeft, ref topRightUsed, ref topLeftUsed, ref bottomRightUsed, ref bottomLeftUsed);
			CheckOuterCornerRule(tiles, topRight, topLeft, bottomRight, bottomLeft, ref topRightUsed, ref topLeftUsed, ref bottomRightUsed, ref bottomLeftUsed);
			return tiles;
		}

		private static void CheckCoreRule(List<TileData> tiles, Material? topRight, Material? topLeft, Material? bottomRight, Material? bottomLeft, ref bool topRightUsed, ref bool topLeftUsed, ref bool bottomRightUsed, ref bool bottomLeftUsed)
		{
			bool quadsAvailable = !topRightUsed && !topLeftUsed && !bottomRightUsed && !bottomLeftUsed;
			bool ruleApplies = quadsAvailable &&
							   topRight != null && topLeft != null && bottomRight != null && bottomLeft != null &&
							   topRight.Equals(topLeft) && topRight.Equals(bottomRight) && topRight.Equals(bottomLeft);
			if (ruleApplies && topRight!.TryGetTileData(TileType.MatCore, TileAlignment.Core, out var tileData))
			{
				tiles.Add(tileData.Value);
				topRightUsed = true;
				topLeftUsed = true;
				bottomRightUsed = true;
				bottomLeftUsed = true;
			}
		}

		private static void CheckInnerCornerRule(List<TileData> tiles, Material? topRight, Material? topLeft, Material? bottomRight, Material? bottomLeft, ref bool topRightUsed, ref bool topLeftUsed, ref bool bottomRightUsed, ref bool bottomLeftUsed)
		{
			// Top-left inner corner
			bool quadsAvailable = !topLeftUsed && !topRightUsed && !bottomLeftUsed;
			bool ruleApplies = quadsAvailable &&
							   topLeft != null && topRight != null && bottomLeft != null &&
							   topLeft.Equals(topRight) && topRight.Equals(bottomLeft);
			if (ruleApplies && topLeft!.TryGetTileData(TileType.MatInnerCorner, TileAlignment.TopRightInnerCorner, out var tileData))
			{
				tiles.Add(tileData.Value);
				topLeftUsed = true;
				topRightUsed = true;
				bottomLeftUsed = true;
			}

			// Top-right inner corner
			quadsAvailable = !topLeftUsed && !topRightUsed && !bottomRightUsed;
			ruleApplies = quadsAvailable &&
						  topLeft != null && topRight != null && bottomRight != null &&
						  topLeft.Equals(topRight) && topRight.Equals(bottomRight);
			if (ruleApplies && topLeft!.TryGetTileData(TileType.MatInnerCorner, TileAlignment.TopLeftInnerCorner, out tileData))
			{
				tiles.Add(tileData.Value);
				topLeftUsed = true;
				topRightUsed = true;
				bottomRightUsed = true;
			}

			// Bottom-left inner corner
			quadsAvailable = !topLeftUsed && !bottomLeftUsed && !bottomRightUsed;
			ruleApplies = quadsAvailable &&
						  topLeft != null && bottomLeft != null && bottomRight != null &&
						  topLeft.Equals(bottomLeft) && bottomLeft.Equals(bottomRight);
			if (ruleApplies && topLeft!.TryGetTileData(TileType.MatInnerCorner, TileAlignment.BottomLeftInnerCorner, out tileData))
			{
				tiles.Add(tileData.Value);
				topLeftUsed = true;
				bottomLeftUsed = true;
				bottomRightUsed = true;
			}

			// Bottom-right inner corner
			quadsAvailable = !topRightUsed && !bottomLeftUsed && !bottomRightUsed;
			ruleApplies = quadsAvailable &&
						  topRight != null && bottomLeft != null && bottomRight != null &&
						  topRight.Equals(bottomLeft) && bottomLeft.Equals(bottomRight);
			if (ruleApplies && topRight!.TryGetTileData(TileType.MatInnerCorner, TileAlignment.BottomRightInnerCorner, out tileData))
			{
				tiles.Add(tileData.Value);
				topRightUsed = true;
				bottomLeftUsed = true;
				bottomRightUsed = true;
			}
		}

		private static void CheckDiagonalRule(List<TileData> tiles, Material? topRight, Material? topLeft, Material? bottomRight, Material? bottomLeft, ref bool topRightUsed, ref bool topLeftUsed, ref bool bottomRightUsed, ref bool bottomLeftUsed)
		{
			// Top-left to bottom-right diagonal
			bool quadsAvailable = !topLeftUsed && !bottomRightUsed;
			bool ruleApplies = quadsAvailable &&
							   topLeft != null && bottomRight != null &&
							   topLeft.Equals(bottomRight);
			if (ruleApplies && topLeft!.TryGetTileData(TileType.MatDiagonal, TileAlignment.DiagonalTopLeftToBottomRight, out var tileData))
			{
				tiles.Add(tileData.Value);
				topLeftUsed = true;
				bottomRightUsed = true;
			}

			// Top-right to bottom-left diagonal
			quadsAvailable = !topRightUsed && !bottomLeftUsed;
			ruleApplies = quadsAvailable &&
						  topRight != null && bottomLeft != null &&
						  topRight.Equals(bottomLeft);
			if (ruleApplies && topRight!.TryGetTileData(TileType.MatDiagonal, TileAlignment.DiagonalTopRightToBottomLeft, out tileData))
			{
				tiles.Add(tileData.Value);
				topRightUsed = true;
				bottomLeftUsed = true;
			}
		}

		private static void CheckEdgeRule(List<TileData> tiles, Material? topRight, Material? topLeft, Material? bottomRight, Material? bottomLeft, ref bool topRightUsed, ref bool topLeftUsed, ref bool bottomRightUsed, ref bool bottomLeftUsed)
		{
			// Top edge
			bool quadsAvailable = !bottomLeftUsed && !bottomRightUsed;
			bool ruleApplies = quadsAvailable &&
							   bottomLeft != null && bottomRight != null &&
							   bottomLeft.Equals(bottomRight);
			if (ruleApplies && bottomLeft!.TryGetTileData(TileType.MatEdge, TileAlignment.TopEdge, out var tileData))
			{
				tiles.Add(tileData.Value);
				bottomLeftUsed = true;
				bottomRightUsed = true;
			}

			// Bottom edge
			quadsAvailable = !topLeftUsed && !topRightUsed;
			ruleApplies = quadsAvailable &&
						  topLeft != null && topRight != null &&
						  topLeft.Equals(topRight);
			if (ruleApplies && topLeft!.TryGetTileData(TileType.MatEdge, TileAlignment.BottomEdge, out tileData))
			{
				tiles.Add(tileData.Value);
				topLeftUsed = true;
				topRightUsed = true;
			}

			// Left edge
			quadsAvailable = !topRightUsed && !bottomRightUsed;
			ruleApplies = quadsAvailable &&
						  topRight != null && bottomRight != null &&
						  topRight.Equals(bottomRight);
			if (ruleApplies && topRight!.TryGetTileData(TileType.MatEdge, TileAlignment.LeftEdge, out tileData))
			{
				tiles.Add(tileData.Value);
				topRightUsed = true;
				bottomRightUsed = true;
			}

			// Right edge
			quadsAvailable = !topLeftUsed && !bottomLeftUsed;
			ruleApplies = quadsAvailable &&
						  topLeft != null && bottomLeft != null &&
						  topLeft.Equals(bottomLeft);
			if (ruleApplies && topLeft!.TryGetTileData(TileType.MatEdge, TileAlignment.RightEdge, out tileData))
			{
				tiles.Add(tileData.Value);
				topLeftUsed = true;
				bottomLeftUsed = true;
			}
		}

		private static void CheckOuterCornerRule(List<TileData> tiles, Material? topRight, Material? topLeft, Material? bottomRight, Material? bottomLeft, ref bool topRightUsed, ref bool topLeftUsed, ref bool bottomRightUsed, ref bool bottomLeftUsed)
		{
			// Top-left corner
			if (!bottomRightUsed && bottomRight != null && bottomRight.TryGetTileData(TileType.MatOuterCorner, TileAlignment.TopLeftOuterCorner, out var tileData))
			{
				tiles.Add(tileData.Value);
				topRightUsed = true;
			}

			// Top-right corner
			if (!bottomLeftUsed && bottomLeft != null && bottomLeft.TryGetTileData(TileType.MatOuterCorner, TileAlignment.TopRightOuterCorner, out tileData))
			{
				tiles.Add(tileData.Value);
				topLeftUsed = true;
			}

			// Bottom-left corner
			if (!topRightUsed && topRight != null && topRight.TryGetTileData(TileType.MatOuterCorner, TileAlignment.BottomLeftOuterCorner, out tileData))
			{
				tiles.Add(tileData.Value);
				bottomRightUsed = true;
			}

			// Bottom-right corner
			if (!topLeftUsed && topLeft != null && topLeft.TryGetTileData(TileType.MatOuterCorner, TileAlignment.BottomRightOuterCorner, out tileData))
			{
				tiles.Add(tileData.Value);
				bottomLeftUsed = true;
			}
		}

		public IEnumerator<(int x, int y, Material? material)> GetEnumerator()
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
	}
}
