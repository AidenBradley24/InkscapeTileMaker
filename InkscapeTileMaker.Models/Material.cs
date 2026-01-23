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
		/// Returns the tile of the specified type.<br/>
		/// Attempts to return a tile with preferred alignment, but will return any tile of the specified type if none with the preferred alignment exists.<br/>
		/// Also accounts for <see cref="Tile.Priority"/>.
		/// </summary>
		public Tile? GetTile(TileType type, TileAlignment preferredAlignment)
		{
			var baseTiles = GetTiles().Where(t => t.Type == type);
			if (!baseTiles.Any()) return null;
			var preferredTiles = baseTiles.Where(t => t.Allignment == preferredAlignment);
			if (preferredTiles.Any())
			{
				return preferredTiles.OrderByDescending(t => t.Priority).First();
			}
			return baseTiles.OrderByDescending(t => t.Priority).First();
		}

		public bool HasTileType(TileType type)
		{
			return GetTiles().Any(t => t.Type == type);
		}

		public static List<Material> GetAllMaterials(Func<IEnumerable<Tile>> tilesProvider)
		{
			var materials = new Dictionary<string, Material>();
			foreach (var tile in tilesProvider())
			{
				if (!string.IsNullOrEmpty(tile.MaterialName) && !materials.ContainsKey(tile.MaterialName.ToLowerInvariant()))
				{
					materials[tile.MaterialName.ToLowerInvariant()] = new Material(tile.MaterialName, tilesProvider);
				}
			}
			return materials.Values.ToList();
		}

		public bool Equals(Material other)
		{
			if (other == null) return false;
			return _name.Equals(other._name, StringComparison.InvariantCultureIgnoreCase);
		}

		public bool TryGetTileData(TileType type, TileAlignment alignment, [NotNullWhen(true)] out TileData? tileData)
		{
			var tile = GetTile(type, alignment);
			if (tile == null)
			{
				tileData = null;
				return false;
			}
			tileData = new TileData
			{
				tile = tile,
				transformation = TileTransformationHelpers.GetTransformationForAlignment(tile.Allignment, alignment)
			};
			return true;
		}
	}
}
