namespace InkscapeTileMaker.Models.Tests
{
	public class TestTileTransformationHelpers
	{
		[Theory]
		[InlineData(TileAlignment.Core, TileAlignment.Core, TileTransformation.None)]
		[InlineData(TileAlignment.TopEdge, TileAlignment.TopEdge, TileTransformation.None)]
		[InlineData(TileAlignment.RightEdge, TileAlignment.RightEdge, TileTransformation.None)]
		[InlineData(TileAlignment.BottomEdge, TileAlignment.BottomEdge, TileTransformation.None)]
		[InlineData(TileAlignment.LeftEdge, TileAlignment.LeftEdge, TileTransformation.None)]
		[InlineData(TileAlignment.TopLeftOuterCorner, TileAlignment.TopLeftOuterCorner, TileTransformation.None)]
		[InlineData(TileAlignment.TopRightOuterCorner, TileAlignment.TopRightOuterCorner, TileTransformation.None)]
		[InlineData(TileAlignment.BottomRightOuterCorner, TileAlignment.BottomRightOuterCorner, TileTransformation.None)]
		[InlineData(TileAlignment.BottomLeftOuterCorner, TileAlignment.BottomLeftOuterCorner, TileTransformation.None)]
		[InlineData(TileAlignment.TopLeftInnerCorner, TileAlignment.TopLeftInnerCorner, TileTransformation.None)]
		[InlineData(TileAlignment.TopRightInnerCorner, TileAlignment.TopRightInnerCorner, TileTransformation.None)]
		[InlineData(TileAlignment.DiagonalTopLeftToBottomRight, TileAlignment.DiagonalTopLeftToBottomRight, TileTransformation.None)]
		[InlineData(TileAlignment.DiagonalTopRightToBottomLeft, TileAlignment.DiagonalTopRightToBottomLeft, TileTransformation.None)]
		public void GetTransformationForAlignment_Identity(TileAlignment source, TileAlignment target, TileTransformation expected)
		{
			var result = TileTransformationHelpers.GetTransformationForAlignment(source, target);
			Assert.Equal(expected, result);
		}

		[Theory]
		[InlineData(TileAlignment.TopEdge, TileAlignment.BottomEdge, TileTransformation.FlipVertical)]
		[InlineData(TileAlignment.RightEdge, TileAlignment.LeftEdge, TileTransformation.FlipHorizontal)]
		[InlineData(TileAlignment.BottomEdge, TileAlignment.TopEdge, TileTransformation.FlipVertical)]
		[InlineData(TileAlignment.LeftEdge, TileAlignment.RightEdge, TileTransformation.FlipHorizontal)]
		[InlineData(TileAlignment.DiagonalTopLeftToBottomRight, TileAlignment.DiagonalTopRightToBottomLeft, TileTransformation.FlipHorizontal)]
		[InlineData(TileAlignment.DiagonalTopRightToBottomLeft, TileAlignment.DiagonalTopLeftToBottomRight, TileTransformation.FlipHorizontal)]
		[InlineData(TileAlignment.TopLeftOuterCorner, TileAlignment.TopRightOuterCorner, TileTransformation.FlipHorizontal)]
		[InlineData(TileAlignment.TopRightOuterCorner, TileAlignment.TopLeftOuterCorner, TileTransformation.FlipHorizontal)]
		public void GetTransformationForAlignment_Flips(TileAlignment source, TileAlignment target, TileTransformation expected)
		{
			var result = TileTransformationHelpers.GetTransformationForAlignment(source, target);
			Assert.Equal(expected, result);
		}

		[Theory]
		[InlineData(TileAlignment.TopLeftOuterCorner, TileAlignment.BottomRightOuterCorner, TileTransformation.FlipVertical | TileTransformation.FlipHorizontal)]
		[InlineData(TileAlignment.TopRightOuterCorner, TileAlignment.BottomLeftOuterCorner, TileTransformation.FlipVertical | TileTransformation.FlipHorizontal)]
		[InlineData(TileAlignment.BottomRightOuterCorner, TileAlignment.TopLeftOuterCorner, TileTransformation.FlipVertical | TileTransformation.FlipHorizontal)]
		[InlineData(TileAlignment.BottomLeftOuterCorner, TileAlignment.TopRightOuterCorner, TileTransformation.FlipVertical | TileTransformation.FlipHorizontal)]
		[InlineData(TileAlignment.TopLeftInnerCorner, TileAlignment.BottomRightInnerCorner, TileTransformation.FlipVertical | TileTransformation.FlipHorizontal)]
		[InlineData(TileAlignment.TopRightInnerCorner, TileAlignment.BottomLeftInnerCorner, TileTransformation.FlipVertical | TileTransformation.FlipHorizontal)]
		[InlineData(TileAlignment.BottomRightInnerCorner, TileAlignment.TopLeftInnerCorner, TileTransformation.FlipVertical | TileTransformation.FlipHorizontal)]
		[InlineData(TileAlignment.BottomLeftInnerCorner, TileAlignment.TopRightInnerCorner, TileTransformation.FlipVertical | TileTransformation.FlipHorizontal)]
		public void GetTransformationForAlignment_DoubleFlips(TileAlignment source, TileAlignment target, TileTransformation expected)
		{
			var result = TileTransformationHelpers.GetTransformationForAlignment(source, target);
			Assert.Equal(expected, result);
		}

		[Theory]
		[InlineData(TileAlignment.TopEdge, TileAlignment.RightEdge, TileTransformation.Rotate90)]
		[InlineData(TileAlignment.RightEdge, TileAlignment.BottomEdge, TileTransformation.Rotate90)]
		[InlineData(TileAlignment.BottomEdge, TileAlignment.LeftEdge, TileTransformation.Rotate90)]
		[InlineData(TileAlignment.LeftEdge, TileAlignment.TopEdge, TileTransformation.Rotate90)]
		public void GetTransformationForAlignment_Rotations(TileAlignment source, TileAlignment target, TileTransformation expected)
		{
			var result = TileTransformationHelpers.GetTransformationForAlignment(source, target);
			Assert.Equal(expected, result);
		}
	}
}
