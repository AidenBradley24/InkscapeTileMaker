using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace TileMaker
{
	[ScriptedImporter(1, "tmsx")]
	public class TileMakerImporter : ScriptedImporter
	{
		[System.Serializable]
		public class TileImporterSettings
		{
			public string Name;
			public string ImageGuid;
			public TileRecord[] Tiles;
			public int TileWidth;
			public int TileHeight;
		}

		[System.Serializable]
		public class TileRecord
		{
			public int Row;
			public int Col;
			public TileType Type;
			public TileVariant Variant;
			public TileAlignment Alignment;
			public string MaterialName;

			// these must be updated with tilemaker enums

			public enum TileType
			{
				Singular,
				DualTileMaterial,
			}

			public enum TileVariant
			{
				Core,
				Edge,
				InnerCorner,
				OuterCorner,
				Diagonal,
				Void
			}

			public enum TileAlignment
			{
				Core,

				TopEdge,
				RightEdge,
				BottomEdge,
				LeftEdge,

				TopLeftOuterCorner,
				TopRightOuterCorner,
				BottomRightOuterCorner,
				BottomLeftOuterCorner,

				TopLeftInnerCorner,
				TopRightInnerCorner,
				BottomRightInnerCorner,
				BottomLeftInnerCorner,

				DiagonalTopLeftToBottomRight,
				DiagonalTopRightToBottomLeft,
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
			if (!GUID.TryParse(importer.ImageGuid, out var imageGuid))
			{
				throw new System.Exception($"unable to get guid of image! \"{importer.ImageGuid}\"");
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

				int col = Mathf.FloorToInt(rect.x / importer.TileWidth);
				int rowFromBottom = Mathf.FloorToInt(rect.y / importer.TileHeight);
				int rowFromTop = (textureHeight / importer.TileHeight) - 1 - rowFromBottom;

				var key = (rowFromTop, col);
				if (!spriteLookup.ContainsKey(key))
				{
					spriteLookup.Add(key, sprite);
				}
			}

			if (importer.Tiles.Length == 0)
			{
				Debug.LogWarning("No tiles found in importer!");
				return;
			}

			if (!importer.Tiles.Select(t => t.Type).All(t => t.Equals(importer.Tiles[0])))
			{
				Debug.LogError($"Inconsitant tile types!: \n{string.Join('\n', importer.Tiles.Select(t => t.Type).Distinct())}");
				return;
			}

			var tileType = importer.Tiles[0].Type;
			switch (tileType)
			{
				case TileRecord.TileType.Singular:
					ImportSingularTiles(ctx, importer, spriteLookup);
					break;
				case TileRecord.TileType.DualTileMaterial:
					ImportDualRuleTile(ctx, importer, spriteLookup);
					break;
			}
		}

		private void ImportSingularTiles(
			AssetImportContext ctx,
			TileImporterSettings importer,
			Dictionary<(int row, int col), Sprite> spriteLookup)
		{
			foreach (var record in importer.Tiles)
			{
				if (!spriteLookup.TryGetValue((record.Row, record.Col), out var sprite))
				{
					Debug.LogWarning($"No sprite found for r:{record.Row},c:{record.Col}");
					continue;
				}

				var tile = ScriptableObject.CreateInstance<Tile>();
				tile.sprite = sprite;
				tile.name = sprite.name;
				ctx.AddObjectToAsset(tile.name, tile);
			}
		}

		private void ImportDualRuleTile(
			AssetImportContext ctx,
			TileImporterSettings importer,
			Dictionary<(int row, int col), Sprite> spriteLookup)
		{
			// attempt to get the skner duel tile
			Debug.Log("Dual rule tiles require manual import." +
				"\n\nThis package can be used with duel tiles:" +
				"\nhttps://github.com/skner-dev/skner.DualGrid");
		}
	}
}
