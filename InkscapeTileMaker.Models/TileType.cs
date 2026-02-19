using System;
using System.Collections.Generic;

namespace InkscapeTileMaker.Models
{
	// TODO refactor this to have Singular and Material. Subtypes will just be TileAlignment
	public enum TileType
	{
		Singular,
		DuelTileMaterial,
		// PipeMaterial // TODO have more material types
	}

	public static class TileTypeExtensions
	{
		public static bool IsMaterial(this TileType type)
		{
			return type != TileType.Singular;
		}

		public static IEnumerable<TileVariant> GetValidVariants(this TileType type)
		{
			return type switch
			{
				TileType.Singular => new[] { TileVariant.Core },
				TileType.DuelTileMaterial => new[] 
				{ 
					TileVariant.Core,
					TileVariant.Edge,
					TileVariant.InnerCorner,
					TileVariant.OuterCorner,
					TileVariant.Diagonal
				},
				_ => Array.Empty<TileVariant>()
			};
		}
	}
}
