using System;

namespace InkscapeTileMaker.Models
{
	/// <summary>
	/// Rotation and flip transformations that can be applied to a tile.
	/// Note that rotation is applied before flip.
	/// </summary>
	[Flags]
	public enum TileTransformation : byte
	{
		None = 0,

		// rotation in low 2 bits
		Rotate0 = 0,
		Rotate90 = 1,
		Rotate180 = 2,
		Rotate270 = 3,

		// flips in higher bits
		NoFlip = 0,
		FlipHorizontal = 1 << 2,  // 4
		FlipVertical = 1 << 3   // 8
	}
}
