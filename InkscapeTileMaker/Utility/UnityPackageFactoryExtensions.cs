using InkscapeTileMaker.Models;
using InkscapeTileMaker.Services;
using UnityPackageNET;
using YamlDotNet.RepresentationModel;

namespace InkscapeTileMaker.Utility
{
	public static class UnityPackageFactoryExtensions
	{
		public static async Task WriteTileAssetsAsync(ITilesetConnection conn, ITilesetRenderingService renderingService, UnityPackageWriter writer, string imageLocation = "Assets/Sprites/Tiles", string tileLocation = "Assets/Tiles")
		{
			var tileset = conn.Tileset;
			var file = conn.CurrentFile;
			if (tileset == null || file == null) return;

			// create sliced image asset
			var imageEntry = UnityPackageEntryFactory.MakeEmptyEntry($"{imageLocation}/{tileset.Name}.png");
			imageEntry.DataStream = await renderingService.RenderFileAsync(file, ".png");
			var root = imageEntry.Metadata!.Root;
			root!.Add("TextureImporter", WriteTextureImporter(tileset, out var uids));
			writer.WriteEntry(imageEntry);
			imageEntry.DataStream.Close();

			// Originally I planned to create separate tile assets for each tile,
			// but Unity's Texture Importer doesn't have stable internal ids for the sprites
			// upon import, if anything looks slightly wrong, unity re-imports and changes the ids.

			// this isn't a huge deal as the sprites are still generated with the correct cutouts, but the tiles assets will need to be made seperately

			// create tile assets
			//foreach (var tile in tileset)
			//{
			//	WriteTileAsset(tile, uids, writer, tileLocation, imageEntry.GUID);
			//}
		}

		private static YamlMappingNode WriteTextureImporter(ITileset tileset, out Dictionary<Tile, long> uids)
		{
			// TextureImporter root
			var textureImporter = new YamlMappingNode
			{
				{ "serializedVersion", new YamlScalarNode("12") },
				{ "internalIDToNameTable", new YamlSequenceNode() },
				{ "externalObjects", new YamlMappingNode() },
				{ "textureType", new YamlScalarNode("8") },   // Sprite (2D and UI)
				{ "spriteMode", new YamlScalarNode("2") },    // Multiple
				{ "spritePixelsToUnits", new YamlScalarNode("100") },
				{ "alphaIsTransparency", new YamlScalarNode("1") },
				{ "textureShape", new YamlScalarNode("1") },
				{ "textureCompression", new YamlScalarNode("1") },
				{ "spriteGenerateFallbackPhysicsShape", new YamlScalarNode("0") }
			};

			var spritesSequence = new YamlSequenceNode();
			var nameFileIdTable = new YamlMappingNode();

			uids = new Dictionary<Tile, long>();
			foreach (var tile in tileset)
			{
				long uid = Random.Shared.NextInt64();
				uids.Add(tile, uid);
				nameFileIdTable.Add(tile.Name, uid.ToString());

				int x = tile.Column * tileset.TileSize.width;
				// Unity's texture space has (0,0) at bottom-left, while the tileset's row 0 is at top.
				// We flip the Y so that the top row in the tileset maps to the highest Y in the texture.
				int y = (tileset.Size.height - (tile.Row + 1) * tileset.TileSize.height);

				int width = tileset.TileSize.width;
				int height = tileset.TileSize.height;

				var spriteNode = new YamlMappingNode
				{
					{ "name", new YamlScalarNode(tile.Name) },
					{ "internalID", new YamlScalarNode(uid.ToString())},
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
				//{ "nameFileIDTable", nameFileIdTable  }
			};

			textureImporter.Add("spriteSheet", spriteSheetNode);

			// platformSettings
			//var platformSettingsSequence = new YamlSequenceNode();
			//var defaultPlatformSettings = new YamlMappingNode
			//{
			//	{ "buildTarget", new YamlScalarNode("DefaultTexturePlatform") },
			//	{ "maxTextureSize", new YamlScalarNode("2048") },
			//	{ "resizeAlgorithm", new YamlScalarNode("0") },
			//	{ "textureCompression", new YamlScalarNode("1") },
			//	{ "compressionQuality", new YamlScalarNode("50") },
			//	{ "crunchedCompression", new YamlScalarNode("0") },
			//	{ "allowsAlphaSplitting", new YamlScalarNode("0") },
			//	{ "overridden", new YamlScalarNode("0") }
			//};
			//platformSettingsSequence.Add(defaultPlatformSettings);
			//textureImporter.Add("platformSettings", platformSettingsSequence);

			return textureImporter;
		}

		//private static void WriteTileAsset(Tile tile, Dictionary<Tile, long> uids, UnityPackageWriter writer, string tileLocation, Guid imageGuid)
		//{
		//	var (asset, metadata, monoBehaviourNode) = 
		//		UnityAssetFactory.MakeScriptableObject($"{tileLocation}/{tile.Name}.asset", Guid.Parse("0000000000000000e000000000000000"));

		//	var scriptNode = (YamlMappingNode)monoBehaviourNode["m_Script"]; 
		//	scriptNode.Children["fileID"] =  new YamlScalarNode("13312");
		//	scriptNode.Children["type"] = "0";
		//	monoBehaviourNode.Children["m_EditorClassIdentifier"] = "UnityEngine.dll::UnityEngine.Tilemaps.Tile";
		//	monoBehaviourNode.Children.Add("m_Sprite", new YamlMappingNode()
		//	{
		//		{ "fileID", new YamlScalarNode(uids[tile].ToString()) },
		//		{ "guid", new YamlScalarNode(imageGuid.ToString("N")) },
		//		{ "type", new YamlScalarNode("3") }
		//	});

		//	var tileEntry = UnityPackageEntryFactory.Combine(asset, metadata);
		//	writer.WriteEntry(tileEntry);
		//}
	}
}
