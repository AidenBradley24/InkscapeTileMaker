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
		private readonly ITilesetConnection _tilesetConnection;
		private readonly ITilesetRenderingService _tilesetRenderingService;

		private static readonly Guid TileMakerImporterScriptGuid = Guid.Parse("a85efa26d4f565242a96b2e7fce398ca");
		private static readonly JsonSerializerOptions _jsonOptions = new()
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			DefaultIgnoreCondition = JsonIgnoreCondition.Never,
			IncludeFields = true,
			WriteIndented = true
		};

		public UnityPackageExporter(ISettingsService settingsService, ITilesetConnection tilesetConnection, ITilesetRenderingService renderingService)
		{
			_settingsService = settingsService;
			_tilesetConnection = tilesetConnection;
			_tilesetRenderingService = renderingService;
		}

		public async Task WriteTilesetPackageAsync(UnityPackageWriter writer)
		{
			var tileset = _tilesetConnection.Tileset;
			var file = _tilesetConnection.CurrentFile;
			if (tileset == null || _tilesetConnection.Tileset == null || file == null) return;

			if (!_settingsService.UnityExportTiles)
			{
				// only export sliced image
				using var png = await _tilesetRenderingService.RenderFileAsync(file, ".png");
				await WriteSlicedImage(writer, png, tileset);
				return;
			}

			// export each material seperately
			await WriteScriptsAsync(writer);

			var materials = Material.GetAllMaterials(() => tileset);
			if (materials.Count == 0)
			{
				using var png = await _tilesetRenderingService.RenderFileAsync(file, ".png");
				var guid = await WriteSlicedImage(writer, png, tileset);
				WriteTilesetAsset(writer, tileset, guid);
				return;
			}

			foreach (var material in materials)
			{
				switch (material.Type)
				{
					case TileType.DualTileMaterial:
						await WriteDualTileMaterial(writer, material);
						break;
				}
			}

			var remaining = tileset.Where(t => !string.IsNullOrWhiteSpace(t.MaterialName)).Distinct().ToList();
			if (remaining.Count == 0) return;
			await WritePlainTiles(writer, remaining);
		}

		private async Task<Guid> WriteSlicedImage(UnityPackageWriter writer, Stream pngStream, ITileset tileset)
		{
			string imageLocation = _settingsService.UnityImageExportPath;
			var imageEntry = UnityPackageEntryFactory.MakeEmptyEntry($"{imageLocation}/{tileset.Name}.png");
			imageEntry.DataStream = pngStream;
			WriteTextureImporter(imageEntry.Metadata!, tileset);
			writer.WriteEntry(imageEntry);
			await imageEntry.DataStream.DisposeAsync();
			return imageEntry.GUID;
		}

		private async Task WritePlainTiles(UnityPackageWriter writer, List<Tile> tiles)
		{
			var exporter = new PlainTilesetExporter(_tilesetConnection, _tilesetRenderingService);
			exporter.TilesetSize = new Models.Rect(tiles.Count).Scale;
			var tmpFile = new FileInfo(Path.GetTempFileName());
			var tileData = tiles.Select(t => new TileData?(new TileData() { Tile = t, Transformation = TileTransformation.None }));
			var exportTiles = await exporter.ExportAsync(tmpFile, [.. tileData], _tilesetConnection.Tileset!.TilePixelSize);
			var tilesetData = new TilesetData(_tilesetConnection.Tileset!.Name, _tilesetConnection.Tileset.TilePixelSize, exporter.TilesetSize * _tilesetConnection.Tileset.TilePixelSize, exportTiles!);
			using (var png = tmpFile.OpenRead())
			{
				var guid = await WriteSlicedImage(writer, png, tilesetData);
				WriteTilesetAsset(writer, tilesetData, guid);
			}
			tmpFile.Delete();
		}

		private async Task WriteDualTileMaterial(UnityPackageWriter writer, Material material)
		{
			var exporter = new DualTileExporter(material.Name, _tilesetConnection, _tilesetRenderingService);
			var tmpFile = new FileInfo(Path.GetTempFileName());
			var tiles = await exporter.ExportAsync(tmpFile, _tilesetConnection.Tileset!.TilePixelSize);
			var tilesetData = new TilesetData(material.Name, _tilesetConnection.Tileset!.TilePixelSize, exporter.TilesetSize * _tilesetConnection.Tileset.TilePixelSize, tiles!);
			using (var png = tmpFile.OpenRead())
			{
				var imageGuid = await WriteSlicedImage(writer, png, tilesetData);
				WriteTilesetAsset(writer, tilesetData, imageGuid);
			}

			tmpFile.Delete();
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

			var textureSettings = new YamlMappingNode
			{
				{ "serializedVersion", new YamlScalarNode("2") },
				{ "filterMode", new YamlScalarNode("0") },
				{ "aniso", new YamlScalarNode("1") },
				{ "mipBias", new YamlScalarNode("0") },
				{ "wrapU", new YamlScalarNode("1") },
				{ "wrapV", new YamlScalarNode("1") },
				{ "wrapW", new YamlScalarNode("1") }
			};
			importer.Root.Add("textureSettings", textureSettings);
		}

		private async Task WriteScriptsAsync(UnityPackageWriter writer)
		{
			string editorScriptLocation = _settingsService.UnityEditorScriptPath;
			using var fs = await FileSystem.Current.OpenAppPackageFileAsync($"Unity/TileMakerImporter.cs");
			var entry = UnityPackageEntryFactory.MakeEmptyEntry($"{editorScriptLocation}/TileMakerImporter.cs", TileMakerImporterScriptGuid);
			entry.DataStream = fs;
			writer.WriteEntry(entry);
		}

		private void WriteTilesetAsset(UnityPackageWriter writer, ITileset tileset, Guid imageGuid)
		{
			var entry = UnityPackageEntryFactory.MakeEmptyEntry($"{_settingsService.UnityImageExportPath}/{tileset.Name}.tmsx");
			entry.DataStream = new MemoryStream();
			using (var sw = new StreamWriter(entry.DataStream, leaveOpen: true))
			{
				var settings = new TilesetImporterSettings
				{
					ImageGuid = imageGuid.ToString("N"),
					Tiles = [.. tileset.Select(t => new TileRecord
					{
						Row = t.Row,
						Col = t.Column,
						Type = t.Type,
						Variant = t.Variant,
						Alignment = t.Alignment,
						MaterialName = t.MaterialName
					})],
					TileWidth = tileset.TilePixelSize.Width,
					TileHeight = tileset.TilePixelSize.Height
				};

				var json = JsonSerializer.Serialize(settings, _jsonOptions);
				sw.Write(json);
			}
			entry.DataStream.Seek(0, SeekOrigin.Begin);
			writer.WriteEntry(entry);
			entry.DataStream.Close();
		}

		[System.Serializable]
		public class TilesetImporterSettings
		{
			public string Name { get; set; } = string.Empty;
			public string ImageGuid { get; set; } = string.Empty;
			public TileRecord[] Tiles { get; set; } = Array.Empty<TileRecord>();
			public int TileWidth { get; set; }
			public int TileHeight { get; set; }
		}

		[System.Serializable]
		public class TileRecord
		{
			public int Row { get; set; }
			public int Col { get; set; }
			public TileType Type { get; set; }
			public TileVariant Variant { get; set; }
			public TileAlignment Alignment { get; set; }
			public string MaterialName { get; set; } = string.Empty;
		}
	}
}
