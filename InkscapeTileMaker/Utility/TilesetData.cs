using InkscapeTileMaker.Models;
using InkscapeTileMaker.Services;
using System.Collections;

namespace InkscapeTileMaker.Utility
{
	public partial class TilesetData : ITileset
	{
		private readonly List<Tile> _tiles;

		public string Name { get; set; }

		public FileInfo? ImageFile { get; set; }

		public Scale TilePixelSize { get; set; }

		public Scale ImagePixelSize { get; set; }

		public int Count => _tiles.Count;

		public bool IsReadOnly => false;

		public TilesetData(string name, Scale tilePixelSize, Scale imagePixelSize)
		{
			Name = name;
			TilePixelSize = tilePixelSize;
			ImagePixelSize = imagePixelSize;
			_tiles = [];
		}

		public TilesetData(string name, Scale tilePixelSize, Scale imagePixelSize, IEnumerable<Tile> tiles)
		{
			Name = name;
			TilePixelSize = tilePixelSize;
			ImagePixelSize = imagePixelSize;
			_tiles = [.. tiles];
		}

		public TilesetData(string name, Scale tilePixelSize, Scale imagePixelSize, IEnumerable<Tile> tiles, FileInfo file)
		{
			Name = name;
			TilePixelSize = tilePixelSize;
			ImagePixelSize = imagePixelSize;
			_tiles = [.. tiles];
			ImageFile = file;
		}

		public TilesetData(ITileset tileset)
		{
			Name = tileset.Name;
			TilePixelSize = tileset.TilePixelSize;
			ImagePixelSize = tileset.ImagePixelSize;
			_tiles = [.. tileset];
		}

		public void Add(Tile item)
		{
			if (item == null) return;
			_tiles.Add(item);
		}

		public void Clear()
		{
			_tiles.Clear();
		}

		public bool Contains(Tile item)
		{
			return _tiles.Contains(item);
		}

		public void CopyTo(Tile[] array, int arrayIndex)
		{
			_tiles.CopyTo(array, arrayIndex);
		}

		public IEnumerator<Tile> GetEnumerator()
		{
			return _tiles.GetEnumerator();
		}

		public bool Remove(Tile item)
		{
			return _tiles.Remove(item);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public Tile? GetTileAt(int row, int column)
		{
			return _tiles.Find(t => t.Row == row && t.Column == column);
		}

		public Tile[] GetAllTiles()
		{
			return _tiles.ToArray();
		}

		public void Update(Tile tile)
		{
			var existingTile = _tiles.Find(t => t.Row == tile.Row && t.Column == tile.Column);
			if (existingTile != null)
			{
				_tiles[_tiles.IndexOf(existingTile)] = tile;
			}
		}
	}
}
