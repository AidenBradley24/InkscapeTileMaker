using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.IO;
using System.Collections.Generic;

namespace TileMaker
{
	[ScriptedImporter(1, "tmsx")]
	public class TileMakerImporter : ScriptedImporter
	{
		[System.Serializable]
		public class TileImporterSettings
		{
			public string imageGuid;
			public TileRecord[] tiles;
			public int tileWidth;
			public int tileHeight;
		}

		[System.Serializable]
		public class TileRecord
		{
			public int row;
			public int col;
			public TileType type;
			public string materialName;

			public enum TileType
			{
				Singular,
				MatOuterCorner,
				MatInnerCorner,
				MatEdge,
				MatCore,
				MatDiagonal
			}
		}

		private static Sprite[] GetSpritesFromTextureGuid(GUID textureGuid)
		{
			var assetPath = AssetDatabase.GUIDToAssetPath(textureGuid);
			if (string.IsNullOrEmpty(assetPath))
			{
				throw new System.Exception("Unable to resolve asset path from GUID.");
			}

			var sprites = AssetDatabase.LoadAllAssetRepresentationsAtPath(assetPath);
			var spriteList = new System.Collections.Generic.List<Sprite>();

			for (var i = 0; i < sprites.Length; i++)
			{
				var sprite = sprites[i] as Sprite;
				if (sprite != null)
				{
					spriteList.Add(sprite);
				}
			}

			return spriteList.ToArray();
		}

		public override void OnImportAsset(AssetImportContext ctx)
		{
			var importer = JsonUtility.FromJson<TileImporterSettings>(File.ReadAllText(ctx.assetPath));
			if (!GUID.TryParse(importer.imageGuid, out var imageGuid))
			{
				throw new System.Exception($"unable to get guid of image! \"{importer.imageGuid}\"");
			}

			ctx.DependsOnSourceAsset(imageGuid);
			var sprites = GetSpritesFromTextureGuid(imageGuid);

			var spriteLookup = new Dictionary<(int row, int col), Sprite>();
			foreach (var sprite in sprites)
			{
				if (sprite == null) continue;

				Rect rect = sprite.rect;

				// Unity's rect.y is measured from the bottom of the texture.
				// Convert to a top-left-origin row using the texture height.
				int textureHeight = sprite.texture.height;

				int col = Mathf.FloorToInt(rect.x / importer.tileWidth);
				int rowFromBottom = Mathf.FloorToInt(rect.y / importer.tileHeight);
				int rowFromTop = (textureHeight / importer.tileHeight) - 1 - rowFromBottom;

				var key = (rowFromTop, col);
				if (!spriteLookup.ContainsKey(key))
				{
					spriteLookup.Add(key, sprite);
				}
			}

			foreach (var record in importer.tiles)
			{
				if (!spriteLookup.TryGetValue((record.row, record.col), out var sprite))
				{
					Debug.LogWarning($"No sprite found for r:{record.row},c:{record.col}");
					continue;
				}

				var tile = ScriptableObject.CreateInstance<Tile>();
				tile.sprite = sprite;
				tile.name = sprite.name;
				ctx.AddObjectToAsset(tile.name, tile);
			}
		}

	}
}
