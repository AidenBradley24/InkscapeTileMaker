using System;
using System.Collections.Generic;

namespace InkscapeTileMaker.Models
{
	public enum TileVariant
	{
		Core,
		Edge,
		InnerCorner,
		OuterCorner,
		Diagonal,
		Void
	}

	public static class TileVariantExtensions
	{
		public static IEnumerable<TileAlignment> GetValidAllignments(this TileVariant variant)
		{
			return variant switch
			{
				TileVariant.Core => new[]
				{
					TileAlignment.Core,
				},
				TileVariant.Void => new[]
				{
					TileAlignment.Core
				},
				TileVariant.Edge => new[]
				{
					TileAlignment.TopEdge,
					TileAlignment.RightEdge,
					TileAlignment.BottomEdge,
					TileAlignment.LeftEdge,
				},
				TileVariant.InnerCorner => new[]
				{
					TileAlignment.TopLeftInnerCorner,
					TileAlignment.TopRightInnerCorner,
					TileAlignment.BottomRightInnerCorner,
					TileAlignment.BottomLeftInnerCorner,
				},
				TileVariant.OuterCorner => new[]
				{
					TileAlignment.TopLeftOuterCorner,
					TileAlignment.TopRightOuterCorner,
					TileAlignment.BottomRightOuterCorner,
					TileAlignment.BottomLeftOuterCorner,
				},
				TileVariant.Diagonal => new[]
				{
					TileAlignment.DiagonalTopLeftToBottomRight,
					TileAlignment.DiagonalTopRightToBottomLeft,
				},
				_ => Array.Empty<TileAlignment>(),
			};
		}
	}
}
