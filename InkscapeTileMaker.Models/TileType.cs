namespace InkscapeTileMaker.Models
{
	// TODO refactor this to have Singular and Material. Subtypes will just be TileAlignment
	public enum TileType
	{
		Singular,
		MatOuterCorner,
		MatInnerCorner,
		MatEdge,
		MatCore,
		MatDiagonal
	}
}
