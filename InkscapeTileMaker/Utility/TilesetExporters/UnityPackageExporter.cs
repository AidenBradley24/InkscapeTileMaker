using InkscapeTileMaker.Models;
using InkscapeTileMaker.Services;
using System.Text.Json;
using System.Text.Json.Serialization;
using UnityPackageNET;
using UnityPackageNET.Metadata;
using YamlDotNet.RepresentationModel;

namespace InkscapeTileMaker.Utility.TilesetExporters
{
	public class UnityPackageExporter
	{
		private readonly ISettingsService _settingsService;

		private readonly Guid tileMakerImporterScriptGuid = Guid.Parse("a85efa26d4f565242a96b2e7fce398ca");
		private readonly Guid materialTileScriptGuid = Guid.Parse("06d20ff3289910e4a8fbb03e6ad3d0bf");

		public UnityPackageExporter(ISettingsService settingsService)
		{
			_settingsService = settingsService;
		}

		public async Task WriteTilesetPackageAsync(UnityPackageWriter writer, ITilesetConnection conn, ITilesetRenderingService renderingService)
		{
			var tileset = conn.Tileset;
			var file = conn.CurrentFile;
			if (tileset == null || conn.Tileset == null || file == null) return;

			// create sliced image asset
			string imageLocation = _settingsService.UnityImageExportPath;
			var imageEntry = UnityPackageEntryFactory.MakeEmptyEntry($"{imageLocation}/{tileset.Name}.png");
			imageEntry.DataStream = await renderingService.RenderFileAsync(file, ".png");
			WriteTextureImporter(imageEntry.Metadata!, tileset);
			writer.WriteEntry(imageEntry);
			imageEntry.DataStream.Close();

			// create tileset asset
			if (!_settingsService.UnityExportTiles) return;
			await WriteScriptsAsync(writer, conn.Tileset.Any(t => !string.IsNullOrWhiteSpace(t.MaterialName)));
			WriteTilesetAsset(writer, tileset, imageEntry.GUID);
		}

		private void WriteTextureImporter(UnityAssetMetadata metadata, ITileset tileset)
		{
			var importer = new AssetImporter(metadata, "TextureImporter");
			importer.Root.Add("serializedVersion", new YamlScalarNode("12"));
			importer.Root.Add("internalIDToNameTable", new YamlSequenceNode());
			importer.Root.Add("externalObjects", new YamlMappingNode());
			importer.Root.Add("textureType", new YamlScalarNode("8"));   // Sprite (2D and UI)
			importer.Root.Add("spriteMode", new YamlScalarNode("2"));    // Multiple
			importer.Root.Add("spritePixelsToUnits", new YamlScalarNode("100"));
			importer.Root.Add("alphaIsTransparency", new YamlScalarNode("1"));
			importer.Root.Add("textureShape", new YamlScalarNode("1"));
			importer.Root.Add("textureCompression", new YamlScalarNode("1"));
			importer.Root.Add("spriteGenerateFallbackPhysicsShape", new YamlScalarNode("0"));

			var spritesSequence = new YamlSequenceNode();

			foreach (var tile in tileset)
			{
				int x = tile.Column * tileset.TilePixelSize.Width;
				// Unity's texture space has (0,0) at bottom-left, while the tileset's row 0 is at top.
				// We flip the Y so that the top row in the tileset maps to the highest Y in the texture.
				int y = (tileset.ImagePixelSize.Height - (tile.Row + 1) * tileset.TilePixelSize.Height);

				int width = tileset.TilePixelSize.Width;
				int height = tileset.TilePixelSize.Height;

				var spriteNode = new YamlMappingNode
				{
					{ "name", new YamlScalarNode(tile.Name) },
					{
						"rect",
						new YamlMappingNode
						{
							{ "serializedVersion", new YamlScalarNode("2") },
							{ "x", new YamlScalarNode(x.ToString()) },
							{ "y", new YamlScalarNode(y.ToString()) },
							{ "width", new YamlScalarNode(width.ToString()) },
							{ "height", new YamlScalarNode(height.ToString()) }
						}
					},
					{ "alignment", new YamlScalarNode("0") },
					{
						"pivot",
						new YamlMappingNode
						{
							{ "x", new YamlScalarNode("0.5") },
							{ "y", new YamlScalarNode("0.5") }
						}
					},
					{
						"border",
						new YamlMappingNode
						{
							{ "x", new YamlScalarNode("0") },
							{ "y", new YamlScalarNode("0") },
							{ "z", new YamlScalarNode("0") },
							{ "w", new YamlScalarNode("0") }
						}
					}
				};

				spritesSequence.Add(spriteNode);
			}

			var spriteSheetNode = new YamlMappingNode
			{
				{ "sprites", spritesSequence },
			};

			importer.Root.Add("spriteSheet", spriteSheetNode);
		}

		private async Task WriteScriptsAsync(UnityPackageWriter writer, bool writeMaterialTile)
		{
			return; // TODO finish unity scripts
			string editorScriptLocation = _settingsService.UnityEditorScriptPath;
			using (var fs = await FileSystem.Current.OpenAppPackageFileAsync($"Unity/TileMakerImporter.cs"))
			{
				var entry = UnityPackageEntryFactory.MakeEmptyEntry($"{editorScriptLocation}/TileMakerImporter.cs", tileMakerImporterScriptGuid);
				entry.DataStream = fs;
				writer.WriteEntry(entry);
			}

			if (writeMaterialTile)
			{
				using var fs = await FileSystem.Current.OpenAppPackageFileAsync($"Unity/MaterialTile.cs");
				var entry = UnityPackageEntryFactory.MakeEmptyEntry($"{editorScriptLocation}/MaterialTile.cs", materialTileScriptGuid);
				entry.DataStream = fs;
				writer.WriteEntry(entry);
			}
		}

		private void WriteTilesetAsset(UnityPackageWriter writer, ITileset tileset, Guid imageEntryGuid)
		{
			var entry = UnityPackageEntryFactory.MakeEmptyEntry($"{_settingsService.UnityImageExportPath}/{tileset.Name}.tmsx");
			entry.DataStream = new MemoryStream();
			using (var sw = new StreamWriter(entry.DataStream, leaveOpen: true))
			{
				var settings = new TileImporterSettings
				{
					imageGuid = imageEntryGuid.ToString("N"),
					tiles = [.. tileset.Select(t => new TileRecord
					{
						row = t.Row,
						col = t.Column,
						type = t.Type,
						materialName = t.MaterialName
					})],
					tileWidth = tileset.TilePixelSize.Width,
					tileHeight = tileset.TilePixelSize.Height
				};

				var jsonOptions = new JsonSerializerOptions
				{
					PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
					DefaultIgnoreCondition = JsonIgnoreCondition.Never,
					IncludeFields = true,
					WriteIndented = true
				};

				var json = JsonSerializer.Serialize(settings, jsonOptions);
				sw.Write(json);
			}
			entry.DataStream.Seek(0, SeekOrigin.Begin);
			writer.WriteEntry(entry);
			entry.DataStream.Close();
		}

		[System.Serializable]
		public class TileImporterSettings
		{
			public string imageGuid { get; set; } = string.Empty;
			public TileRecord[] tiles { get; set; } = Array.Empty<TileRecord>();
			public int tileWidth { get; set; }
			public int tileHeight { get; set; }
		}

		[System.Serializable]
		public class TileRecord
		{
			public int row { get; set; }
			public int col { get; set; }
			public TileType type { get; set; }
			public string materialName { get; set; } = string.Empty;
		}
	}
}
