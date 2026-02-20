using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace InkscapeTileMaker.Models
{
	/// <summary>
	/// Manages a duel grid of materials.
	/// Coordinates (x, y) where (1, 1) is bottom right.
	/// </summary>
	public class DuelGridMaterialTilemap : ITilemap
	{
		private readonly Material?[,] _duelGridMaterial;

		public int Width { get; }
		public int Height { get; }

		public Rect TileGridRect => new Rect(0, 0, Width - 1, Height - 1);

		public Material? this[int x, int y]
		{
			get
			{
				if (x < 0 || x >= Width || y < 0 || y >= Height)
					return null;
				return _duelGridMaterial[x, y];
			}
			set
			{
				if (x < 0 || x >= Width || y < 0 || y >= Height)
					return;
				_duelGridMaterial[x, y] = value;
				TilesInAreaChanged(new Rect(x, y, x + 1, y + 1));
			}
		}

		public event Action<Rect> TilesInAreaChanged = delegate { };

		public DuelGridMaterialTilemap(int width, int height)
		{
			Width = width;
			Height = height;
			_duelGridMaterial = new Material[width, height];
		}

		public int Paint(Material material, IEnumerable<(int x, int y)> coordinatesOnDuelGrid)
		{
			int count = 0;
			foreach (var (x, y) in coordinatesOnDuelGrid)
			{
				if (x < 0 || x >= Width || y < 0 || y >= Height)
					continue;
				if (_duelGridMaterial[x, y] != null && _duelGridMaterial[x, y]!.Equals(material))
					continue;
				_duelGridMaterial[x, y] = material;
				count++;
			}
			TilesInAreaChanged(TileGridRect);
			return count;
		}

		public void Clear()
		{
			for (int y = 0; y < Height; y++)
			{
				for (int x = 0; x < Width; x++)
				{
					_duelGridMaterial[x, y] = null;
				}
			}
			TilesInAreaChanged(TileGridRect);
		}

		public IReadOnlyList<TileData> GetTilesAt(int x, int y)
		{
			var tiles = new List<TileData>();

			Material? GetMaterialOrNull(int gx, int gy)
			{
				if (gx < 0 || gx >= Width || gy < 0 || gy >= Height)
					return null;
				return _duelGridMaterial[gx, gy];
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

		public IEnumerable<IReadOnlyList<TileData>> GetTilesInArea(Rect area)
		{
			return area.GetPositions().Select(pos => GetTilesAt(pos.x, pos.y));
		}

		public IEnumerator<(IReadOnlyList<TileData> overlappingTiles, (int x, int y) position)> GetEnumerator()
		{
			return GetTilesInArea(TileGridRect)
				.Zip(TileGridRect.GetPositions(), (tiles, pos) => (overlappingTiles: tiles, position: pos))
				.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#region Duel Grid Material Tile Rules
		private static void CheckCoreRule(List<TileData> tiles, Material? topRight, Material? topLeft, Material? bottomRight, Material? bottomLeft, ref bool topRightUsed, ref bool topLeftUsed, ref bool bottomRightUsed, ref bool bottomLeftUsed)
		{
			bool quadsAvailable = !topRightUsed && !topLeftUsed && !bottomRightUsed && !bottomLeftUsed;
			bool ruleApplies = quadsAvailable &&
							   topRight != null && topLeft != null && bottomRight != null && bottomLeft != null &&
							   topRight.Equals(topLeft) && topRight.Equals(bottomRight) && topRight.Equals(bottomLeft);
			if (ruleApplies && topRight!.TryGetTileData(TileVariant.Core, TileAlignment.Core, out var tileData))
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
			if (ruleApplies && topLeft!.TryGetTileData(TileVariant.InnerCorner, TileAlignment.TopLeftInnerCorner, out var tileData))
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
			if (ruleApplies && topLeft!.TryGetTileData(TileVariant.InnerCorner, TileAlignment.TopRightInnerCorner, out tileData))
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
			if (ruleApplies && topLeft!.TryGetTileData(TileVariant.InnerCorner, TileAlignment.BottomLeftInnerCorner, out tileData))
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
			if (ruleApplies && topRight!.TryGetTileData(TileVariant.InnerCorner, TileAlignment.BottomRightInnerCorner, out tileData))
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
			if (ruleApplies && topLeft!.TryGetTileData(TileVariant.Diagonal, TileAlignment.DiagonalTopLeftToBottomRight, out var tileData))
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
			if (ruleApplies && topRight!.TryGetTileData(TileVariant.Diagonal, TileAlignment.DiagonalTopRightToBottomLeft, out tileData))
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
			if (ruleApplies && bottomLeft!.TryGetTileData(TileVariant.Edge, TileAlignment.TopEdge, out var tileData))
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
			if (ruleApplies && topLeft!.TryGetTileData(TileVariant.Edge, TileAlignment.BottomEdge, out tileData))
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
			if (ruleApplies && topRight!.TryGetTileData(TileVariant.Edge, TileAlignment.LeftEdge, out tileData))
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
			if (ruleApplies && topLeft!.TryGetTileData(TileVariant.Edge, TileAlignment.RightEdge, out tileData))
			{
				tiles.Add(tileData.Value);
				topLeftUsed = true;
				bottomLeftUsed = true;
			}
		}

		private static void CheckOuterCornerRule(List<TileData> tiles, Material? topRight, Material? topLeft, Material? bottomRight, Material? bottomLeft, ref bool topRightUsed, ref bool topLeftUsed, ref bool bottomRightUsed, ref bool bottomLeftUsed)
		{
			// Top-left corner
			if (!bottomRightUsed && bottomRight != null && bottomRight.TryGetTileData(TileVariant.OuterCorner, TileAlignment.TopLeftOuterCorner, out var tileData))
			{
				tiles.Add(tileData.Value);
				topRightUsed = true;
			}

			// Top-right corner
			if (!bottomLeftUsed && bottomLeft != null && bottomLeft.TryGetTileData(TileVariant.OuterCorner, TileAlignment.TopRightOuterCorner, out tileData))
			{
				tiles.Add(tileData.Value);
				topLeftUsed = true;
			}

			// Bottom-left corner
			if (!topRightUsed && topRight != null && topRight.TryGetTileData(TileVariant.OuterCorner, TileAlignment.BottomLeftOuterCorner, out tileData))
			{
				tiles.Add(tileData.Value);
				bottomRightUsed = true;
			}

			// Bottom-right corner
			if (!topLeftUsed && topLeft != null && topLeft.TryGetTileData(TileVariant.OuterCorner, TileAlignment.BottomRightOuterCorner, out tileData))
			{
				tiles.Add(tileData.Value);
				bottomLeftUsed = true;
			}
		}
		#endregion
	}
}
