using InkscapeTileMaker.Models;
using SkiaSharp;

namespace InkscapeTileMaker.Utility
{
	public static class TileDataExtensions
	{
		public static SKMatrix ToSKMatrix(this TileTransformation transformation)
		{
			var matrix = SKMatrix.CreateIdentity();

			if (transformation.HasFlag(TileTransformation.Rotate90))
			{
				matrix = SKMatrix.CreateRotationDegrees(90);
			}
			else if (transformation.HasFlag(TileTransformation.Rotate180))
			{
				matrix = SKMatrix.CreateRotationDegrees(180);
			}
			else if (transformation.HasFlag(TileTransformation.Rotate270))
			{
				matrix = SKMatrix.CreateRotationDegrees(270);
			}

			if (transformation.HasFlag(TileTransformation.FlipHorizontal))
			{
				var flipMatrix = SKMatrix.CreateScale(-1, 1);
				matrix = SKMatrix.Concat(matrix, flipMatrix);
			}

			if (transformation.HasFlag(TileTransformation.FlipVertical))
			{
				var flipMatrix = SKMatrix.CreateScale(1, -1);
				matrix = SKMatrix.Concat(matrix, flipMatrix);
			}

			return matrix;
		}
	}
}
