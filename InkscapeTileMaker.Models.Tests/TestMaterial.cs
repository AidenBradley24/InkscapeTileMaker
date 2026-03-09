namespace InkscapeTileMaker.Models.Tests
{
	public class TestMaterial
	{
		readonly List<Tile> tiles =
		[
			new Tile { Name = "CoreTile", MaterialName = "Brick", Variant=TileVariant.Core, Type = TileType.DualTileMaterial, Row = 0, Column = 0, Alignment = TileAlignment.Core, Priority = 1 },
			new Tile { Name = "EdgeTile", MaterialName = "Brick", Variant=TileVariant.Edge, Type = TileType.DualTileMaterial, Row = 0, Column = 1, Alignment = TileAlignment.RightEdge, Priority = 1 },
			new Tile { Name = "EdgeTile2", MaterialName = "Brick", Variant=TileVariant.Edge, Type = TileType.DualTileMaterial, Row = 0, Column = 1, Alignment = TileAlignment.BottomEdge, Priority = 2 },
			new Tile { Name = "CornerTile", MaterialName = "Brick", Variant=TileVariant.OuterCorner, Type = TileType.DualTileMaterial, Row = 0, Column = 2, Alignment = TileAlignment.TopLeftOuterCorner, Priority = 1 },
			new Tile { Name = "SingularTile", MaterialName = "Wood", Variant=TileVariant.Core, Type = TileType.Singular, Row = 1, Column = 0, Priority = 1 },
			new Tile { Name = "NoMaterialTile", MaterialName = string.Empty, Variant=TileVariant.Core, Type = TileType.DualTileMaterial, Row = 2, Column = 0, Priority = 1 },
			new Tile { Name = "NullMaterialTile", MaterialName = null, Variant=TileVariant.Core, Type = TileType.DualTileMaterial, Row = 2, Column = 1, Priority = 1 },
			new Tile { Name = "StoneCore", MaterialName = "Stone", Variant=TileVariant.Core, Type = TileType.DualTileMaterial, Row = 3, Column = 0, Priority = 1 },
			new Tile { Name = "StoneEdge", MaterialName = "stone", Variant=TileVariant.Edge, Type = TileType.DualTileMaterial, Row = 3, Column = 1, Priority = 1 },
		];

		[Fact]
		public void GetAllMaterials_ReturnsDistinctMaterialsByName()
		{
			// Brick (many tiles), Wood (one), Stone/stone (case variants) => 3 materials
			var materials = Material.GetAllMaterials(() => tiles);

			Assert.Equal(4, materials.Count);

			Assert.Contains(materials, m => m.Name == "Brick");
			Assert.Contains(materials, m => m.Name == "Wood");
			Assert.Contains(materials, m => m.Name == "Stone");
			Assert.Contains(materials, m => m.Name == "stone");
		}

		[Fact]
		public void GetAllMaterials_IgnoresTilesWithNullOrEmptyMaterialName()
		{
			var materials = Material.GetAllMaterials(() => tiles);
			Assert.DoesNotContain(materials, m => m.Name == string.Empty);
			Assert.DoesNotContain(materials, m => m.Name == null);
		}

		[Fact]
		public void GetAllMaterials_MaterialInstancesUseProvidedTilesProvider()
		{
			var materials = Material.GetAllMaterials(() => tiles);
			var brickMaterial = materials.Single(m => m.Name == "Brick");
			Assert.True(brickMaterial.HasTileVariant(TileVariant.Core));
		}

		[Fact]
		public void GetTile_ReturnsNull_WhenNoTileOfTypeExists()
		{
			var brickMaterial = new Material("Brick", () => tiles);
			var result = brickMaterial.GetTile(TileVariant.Diagonal, TileAlignment.DiagonalTopLeftToBottomRight);
			Assert.Null(result);
		}

		[Fact]
		public void GetTile_ReturnsPreferredAlignment_WhenAvailable()
		{
			var brickMaterial = new Material("Brick", () => tiles);
			var result = brickMaterial.GetTile(TileVariant.Edge, TileAlignment.BottomEdge);
			Assert.NotNull(result);
			Assert.Equal("EdgeTile2", result!.Name);
			Assert.Equal(TileAlignment.BottomEdge, result.Alignment);
		}

		[Fact]
		public void GetTile_UsesHighestPriority_WithinPreferredAlignment()
		{
			var priorityTiles = new List<Tile>
			{
				new Tile { Name = "LowPriority", MaterialName = "PriorityMat", Variant=TileVariant.Edge, Type = TileType.DualTileMaterial, Alignment = TileAlignment.RightEdge, Priority = 1 },
				new Tile { Name = "HighPriority", MaterialName = "PriorityMat", Variant=TileVariant.Edge, Type = TileType.DualTileMaterial, Alignment = TileAlignment.RightEdge, Priority = 10 },
				new Tile { Name = "OtherAlignment", MaterialName = "PriorityMat", Variant=TileVariant.Edge, Type = TileType.DualTileMaterial, Alignment = TileAlignment.LeftEdge, Priority = 100 },
			};

			var material = new Material("PriorityMat", () => priorityTiles);
			var result = material.GetTile(TileVariant.Edge, TileAlignment.RightEdge);
			Assert.NotNull(result);
			Assert.Equal("HighPriority", result!.Name);
		}

		[Fact]
		public void GetTile_UsesHighestPriority_WhenFallingBackToAnyAlignment()
		{
			var priorityTiles = new List<Tile>
			{
				new Tile
				{
					Name = "LowPriority",
					MaterialName = "PriorityMat2",
					Variant=TileVariant.Edge,
					Type = TileType.DualTileMaterial,
					Alignment = TileAlignment.TopEdge,
					Priority = 1,
					SecondaryAlignments = [TileAlignment.RightEdge]
				},
				new Tile
				{
					Name = "HighPriority",
					MaterialName = "PriorityMat2",
					Variant=TileVariant.Edge,
					Type = TileType.DualTileMaterial,
					Alignment = TileAlignment.LeftEdge,
					Priority = 10,
					SecondaryAlignments = [TileAlignment.RightEdge]
				},
			};

			var material = new Material("PriorityMat2", () => priorityTiles);
			var result = material.GetTile(TileVariant.Edge, TileAlignment.RightEdge);
			Assert.NotNull(result);
			Assert.Equal("HighPriority", result!.Name);
		}

		[Fact]
		public void GetTile_UsesLowerPriority_NotPartOfSecondaryAlignments()
		{
			var priorityTiles = new List<Tile>
			{
				new Tile
				{
					Name = "LowPriority",
					MaterialName = "PriorityMat2",
					Variant=TileVariant.Edge,
					Type = TileType.DualTileMaterial,
					Alignment = TileAlignment.TopEdge,
					Priority = 1,
					SecondaryAlignments = [TileAlignment.RightEdge]
				},
				new Tile
				{
					Name = "HighPriority",
					MaterialName = "PriorityMat2",
					Variant=TileVariant.Edge,
					Type = TileType.DualTileMaterial,
					Alignment = TileAlignment.LeftEdge,
					Priority = 10,
					SecondaryAlignments = []
				},
			};

			var material = new Material("PriorityMat2", () => priorityTiles);
			var result = material.GetTile(TileVariant.Edge, TileAlignment.RightEdge);
			Assert.NotNull(result);
			Assert.Equal("LowPriority", result!.Name);
		}
	}
}
