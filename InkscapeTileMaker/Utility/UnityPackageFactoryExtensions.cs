using InkscapeTileMaker.Models;
using InkscapeTileMaker.Services;
using UnityPackageNET;
using YamlDotNet.RepresentationModel;
using static UnityPackageNET.UnityPackageFactory;

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
			var imageEntry = MakeEmptyEntry($"{imageLocation}/{tileset.Name}.png");
			imageEntry.DataStream = await renderingService.RenderFileAsync(file, ".png");
			var root = imageEntry.Metadata!.Document.RootNode as YamlMappingNode;
			root!.Add("TextureImporter", WriteTextureImporter(tileset, out var uids));
			writer.WriteEntry(imageEntry);
			imageEntry.DataStream.Close();

			// create tile assets
			foreach (var tile in tileset)
			{
				WriteTileAsset(tile, uids, writer, tileLocation, imageEntry.GUID);
			}
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
				{ "sprites", spritesSequence }
			};

			textureImporter.Add("spriteSheet", spriteSheetNode);

			// platformSettings
			var platformSettingsSequence = new YamlSequenceNode();
			var defaultPlatformSettings = new YamlMappingNode
			{
				{ "buildTarget", new YamlScalarNode("DefaultTexturePlatform") },
				{ "maxTextureSize", new YamlScalarNode("2048") },
				{ "resizeAlgorithm", new YamlScalarNode("0") },
				{ "textureCompression", new YamlScalarNode("1") },
				{ "compressionQuality", new YamlScalarNode("50") },
				{ "crunchedCompression", new YamlScalarNode("0") },
				{ "allowsAlphaSplitting", new YamlScalarNode("0") },
				{ "overridden", new YamlScalarNode("0") }
			};
			platformSettingsSequence.Add(defaultPlatformSettings);

			textureImporter.Add("platformSettings", platformSettingsSequence);
			return textureImporter;
		}

		private static void WriteTileAsset(Tile tile, Dictionary<Tile, long> uids, UnityPackageWriter writer, string tileLocation, Guid imageGuid)
		{
			var tileEntry = MakeEmptyEntry($"{tileLocation}/{tile.Name}.asset");
			var root = new YamlMappingNode();
			var doc = new YamlDocument(root);

			// Build root mapping (MonoBehaviour)
			var monoBehaviourNode = new YamlMappingNode
			{
				{ "m_ObjectHideFlags", new YamlScalarNode("0") },
				{ "m_CorrespondingSourceObject", new YamlMappingNode { { "fileID", new YamlScalarNode("0") } } },
				{ "m_PrefabInstance", new YamlMappingNode { { "fileID", new YamlScalarNode("0") } } },
				{ "m_PrefabAsset", new YamlMappingNode { { "fileID", new YamlScalarNode("0") } } },
				{ "m_GameObject", new YamlMappingNode { { "fileID", new YamlScalarNode("0") } } },
				{ "m_Enabled", new YamlScalarNode("1") },
				{ "m_EditorHideFlags", new YamlScalarNode("0") },

				{
					"m_Script",
					new YamlMappingNode
					{
						{ "fileID", new YamlScalarNode("13312") },
						{ "guid", new YamlScalarNode("0000000000000000e000000000000000") },
						{ "type", new YamlScalarNode("0") }
					}
				},

				{ "m_Name", new YamlScalarNode(tile.Name) },
				{ "m_EditorClassIdentifier", new YamlScalarNode("UnityEngine.dll::UnityEngine.Tilemaps.Tile") },

				{
					"m_Sprite",
					new YamlMappingNode()
					{
						{ "fileID", new YamlScalarNode(uids[tile].ToString()) },
						{ "guid", new YamlScalarNode(imageGuid.ToString("N")) },
						{ "type", new YamlScalarNode("3") }
					}
				}
			};

			root.Add("MonoBehaviour", monoBehaviourNode);

			tileEntry.DataStream = new MemoryStream();
			YamlStream ys = new YamlStream(doc);
			using (var sw = new StreamWriter(tileEntry.DataStream, leaveOpen: true))
			{
				ys.Save(sw);
			}
			tileEntry.DataStream.Flush();
			tileEntry.DataStream.Position = 0;

			writer.WriteEntry(tileEntry);
		}
	}
}
