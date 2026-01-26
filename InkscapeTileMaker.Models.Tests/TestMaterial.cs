namespace InkscapeTileMaker.Models.Tests
{
	public class TestMaterial
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
			new Tile { Name = "StoneEdge", MaterialName = "stone", Type = TileType.MatEdge, Row = 3, Column = 1, Priority = 1 },
		];

		[Fact]
		public void ExcludesSingularTile()
		{
			var brickMaterial = new Material("Brick", () => tiles);
			Assert.False(brickMaterial.HasTileType(TileType.Singular));
		}

		[Fact]
		public void GetAllMaterials_ReturnsDistinctMaterialsByName()
		{
			// Brick (many tiles), Wood (one), Stone/stone (case variants) => 3 materials
			var materials = Material.GetAllMaterials(() => tiles);

			Assert.Equal(3, materials.Count);

			Assert.Contains(materials, m => m.Name == "Brick");
			Assert.Contains(materials, m => m.Name == "Wood");
			Assert.Contains(materials, m => m.Name == "Stone");
		}

		[Fact]
		public void GetAllMaterials_IgnoresTilesWithNullOrEmptyMaterialName()
		{
			var materials = Material.GetAllMaterials(() => tiles);

			// Ensure tiles with empty or null MaterialName did not produce a material
			Assert.DoesNotContain(materials, m => m.Name == string.Empty);
			Assert.DoesNotContain(materials, m => m.Name == null);
		}

		[Fact]
		public void GetAllMaterials_IsCaseInsensitiveOnMaterialKey()
		{
			// Stone and stone should produce only one Material
			var materials = Material.GetAllMaterials(() => tiles);

			var stoneMaterials = materials.Where(m => m.Name.Equals("Stone", StringComparison.OrdinalIgnoreCase));
			Assert.Single(stoneMaterials);
		}

		[Fact]
		public void GetAllMaterials_MaterialInstancesUseProvidedTilesProvider()
		{
			var materials = Material.GetAllMaterials(() => tiles);

			var brickMaterial = materials.Single(m => m.Name == "Brick");

			// If the tilesProvider was wired correctly into the Material instance,
			// it should see the same tiles and therefore have the MatCore tile type.
			Assert.True(brickMaterial.HasTileType(TileType.MatCore));
		}

		[Fact]
		public void GetTile_ReturnsNull_WhenNoTileOfTypeExists()
		{
			// Brick material has no Singular tiles
			var brickMaterial = new Material("Brick", () => tiles);

			var result = brickMaterial.GetTile(TileType.Singular, TileAlignment.Core);

			Assert.Null(result);
		}

		[Fact]
		public void GetTile_ReturnsPreferredAlignment_WhenAvailable()
		{
			var brickMaterial = new Material("Brick", () => tiles);

			// There is a MatEdge tile with BottomEdge alignment (EdgeTile2)
			var result = brickMaterial.GetTile(TileType.MatEdge, TileAlignment.BottomEdge);

			Assert.NotNull(result);
			Assert.Equal("EdgeTile2", result!.Name);
			Assert.Equal(TileAlignment.BottomEdge, result.Allignment);
		}

		[Fact]
		public void GetTile_FallsBackToAnyAlignment_WhenPreferredNotAvailable()
		{
			var brickMaterial = new Material("Brick", () => tiles);

			// There is no MatCore tile with RightEdge alignment, so it should fall back
			// to any MatCore tile, which is CoreTile
			var result = brickMaterial.GetTile(TileType.MatCore, TileAlignment.RightEdge);

			Assert.NotNull(result);
			Assert.Equal("CoreTile", result!.Name);
			Assert.Equal(TileType.MatCore, result.Type);
		}

		[Fact]
		public void GetTile_UsesHighestPriority_WithinPreferredAlignment()
		{
			// Arrange a material with multiple tiles of same type and alignment but different priorities
			var priorityTiles = new List<Tile>
			{
				new Tile { Name = "LowPriority", MaterialName = "PriorityMat", Type = TileType.MatEdge, Allignment = TileAlignment.RightEdge, Priority = 1 },
				new Tile { Name = "HighPriority", MaterialName = "PriorityMat", Type = TileType.MatEdge, Allignment = TileAlignment.RightEdge, Priority = 10 },
				new Tile { Name = "OtherAlignment", MaterialName = "PriorityMat", Type = TileType.MatEdge, Allignment = TileAlignment.LeftEdge, Priority = 100 },
			};

			var material = new Material("PriorityMat", () => priorityTiles);

			var result = material.GetTile(TileType.MatEdge, TileAlignment.RightEdge);

			Assert.NotNull(result);
			Assert.Equal("HighPriority", result!.Name);
		}

		[Fact]
		public void GetTile_UsesHighestPriority_WhenFallingBackToAnyAlignment()
		{
			// Arrange a material with multiple tiles of same type but no tile with preferred alignment
			var priorityTiles = new List<Tile>
			{
				new Tile { Name = "LowPriority", MaterialName = "PriorityMat2", Type = TileType.MatCore, Allignment = TileAlignment.Core, Priority = 1 },
				new Tile { Name = "HighPriority", MaterialName = "PriorityMat2", Type = TileType.MatCore, Allignment = TileAlignment.LeftEdge, Priority = 10 },
			};

			var material = new Material("PriorityMat2", () => priorityTiles);

			// Preferred alignment does not exist, should pick highest priority of any alignment
			var result = material.GetTile(TileType.MatCore, TileAlignment.RightEdge);

			Assert.NotNull(result);
			Assert.Equal("HighPriority", result!.Name);
		}
	}
}
