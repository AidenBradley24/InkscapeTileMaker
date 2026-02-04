namespace InkscapeTileMaker.Models
{
	/// <summary>
	/// Refers to how tiles appear visually in a material tile
	/// </summary>
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

	public static class TileAlignmentExtensions
	{
		/// <summary>
		/// Get the direction that a tile surface is facing if it was a wall. For example, a tile with TopEdge alignment would be facing upwards, so it would return (0, -1). <br/>
		/// Note that upwards is negative y.
		/// </summary>
		public static (int x, int y) GetFaceDirection(this TileAlignment alignment)
		{
			return alignment switch
			{
				TileAlignment.TopEdge => (0, -1),
				TileAlignment.BottomEdge => (0, 1),
				TileAlignment.LeftEdge => (-1, 0),
				TileAlignment.RightEdge => (1, 0),
				TileAlignment.TopLeftOuterCorner => (-1, -1),
				TileAlignment.TopRightOuterCorner => (1, -1),
				TileAlignment.BottomRightOuterCorner => (1, 1),
				TileAlignment.BottomLeftOuterCorner => (-1, 1),
				TileAlignment.TopLeftInnerCorner => (1, 1),
				TileAlignment.TopRightInnerCorner => (-1, 1),
				TileAlignment.BottomLeftInnerCorner => (1, -1),
				TileAlignment.BottomRightInnerCorner => (-1, -1),
				TileAlignment.DiagonalTopLeftToBottomRight => (1, 1),
				TileAlignment.DiagonalTopRightToBottomLeft => (-1, 1),
				_ => (0, 0),
			};
		}
	}
}
