using UnityEngine;
using UnityEngine.Tilemaps;

namespace TileMaker
{
	[CreateAssetMenu(fileName = "new MaterialTile", menuName = "Scriptable Objects/Material Tile")]
	public class MaterialTile : ScriptableObject
	{
		public TileBase
			coreTile,

			topEdgeTile,
			rightEdgeTile,
			bottomEdgeTile,
			leftEdgeTile,

			topLeftOuterCornerTile,
			topRightOuterCornerTile,
			bottomRightOuterCornerTile,
			bottomLeftOuterCornerTile,

			topLeftInnerCornerTile,
			topRightInnerCornerTile,
			bottomRightInnerCornerTile,
			bottomLeftInnerCornerTile,

			diagonalTopLeftToBottomRightTile,
			diagonalTopRightToBottomLeftTile;

		public TileBase GetTile(TileAlignment alignment)
		{
			return alignment switch
			{
				TileAlignment.Core => coreTile,
				TileAlignment.TopEdge => topEdgeTile,
				TileAlignment.RightEdge => rightEdgeTile,
				TileAlignment.BottomEdge => bottomEdgeTile,
				TileAlignment.LeftEdge => leftEdgeTile,
				TileAlignment.TopLeftOuterCorner => topLeftOuterCornerTile,
				TileAlignment.TopRightOuterCorner => topRightOuterCornerTile,
				TileAlignment.BottomRightOuterCorner => bottomRightOuterCornerTile,
				TileAlignment.BottomLeftOuterCorner => bottomLeftOuterCornerTile,
				TileAlignment.TopLeftInnerCorner => topLeftInnerCornerTile,
				TileAlignment.TopRightInnerCorner => topRightInnerCornerTile,
				TileAlignment.BottomRightInnerCorner => bottomRightInnerCornerTile,
				TileAlignment.BottomLeftInnerCorner => bottomLeftInnerCornerTile,
				TileAlignment.DiagonalTopLeftToBottomRight => diagonalTopLeftToBottomRightTile,
				TileAlignment.DiagonalTopRightToBottomLeft => diagonalTopRightToBottomLeftTile,
				_ => null,
			};
		}
	}

	public enum TileAlignment
	{
		Core,

		TopEdge,
		RightEdge,
		BottomEdge,
		LeftEdge,

		TopLeftOuterCorner,
		TopRightOuterCorner,
		BottomRightOuterCorner,
		BottomLeftOuterCorner,

		TopLeftInnerCorner,
		TopRightInnerCorner,
		BottomRightInnerCorner,
		BottomLeftInnerCorner,

		DiagonalTopLeftToBottomRight,
		DiagonalTopRightToBottomLeft,
	}
}
