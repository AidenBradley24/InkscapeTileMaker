using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace InkscapeTileMaker.Models
{
	public class Material : IEquatable<Material>
	{
		private readonly Func<IEnumerable<Tile>> _tilesProvider;
		private readonly string _name;

		public string Name => _name;

		public TileType Type => _tilesProvider().FirstOrDefault(t => t.MaterialName == _name)?.Type ?? TileType.Singular;

		public Material(string name, Func<IEnumerable<Tile>> tilesProvider)
		{
			_name = name;
			_tilesProvider = tilesProvider;
		}

		public IEnumerable<Tile> GetTiles()
		{
			return _tilesProvider().Where(t => t.MaterialName == _name);
		}

		/// <summary>
		/// Returns the tile of the specified variant.
		/// </summary>
		/// <remarks>
		/// If the prefered alignment isn't available, returns the best match accounting for secondary alignments and priority.<br/>
		/// If no tile of the specified variant is available, returns null.
		/// </remarks>
		public Tile? GetTile(TileVariant variant, TileAlignment preferredAlignment)
		{
			var baseTiles = GetTiles().Where(t => t.Variant == variant);
			if (!baseTiles.Any()) return null;

			var preferredTiles = baseTiles.Where(t => t.Alignment == preferredAlignment).OrderByDescending(t => t.Priority);
			if (preferredTiles.Any())
			{
				return preferredTiles.First();
			}

			preferredTiles = baseTiles.Where(t => t.SecondaryAlignments.Contains(preferredAlignment)).OrderByDescending(t => t.Priority);
			if (preferredTiles.Any())
			{
				return preferredTiles.First();
			}

			return null;
		}

		public bool HasTileVariant(TileVariant variant)
		{
			return GetTiles().Any(t => t.Variant == variant);
		}

		public static HashSet<Material> GetAllMaterials(Func<IEnumerable<Tile>> tilesProvider)
		{
			var materials = new Dictionary<string, Material>();
			foreach (var tile in tilesProvider())
			{
				if (!string.IsNullOrEmpty(tile.MaterialName) && !materials.ContainsKey(tile.MaterialName))
				{
					materials[tile.MaterialName] = new Material(tile.MaterialName, tilesProvider);
				}
			}
			return materials.Values.ToHashSet();
		}

		public bool Equals(Material? other)
		{
			if (other == null) return false;
			return _name.Equals(other._name, StringComparison.Ordinal);
		}

		public bool TryGetTileData(TileVariant variant, TileAlignment alignment, [NotNullWhen(true)] out TileData? tileData)
		{
			var tile = GetTile(variant, alignment);
			if (tile == null)
			{
				tileData = null;
				return false;
			}
			tileData = new TileData
			{
				Tile = tile,
				Transformation = TileTransformationHelpers.GetTransformationForAlignment(tile.Alignment, alignment)
			};
			return true;
		}

		public override int GetHashCode()
		{
			return _name.GetHashCode(StringComparison.Ordinal);
		}
	}
}
