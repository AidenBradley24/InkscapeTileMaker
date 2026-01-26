namespace InkscapeTileMaker.Models.Tests
{
	public class TestMaterialTilemap
	{
		readonly List<Tile> tiles =
		[
			new Tile { Name = "CoreTile", MaterialName = "Brick", Type = TileType.MatCore, Row = 0, Column = 0, Allignment = TileAlignment.Core, Priority = 1 },
			new Tile { Name = "EdgeTile", MaterialName = "Brick", Type = TileType.MatEdge, Row = 0, Column = 1, Allignment = TileAlignment.RightEdge, Priority = 1 },
			new Tile { Name = "EdgeTile2", MaterialName = "Brick", Type = TileType.MatEdge, Row = 0, Column = 1, Allignment = TileAlignment.BottomEdge, Priority = 2 },
			new Tile { Name = "CornerTile", MaterialName = "Brick", Type = TileType.MatOuterCorner, Row = 0, Column = 2, Allignment = TileAlignment.TopLeftOuterCorner, Priority = 1 },
			new Tile { Name = "SingularTile", MaterialName = "Wood", Type = TileType.Singular, Row = 1, Column = 0, Priority = 1 },
			new Tile { Name = "NoMaterialTile", MaterialName = string.Empty, Type = TileType.MatCore, Row = 2, Column = 0, Priority = 1 },
			new Tile { Name = "NullMaterialTile", MaterialName = null, Type = TileType.MatCore, Row = 2, Column = 1, Priority = 1 },
			new Tile { Name = "StoneCore", MaterialName = "Stone", Type = TileType.MatCore, Row = 3, Column = 0, Priority = 1 },
			new Tile { Name = "StoneEdge", MaterialName = "stone", Type = TileType.MatEdge, Row = 3, Column = 1, Allignment = TileAlignment.TopEdge, Priority = 1 },
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
		public void GetTilesOnDuelGrid_AllFourCornersSameMaterial_CoreTileReturned()
		{
			// Arrange
			var brick = Materials.First(m => m.Name == "Brick");
			var map = new MaterialTilemap(4, 4);

			// Fill a 2x2 block of Brick so that:
			// topRight = (2,2), topLeft = (1,2), bottomRight = (2,1), bottomLeft = (1,1)
			map[1, 1] = brick;
			map[2, 1] = brick;
			map[1, 2] = brick;
			map[2, 2] = brick;

			// Act
			var tilesOnDuelGrid = map.GetTilesOnDuelGrid(2, 2);

			// Assert
			Assert.Single(tilesOnDuelGrid);
			Assert.Equal("CoreTile", tilesOnDuelGrid[0].tile.Name);
		}

		[Fact]
		public void GetTilesOnDuelGrid_TopEdgeRule_Applies()
		{
			// Arrange
			var brick = Materials.First(m => m.Name == "Brick");
			var map = new MaterialTilemap(4, 4);

			// Top row of 2x2 area is Brick, bottom row is null
			// topLeft = (1,2), topRight = (2,2)
			map[1, 2] = brick;
			map[2, 2] = brick;

			// Act
			var tilesOnDuelGrid = map.GetTilesOnDuelGrid(2, 2);

			// Assert
			Assert.Single(tilesOnDuelGrid);
			// Edge tile for Brick we configured has RightEdge alignment;
			// we only verify the correct material/tile name is chosen.
			Assert.Equal("EdgeTile2", tilesOnDuelGrid[0].tile.Name);
		}

		[Fact]
		public void GetTilesOnDuelGrid_BottomEdgeRule_Applies()
		{
			// Arrange
			var brick = Materials.First(m => m.Name == "Brick");
			var map = new MaterialTilemap(4, 4);

			// Bottom row of 2x2 area is Brick, top row is null
			// bottomLeft = (1,1), bottomRight = (2,1)
			map[1, 1] = brick;
			map[2, 1] = brick;

			// Act
			var tilesOnDuelGrid = map.GetTilesOnDuelGrid(2, 2);

			// Assert
			Assert.Single(tilesOnDuelGrid);
			Assert.Equal("EdgeTile2", tilesOnDuelGrid[0].tile.Name);
		}

		[Fact]
		public void GetTilesOnDuelGrid_LeftEdgeRule_Applies()
		{
			// Arrange
			var brick = Materials.First(m => m.Name == "Brick");
			var map = new MaterialTilemap(4, 4);

			// Left column of 2x2 area is Brick
			// topLeft = (1,2), bottomLeft = (1,1)
			map[1, 1] = brick;
			map[1, 2] = brick;

			// Act
			var tilesOnDuelGrid = map.GetTilesOnDuelGrid(2, 2);

			// Assert
			Assert.Single(tilesOnDuelGrid);
			// We only know it's an edge tile of Brick; check material name
			Assert.Equal("Brick", tilesOnDuelGrid[0].tile.MaterialName);
			Assert.Equal(TileType.MatEdge, tilesOnDuelGrid[0].tile.Type);
		}

		[Fact]
		public void GetTilesOnDuelGrid_RightEdgeRule_Applies()
		{
			// Arrange
			var brick = Materials.First(m => m.Name == "Brick");
			var map = new MaterialTilemap(4, 4);

			// Right column of 2x2 area is Brick
			// topRight = (2,2), bottomRight = (2,1)
			map[2, 1] = brick;
			map[2, 2] = brick;

			// Act
			var tilesOnDuelGrid = map.GetTilesOnDuelGrid(2, 2);

			// Assert
			Assert.Single(tilesOnDuelGrid);
			Assert.Equal("EdgeTile", tilesOnDuelGrid[0].tile.Name);
		}

		[Fact]
		public void GetTilesOnDuelGrid_SingleCornerRule_Applies_TopLeft()
		{
			// Arrange
			var brick = Materials.First(m => m.Name == "Brick");
			var map = new MaterialTilemap(4, 4);

			// Only top-left corner of 2x2 area is Brick
			// topLeft = (1,2)
			map[1, 2] = brick;

			// Act
			var tilesOnDuelGrid = map.GetTilesOnDuelGrid(2, 2);

			// Assert
			Assert.Single(tilesOnDuelGrid);
			Assert.Equal("CornerTile", tilesOnDuelGrid[0].tile.Name);
			Assert.Equal(TileType.MatOuterCorner, tilesOnDuelGrid[0].tile.Type);
		}

		[Fact]
		public void GetTilesOnDuelGrid_NoMatchingMaterialTiles_ReturnsEmpty()
		{
			// Arrange
			var wood = Materials.First(m => m.Name == "Wood");
			var map = new MaterialTilemap(4, 4);

			// Place some material that has only Singular tiles configured
			map[1, 1] = wood;
			map[2, 2] = wood;

			// Act
			var tilesOnDuelGrid = map.GetTilesOnDuelGrid(2, 2);

			// Assert
			Assert.Empty(tilesOnDuelGrid);
		}
	}
}
