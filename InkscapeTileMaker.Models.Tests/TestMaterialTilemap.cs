namespace InkscapeTileMaker.Models.Tests
{
	public class TestMaterialTilemap
	{
		readonly List<Tile> tiles =
		[
			new Tile { Name = "CoreTile", MaterialName = "Brick", Type = TileType.MatCore, Row = 0, Column = 0, Allignment = TileAlignment.Core, Priority = 1 },
			new Tile { Name = "EdgeTileRight", MaterialName = "Brick", Type = TileType.MatEdge, Row = 0, Column = 1, Allignment = TileAlignment.RightEdge, Priority = 2 },
			new Tile { Name = "EdgeTileBottom", MaterialName = "Brick", Type = TileType.MatEdge, Row = 0, Column = 1, Allignment = TileAlignment.BottomEdge, Priority = 1 },
			new Tile { Name = "OuterCornerTileTL", MaterialName = "Brick", Type = TileType.MatOuterCorner, Row = 0, Column = 2, Allignment = TileAlignment.TopLeftOuterCorner, Priority = 1 },
			new Tile { Name = "InnerCornerTileBR", MaterialName = "Brick", Type = TileType.MatInnerCorner, Row = 0, Column = 3, Allignment = TileAlignment.BottomRightInnerCorner, Priority = 1 },
			new Tile { Name = "DiagonalTileTLBR", MaterialName = "Brick", Type = TileType.MatDiagonal, Row = 0, Column = 4, Allignment = TileAlignment.DiagonalTopLeftToBottomRight, Priority = 1 }
		];

		Material[] Materials => Material.GetAllMaterials(() => tiles).ToArray();

		[Fact]
		public void MaterialTilemap_Creation_Works()
		{
			var tilemap = new MaterialTilemap(10, 10);
			Assert.NotNull(tilemap);
			Assert.Equal(10, tilemap.Width);
			Assert.Equal(10, tilemap.Height);
		}

		[Fact]
		public void Placement_Works()
		{
			var tilemap = new MaterialTilemap(10, 10);
			tilemap[0, 0] = Materials.First(m => m.Name == "Brick");
			var retrievedMaterial = tilemap[0, 0];
			Assert.NotNull(retrievedMaterial);
			Assert.Equal("Brick", retrievedMaterial.Name);
		}

		[Fact]
		public void Placement_OutOfRange()
		{
			var tilemap = new MaterialTilemap(5, 5);
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
		public void GetTilesOnDuelGrid_CoreRule_Applies()
		{
			var brick = Materials.First(m => m.Name == "Brick");
			var map = new MaterialTilemap(4, 4);

			map[0, 0] = brick;
			map[0, 1] = brick;
			map[1, 0] = brick;
			map[1, 1] = brick;

			var tilesOnDuelGrid = map.GetTilesOnDuelGrid(0, 0);

			Assert.Single(tilesOnDuelGrid);
			Assert.Equal("CoreTile", tilesOnDuelGrid[0].tile.Name);
			Assert.Equal(TileTransformation.None, tilesOnDuelGrid[0].transformation);
		}

		[Fact]
		public void GetTilesOnDuelGrid_TopEdgeRule_Applies()
		{
			var brick = Materials.First(m => m.Name == "Brick");
			var map = new MaterialTilemap(4, 4);

			map[0, 0] = null;
			map[1, 0] = null;
			map[0, 1] = brick;
			map[1, 1] = brick;

			var tilesOnDuelGrid = map.GetTilesOnDuelGrid(0, 0);

			Assert.Single(tilesOnDuelGrid);
			Assert.Equal("EdgeTileRight", tilesOnDuelGrid[0].tile.Name);
			Assert.Equal(TileTransformation.Rotate90, tilesOnDuelGrid[0].transformation);
		}

		[Fact]
		public void GetTilesOnDuelGrid_BottomEdgeRule_Applies()
		{
			var brick = Materials.First(m => m.Name == "Brick");
			var map = new MaterialTilemap(4, 4);

			map[0, 0] = brick;
			map[1, 0] = brick;
			map[0, 1] = null;
			map[1, 1] = null;

			var tilesOnDuelGrid = map.GetTilesOnDuelGrid(0, 0);

			Assert.Single(tilesOnDuelGrid);
			Assert.Equal("EdgeTileBottom", tilesOnDuelGrid[0].tile.Name);
			Assert.Equal(TileTransformation.None, tilesOnDuelGrid[0].transformation);
		}

		[Fact]
		public void GetTilesOnDuelGrid_LeftEdgeRule_Applies()
		{
			var brick = Materials.First(m => m.Name == "Brick");
			var map = new MaterialTilemap(4, 4);

			map[0, 0] = null;
			map[1, 0] = brick;
			map[0, 1] = null;
			map[1, 1] = brick;

			var tilesOnDuelGrid = map.GetTilesOnDuelGrid(0, 0);

			Assert.Single(tilesOnDuelGrid);
			Assert.Equal("EdgeTileRight", tilesOnDuelGrid[0].tile.Name);
			Assert.Equal(TileTransformation.FlipHorizontal, tilesOnDuelGrid[0].transformation);
		}

		[Fact]
		public void GetTilesOnDuelGrid_RightEdgeRule_Applies()
		{
			var brick = Materials.First(m => m.Name == "Brick");
			var map = new MaterialTilemap(4, 4);

			map[0, 0] = brick;
			map[1, 0] = null;
			map[0, 1] = brick;
			map[1, 1] = null;

			var tilesOnDuelGrid = map.GetTilesOnDuelGrid(0, 0);

			Assert.Single(tilesOnDuelGrid);
			Assert.Equal("EdgeTileRight", tilesOnDuelGrid[0].tile.Name);
			Assert.Equal(TileTransformation.None, tilesOnDuelGrid[0].transformation);
		}

		[Fact]
		public void GetTilesOnDuelGrid_TopLeftOuterCornerRule_Applies()
		{
			var brick = Materials.First(m => m.Name == "Brick");
			var map = new MaterialTilemap(4, 4);

			map[0, 0] = null;
			map[1, 0] = null;
			map[0, 1] = null;
			map[1, 1] = brick;

			var tilesOnDuelGrid = map.GetTilesOnDuelGrid(0, 0);

			Assert.Single(tilesOnDuelGrid);
			Assert.Equal("OuterCornerTileTL", tilesOnDuelGrid[0].tile.Name);
			Assert.Equal(TileTransformation.None, tilesOnDuelGrid[0].transformation);
		}

		[Fact]
		public void GetTilesOnDuelGrid_TopRightOuterCornerRule_Applies()
		{
			var brick = Materials.First(m => m.Name == "Brick");
			var map = new MaterialTilemap(4, 4);

			map[0, 0] = null;
			map[1, 0] = null;
			map[0, 1] = brick;
			map[1, 1] = null;

			var tilesOnDuelGrid = map.GetTilesOnDuelGrid(0, 0);

			Assert.Single(tilesOnDuelGrid);
			Assert.Equal("OuterCornerTileTL", tilesOnDuelGrid[0].tile.Name);
			Assert.Equal(TileTransformation.FlipHorizontal, tilesOnDuelGrid[0].transformation);
		}

		[Fact]
		public void GetTilesOnDuelGrid_BottomLeftOuterCornerRule_Applies()
		{
			var brick = Materials.First(m => m.Name == "Brick");
			var map = new MaterialTilemap(4, 4);

			map[0, 0] = null;
			map[1, 0] = brick;
			map[0, 1] = null;
			map[1, 1] = null;

			var tilesOnDuelGrid = map.GetTilesOnDuelGrid(0, 0);

			Assert.Single(tilesOnDuelGrid);
			Assert.Equal("OuterCornerTileTL", tilesOnDuelGrid[0].tile.Name);
			Assert.Equal(TileTransformation.FlipVertical, tilesOnDuelGrid[0].transformation);
		}

		[Fact]
		public void GetTilesOnDuelGrid_BottomRightOuterCornerRule_Applies()
		{
			var brick = Materials.First(m => m.Name == "Brick");
			var map = new MaterialTilemap(4, 4);

			map[0, 0] = brick;
			map[1, 0] = null;
			map[0, 1] = null;
			map[1, 1] = null;

			var tilesOnDuelGrid = map.GetTilesOnDuelGrid(0, 0);

			Assert.Single(tilesOnDuelGrid);
			Assert.Equal("OuterCornerTileTL", tilesOnDuelGrid[0].tile.Name);
			Assert.Equal(TileTransformation.FlipHorizontal | TileTransformation.FlipVertical, tilesOnDuelGrid[0].transformation);
		}

		[Fact]
		public void GetTilesOnDuelGrid_TopLeftInnerCornerRule_Applies()
		{
			var brick = Materials.First(m => m.Name == "Brick");
			var map = new MaterialTilemap(4, 4);

			map[0, 0] = brick;
			map[1, 0] = brick;
			map[0, 1] = brick;
			map[1, 1] = null;

			var tilesOnDuelGrid = map.GetTilesOnDuelGrid(0, 0);

			Assert.Single(tilesOnDuelGrid);
			Assert.Equal("InnerCornerTileBR", tilesOnDuelGrid[0].tile.Name);
			Assert.Equal(TileTransformation.FlipVertical | TileTransformation.FlipHorizontal, tilesOnDuelGrid[0].transformation);
		}

		[Fact]
		public void GetTilesOnDuelGrid_TopRightInnerCornerRule_Applies()
		{
			var brick = Materials.First(m => m.Name == "Brick");
			var map = new MaterialTilemap(4, 4);

			map[0, 0] = brick;
			map[1, 0] = brick;
			map[0, 1] = null;
			map[1, 1] = brick;

			var tilesOnDuelGrid = map.GetTilesOnDuelGrid(0, 0);

			Assert.Single(tilesOnDuelGrid);
			Assert.Equal("InnerCornerTileBR", tilesOnDuelGrid[0].tile.Name);
			Assert.Equal(TileTransformation.FlipVertical, tilesOnDuelGrid[0].transformation);
		}

		[Fact]
		public void GetTilesOnDuelGrid_BottomLeftInnerCornerRule_Applies()
		{
			var brick = Materials.First(m => m.Name == "Brick");
			var map = new MaterialTilemap(4, 4);

			map[0, 0] = brick;
			map[1, 0] = null;
			map[0, 1] = brick;
			map[1, 1] = brick;

			var tilesOnDuelGrid = map.GetTilesOnDuelGrid(0, 0);

			Assert.Single(tilesOnDuelGrid);
			Assert.Equal("InnerCornerTileBR", tilesOnDuelGrid[0].tile.Name);
			Assert.Equal(TileTransformation.FlipHorizontal, tilesOnDuelGrid[0].transformation);
		}

		[Fact]
		public void GetTilesOnDuelGrid_BottomRightInnerCornerRule_Applies()
		{
			var brick = Materials.First(m => m.Name == "Brick");
			var map = new MaterialTilemap(4, 4);

			map[0, 0] = null;
			map[1, 0] = brick;
			map[0, 1] = brick;
			map[1, 1] = brick;

			var tilesOnDuelGrid = map.GetTilesOnDuelGrid(0, 0);

			Assert.Single(tilesOnDuelGrid);
			Assert.Equal("InnerCornerTileBR", tilesOnDuelGrid[0].tile.Name);
			Assert.Equal(TileTransformation.None, tilesOnDuelGrid[0].transformation);
		}

		[Fact]
		public void GetTilesOnDuelGrid_DiagonalTLBRRule_Applies()
		{
			var brick = Materials.First(m => m.Name == "Brick");
			var map = new MaterialTilemap(4, 4);

			map[0, 0] = brick;
			map[1, 0] = null;
			map[0, 1] = null;
			map[1, 1] = brick;

			var tilesOnDuelGrid = map.GetTilesOnDuelGrid(0, 0);

			Assert.Single(tilesOnDuelGrid);
			Assert.Equal("DiagonalTileTLBR", tilesOnDuelGrid[0].tile.Name);
			Assert.Equal(TileTransformation.None, tilesOnDuelGrid[0].transformation);
		}

		[Fact]
		public void GetTilesOnDuelGrid_DiagonalTRBLRule_Applies()
		{
			var brick = Materials.First(m => m.Name == "Brick");
			var map = new MaterialTilemap(4, 4);

			map[0, 0] = null;
			map[1, 0] = brick;
			map[0, 1] = brick;
			map[1, 1] = null;

			var tilesOnDuelGrid = map.GetTilesOnDuelGrid(0, 0);

			Assert.Single(tilesOnDuelGrid);
			Assert.Equal("DiagonalTileTLBR", tilesOnDuelGrid[0].tile.Name);
			Assert.Equal(TileTransformation.FlipHorizontal, tilesOnDuelGrid[0].transformation);
		}
	}
}
