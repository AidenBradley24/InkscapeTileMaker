using System;

namespace InkscapeTileMaker.Models
{
	/// <summary>
	/// Rotation and flip transformations that can be applied to a tile.
	/// Note that flip is applied before rotation.
	/// </summary>
	[Flags]
	public enum TileTransformation : byte
	{
		None = 0,

		// first two bits are rotation
		Rotate0 = 0,
		Rotate90 = 1,
		Rotate180 = 2,
		Rotate270 = 3,

		// third and fourth bits are flip
		NoFlip = 0,
		FlipHorizontal = 4,
		FlipVertical = 8
	}
}
