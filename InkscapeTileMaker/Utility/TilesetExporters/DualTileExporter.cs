using InkscapeTileMaker.Models;
using InkscapeTileMaker.Services;

namespace InkscapeTileMaker.Utility.TilesetExporters
{
	public class DualTileExporter(string materialName, ITilesetConnection tilesetConnection)
		: MaterialExporter(materialName, tilesetConnection)
	{
		public override TileType Type => TileType.DualTileMaterial;

		public override Scale TilesetSize => new(4, 4);

		protected override Tile?[] GetOrderedTiles()
		{
			return
			[
				// Row 0
				new()
				{
					Name = $"{Material.Name}_0",
					MaterialName = Material.Name,
					Row = 0,
					Column = 0,
					Type = TileType.DualTileMaterial,
					Variant = TileVariant.OuterCorner,
					Alignment = TileAlignment.TopRightOuterCorner
				},
				new()
				{
					Name = $"{Material.Name}_1",
					MaterialName = Material.Name,
					Row = 0,
					Column = 1,
					Type = TileType.DualTileMaterial,
					Variant = TileVariant.Edge,
					Alignment = TileAlignment.LeftEdge
				},
				new()
				{
					Name = $"{Material.Name}_2",
					MaterialName = Material.Name,
					Row = 0,
					Column = 2,
					Type = TileType.DualTileMaterial,
					Variant = TileVariant.InnerCorner,
					Alignment = TileAlignment.BottomLeftInnerCorner
				},
				new()
				{
					Name = $"{Material.Name}_3",
					MaterialName = Material.Name,
					Row = 0,
					Column = 3,
					Type = TileType.DualTileMaterial,
					Variant = TileVariant.Edge,
					Alignment = TileAlignment.TopEdge
				},

				// Row 1
				new()
				{
					Name = $"{Material.Name}_4",
					MaterialName = Material.Name,
					Row = 1,
					Column = 0,
					Type = TileType.DualTileMaterial,
					Variant = TileVariant.Diagonal,
					Alignment = TileAlignment.DiagonalTopLeftToBottomRight
				},
				new()
				{
					Name = $"{Material.Name}_5",
					MaterialName = Material.Name,
					Row = 1,
					Column = 1,
					Type = TileType.DualTileMaterial,
					Variant = TileVariant.InnerCorner,
					Alignment = TileAlignment.BottomRightInnerCorner
				},
				new()
				{
					Name = $"{Material.Name}_6",
					MaterialName = Material.Name,
					Row = 1,
					Column = 2,
					Type = TileType.DualTileMaterial,
					Variant = TileVariant.Core,
					Alignment = TileAlignment.Core
				},
				new()
				{
					Name = $"{Material.Name}_7",
					MaterialName = Material.Name,
					Row = 1,
					Column = 3,
					Type = TileType.DualTileMaterial,
					Variant = TileVariant.InnerCorner,
					Alignment = TileAlignment.TopLeftInnerCorner
				},

				// Row 2
				new()
				{
					Name = $"{Material.Name}_8",
					MaterialName = Material.Name,
					Row = 2,
					Column = 0,
					Type = TileType.DualTileMaterial,
					Variant = TileVariant.OuterCorner,
					Alignment = TileAlignment.BottomLeftOuterCorner
				},
				new()
				{
					Name = $"{Material.Name}_9",
					MaterialName = Material.Name,
					Row = 2,
					Column = 1,
					Type = TileType.DualTileMaterial,
					Variant = TileVariant.Edge,
					Alignment = TileAlignment.BottomEdge
				},
				new()
				{
					Name = $"{Material.Name}_10",
					MaterialName = Material.Name,
					Row = 2,
					Column = 2,
					Type = TileType.DualTileMaterial,
					Variant = TileVariant.InnerCorner,
					Alignment = TileAlignment.TopRightInnerCorner
				},
				new()
				{
					Name = $"{Material.Name}_11",
					MaterialName = Material.Name,
					Row = 2,
					Column = 3,
					Type = TileType.DualTileMaterial,
					Variant = TileVariant.Edge,
					Alignment = TileAlignment.RightEdge
				},

				// Row 3
				new()
				{
					Name = $"{Material.Name}_12",
					MaterialName = Material.Name,
					Row = 3,
					Column = 1,
					Variant = TileVariant.Void,
				},
				new()
				{
					Name = $"{Material.Name}_13",
					MaterialName = Material.Name,
					Row = 3,
					Column = 1,
					Type = TileType.DualTileMaterial,
					Variant = TileVariant.OuterCorner,
					Alignment = TileAlignment.TopLeftOuterCorner
				},
				new()
				{
					Name = $"{Material.Name}_14",
					MaterialName = Material.Name,
					Row = 3,
					Column = 2,
					Type = TileType.DualTileMaterial,
					Variant = TileVariant.Diagonal,
					Alignment = TileAlignment.DiagonalTopRightToBottomLeft
				},
				new()
				{
					Name = $"{Material.Name}_15",
					MaterialName = Material.Name,
					Row = 3,
					Column = 3,
					Type = TileType.DualTileMaterial,
					Variant = TileVariant.OuterCorner,
					Alignment = TileAlignment.BottomRightOuterCorner
				}
			];
		}
	}
}
