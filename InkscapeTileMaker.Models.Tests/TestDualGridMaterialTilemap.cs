namespace InkscapeTileMaker.Models.Tests
{
	public class TestDualGridMaterialTilemap
	{
		readonly List<Tile> tiles =
		[
			new Tile { Name = "CoreTile", MaterialName = "Brick", Variant=TileVariant.Core, Type = TileType.DualTileMaterial, Row = 0, Column = 0, Allignment = TileAlignment.Core, Priority = 1 },
			new Tile { Name = "EdgeTileRight", MaterialName = "Brick", Variant=TileVariant.Edge, Type = TileType.DualTileMaterial, Row = 0, Column = 1, Allignment = TileAlignment.RightEdge, Priority = 2 },
			new Tile { Name = "EdgeTileBottom", MaterialName = "Brick", Variant=TileVariant.Edge, Type = TileType.DualTileMaterial, Row = 0, Column = 1, Allignment = TileAlignment.BottomEdge, Priority = 1 },
			new Tile { Name = "OuterCornerTileTL", MaterialName = "Brick", Variant=TileVariant.OuterCorner, Type = TileType.DualTileMaterial, Row = 0, Column = 2, Allignment = TileAlignment.TopLeftOuterCorner, Priority = 1 },
			new Tile { Name = "InnerCornerTileBR", MaterialName = "Brick", Variant=TileVariant.InnerCorner, Type = TileType.DualTileMaterial, Row = 0, Column = 3, Allignment = TileAlignment.BottomRightInnerCorner, Priority = 1 },
			new Tile { Name = "DiagonalTileTLBR", MaterialName = "Brick", Variant=TileVariant.Diagonal, Type = TileType.DualTileMaterial, Row = 0, Column = 4, Allignment = TileAlignment.DiagonalTopLeftToBottomRight, Priority = 1 }
		];

		Material[] Materials => Material.GetAllMaterials(() => tiles).ToArray();

		[Fact]
		public void MaterialTilemap_Creation_Works()
		{
			var tilemap = new DualGridMaterialTilemap(10, 10);
			Assert.NotNull(tilemap);
			Assert.Equal(10, tilemap.Width);
			Assert.Equal(10, tilemap.Height);
		}

		[Fact]
		public void Placement_Works()
		{
			var tilemap = new DualGridMaterialTilemap(10, 10);
			tilemap[0, 0] = Materials.First(m => m.Name == "Brick");
			var retrievedMaterial = tilemap[0, 0];
			Assert.NotNull(retrievedMaterial);
			Assert.Equal("Brick", retrievedMaterial.Name);
		}

		[Fact]
		public void Placement_OutOfRange()
		{
			var tilemap = new DualGridMaterialTilemap(5, 5);
			tilemap[-1, 0] = Materials[0];
			tilemap[0, -1] = Materials[0];
			tilemap[5, 0] = Materials[0];
			tilemap[0, 5] = Materials[0];
			Assert.Null(tilemap[-1, 0]);
			Assert.Null(tilemap[0, -1]);
			Assert.Null(tilemap[5, 0]);
			Assert.Null(tilemap[0, 5]);
		}

		[Fact]
		public void GetTilesOnDualGrid_CoreRule_Applies()
		{
			var brick = Materials.First(m => m.Name == "Brick");
			var map = new DualGridMaterialTilemap(4, 4);

			map[0, 0] = brick;
			map[0, 1] = brick;
			map[1, 0] = brick;
			map[1, 1] = brick;

			var tilesOnDualGrid = map.GetTilesAt(0, 0);

			Assert.Single(tilesOnDualGrid);
			Assert.Equal("CoreTile", tilesOnDualGrid[0].tile.Name);
			Assert.Equal(TileTransformation.None, tilesOnDualGrid[0].transformation);
		}

		[Fact]
		public void GetTilesOnDualGrid_TopEdgeRule_Applies()
		{
			var brick = Materials.First(m => m.Name == "Brick");
			var map = new DualGridMaterialTilemap(4, 4);

			map[0, 0] = null;
			map[1, 0] = null;
			map[0, 1] = brick;
			map[1, 1] = brick;

			var tilesOnDualGrid = map.GetTilesAt(0, 0);

			Assert.Single(tilesOnDualGrid);
			Assert.Equal("EdgeTileRight", tilesOnDualGrid[0].tile.Name);
			Assert.Equal(TileTransformation.Rotate90 | TileTransformation.FlipVertical, tilesOnDualGrid[0].transformation);
			// 270 degree rotation is the same as 90 degrees + vertical flip
		}

		[Fact]
		public void GetTilesOnDualGrid_BottomEdgeRule_Applies()
		{
			var brick = Materials.First(m => m.Name == "Brick");
			var map = new DualGridMaterialTilemap(4, 4);

			map[0, 0] = brick;
			map[1, 0] = brick;
			map[0, 1] = null;
			map[1, 1] = null;

			var tilesOnDualGrid = map.GetTilesAt(0, 0);

			Assert.Single(tilesOnDualGrid);
			Assert.Equal("EdgeTileBottom", tilesOnDualGrid[0].tile.Name);
			Assert.Equal(TileTransformation.None, tilesOnDualGrid[0].transformation);
		}

		[Fact]
		public void GetTilesOnDualGrid_LeftEdgeRule_Applies()
		{
			var brick = Materials.First(m => m.Name == "Brick");
			var map = new DualGridMaterialTilemap(4, 4);

			map[0, 0] = null;
			map[1, 0] = brick;
			map[0, 1] = null;
			map[1, 1] = brick;

			var tilesOnDualGrid = map.GetTilesAt(0, 0);

			Assert.Single(tilesOnDualGrid);
			Assert.Equal("EdgeTileRight", tilesOnDualGrid[0].tile.Name);
			Assert.Equal(TileTransformation.FlipHorizontal, tilesOnDualGrid[0].transformation);
		}

		[Fact]
		public void GetTilesOnDualGrid_RightEdgeRule_Applies()
		{
			var brick = Materials.First(m => m.Name == "Brick");
			var map = new DualGridMaterialTilemap(4, 4);

			map[0, 0] = brick;
			map[1, 0] = null;
			map[0, 1] = brick;
			map[1, 1] = null;

			var tilesOnDualGrid = map.GetTilesAt(0, 0);

			Assert.Single(tilesOnDualGrid);
			Assert.Equal("EdgeTileRight", tilesOnDualGrid[0].tile.Name);
			Assert.Equal(TileTransformation.None, tilesOnDualGrid[0].transformation);
		}

		[Fact]
		public void GetTilesOnDualGrid_TopLeftOuterCornerRule_Applies()
		{
			var brick = Materials.First(m => m.Name == "Brick");
			var map = new DualGridMaterialTilemap(4, 4);

			map[0, 0] = null;
			map[1, 0] = null;
			map[0, 1] = null;
			map[1, 1] = brick;

			var tilesOnDualGrid = map.GetTilesAt(0, 0);

			Assert.Single(tilesOnDualGrid);
			Assert.Equal("OuterCornerTileTL", tilesOnDualGrid[0].tile.Name);
			Assert.Equal(TileTransformation.None, tilesOnDualGrid[0].transformation);
		}

		[Fact]
		public void GetTilesOnDualGrid_TopRightOuterCornerRule_Applies()
		{
			var brick = Materials.First(m => m.Name == "Brick");
			var map = new DualGridMaterialTilemap(4, 4);

			map[0, 0] = null;
			map[1, 0] = null;
			map[0, 1] = brick;
			map[1, 1] = null;

			var tilesOnDualGrid = map.GetTilesAt(0, 0);

			Assert.Single(tilesOnDualGrid);
			Assert.Equal("OuterCornerTileTL", tilesOnDualGrid[0].tile.Name);
			Assert.Equal(TileTransformation.FlipHorizontal, tilesOnDualGrid[0].transformation);
		}

		[Fact]
		public void GetTilesOnDualGrid_BottomLeftOuterCornerRule_Applies()
		{
			var brick = Materials.First(m => m.Name == "Brick");
			var map = new DualGridMaterialTilemap(4, 4);

			map[0, 0] = null;
			map[1, 0] = brick;
			map[0, 1] = null;
			map[1, 1] = null;

			var tilesOnDualGrid = map.GetTilesAt(0, 0);

			Assert.Single(tilesOnDualGrid);
			Assert.Equal("OuterCornerTileTL", tilesOnDualGrid[0].tile.Name);
			Assert.Equal(TileTransformation.FlipVertical, tilesOnDualGrid[0].transformation);
		}

		[Fact]
		public void GetTilesOnDualGrid_BottomRightOuterCornerRule_Applies()
		{
			var brick = Materials.First(m => m.Name == "Brick");
			var map = new DualGridMaterialTilemap(4, 4);

			map[0, 0] = brick;
			map[1, 0] = null;
			map[0, 1] = null;
			map[1, 1] = null;

			var tilesOnDualGrid = map.GetTilesAt(0, 0);

			Assert.Single(tilesOnDualGrid);
			Assert.Equal("OuterCornerTileTL", tilesOnDualGrid[0].tile.Name);
			Assert.Equal(TileTransformation.FlipHorizontal | TileTransformation.FlipVertical, tilesOnDualGrid[0].transformation);
		}

		[Fact]
		public void GetTilesOnDualGrid_TopLeftInnerCornerRule_Applies()
		{
			var brick = Materials.First(m => m.Name == "Brick");
			var map = new DualGridMaterialTilemap(4, 4);

			map[0, 0] = brick;
			map[1, 0] = brick;
			map[0, 1] = brick;
			map[1, 1] = null;

			var tilesOnDualGrid = map.GetTilesAt(0, 0);

			Assert.Single(tilesOnDualGrid);
			Assert.Equal("InnerCornerTileBR", tilesOnDualGrid[0].tile.Name);
			Assert.Equal(TileTransformation.FlipVertical | TileTransformation.FlipHorizontal, tilesOnDualGrid[0].transformation);
		}

		[Fact]
		public void GetTilesOnDualGrid_TopRightInnerCornerRule_Applies()
		{
			var brick = Materials.First(m => m.Name == "Brick");
			var map = new DualGridMaterialTilemap(4, 4);

			map[0, 0] = brick;
			map[1, 0] = brick;
			map[0, 1] = null;
			map[1, 1] = brick;

			var tilesOnDualGrid = map.GetTilesAt(0, 0);

			Assert.Single(tilesOnDualGrid);
			Assert.Equal("InnerCornerTileBR", tilesOnDualGrid[0].tile.Name);
			Assert.Equal(TileTransformation.FlipVertical, tilesOnDualGrid[0].transformation);
		}

		[Fact]
		public void GetTilesOnDualGrid_BottomLeftInnerCornerRule_Applies()
		{
			var brick = Materials.First(m => m.Name == "Brick");
			var map = new DualGridMaterialTilemap(4, 4);

			map[0, 0] = brick;
			map[1, 0] = null;
			map[0, 1] = brick;
			map[1, 1] = brick;

			var tilesOnDualGrid = map.GetTilesAt(0, 0);

			Assert.Single(tilesOnDualGrid);
			Assert.Equal("InnerCornerTileBR", tilesOnDualGrid[0].tile.Name);
			Assert.Equal(TileTransformation.FlipHorizontal, tilesOnDualGrid[0].transformation);
		}

		[Fact]
		public void GetTilesOnDualGrid_BottomRightInnerCornerRule_Applies()
		{
			var brick = Materials.First(m => m.Name == "Brick");
			var map = new DualGridMaterialTilemap(4, 4);

			map[0, 0] = null;
			map[1, 0] = brick;
			map[0, 1] = brick;
			map[1, 1] = brick;

			var tilesOnDualGrid = map.GetTilesAt(0, 0);

			Assert.Single(tilesOnDualGrid);
			Assert.Equal("InnerCornerTileBR", tilesOnDualGrid[0].tile.Name);
			Assert.Equal(TileTransformation.None, tilesOnDualGrid[0].transformation);
		}

		[Fact]
		public void GetTilesOnDualGrid_DiagonalTLBRRule_Applies()
		{
			var brick = Materials.First(m => m.Name == "Brick");
			var map = new DualGridMaterialTilemap(4, 4);

			map[0, 0] = brick;
			map[1, 0] = null;
			map[0, 1] = null;
			map[1, 1] = brick;

			var tilesOnDualGrid = map.GetTilesAt(0, 0);

			Assert.Single(tilesOnDualGrid);
			Assert.Equal("DiagonalTileTLBR", tilesOnDualGrid[0].tile.Name);
			Assert.Equal(TileTransformation.None, tilesOnDualGrid[0].transformation);
		}

		[Fact]
		public void GetTilesOnDualGrid_DiagonalTRBLRule_Applies()
		{
			var brick = Materials.First(m => m.Name == "Brick");
			var map = new DualGridMaterialTilemap(4, 4);

			map[0, 0] = null;
			map[1, 0] = brick;
			map[0, 1] = brick;
			map[1, 1] = null;

			var tilesOnDualGrid = map.GetTilesAt(0, 0);

			Assert.Single(tilesOnDualGrid);
			Assert.Equal("DiagonalTileTLBR", tilesOnDualGrid[0].tile.Name);
			Assert.Equal(TileTransformation.FlipHorizontal, tilesOnDualGrid[0].transformation);
		}
	}
}
