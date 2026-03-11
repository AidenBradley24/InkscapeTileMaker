using InkscapeTileMaker.Models;
using System.Collections;

namespace InkscapeTileMaker.Services
{
	public partial class InkscapeSvgTileset : ITileset, ICollection<Tile>
	{
		private readonly InkscapeSvgConnection _connection;

		public string Name
		{
			get
			{
				var file = _connection.CurrentFile;
				if (file == null) return string.Empty;
				return Path.GetFileNameWithoutExtension(file.Name);
			}
		}

		public Scale TilePixelSize => _connection.GetTileSize();

		public Scale ImagePixelSize => _connection.GetSvgSize();

		public int Count => _connection.GetTileCount();

		public bool IsReadOnly => false;

		public InkscapeSvgTileset(InkscapeSvgConnection connection)
		{
			_connection = connection;
		}

		public void Add(Tile item)
		{
			_connection.AddTile(item);
		}

		public void Clear()
		{
			_connection.ClearTiles();
		}

		public bool Contains(Tile item)
		{
			return _connection.CheckContainsTile(item);
		}

		public void CopyTo(Tile[] array, int arrayIndex)
		{
			int count = Count;
			var enumerator = GetEnumerator();
			for (int i = 0; i < count && enumerator.MoveNext(); i++)
			{
				array[arrayIndex + i] = enumerator.Current;
			}
		}

		public IEnumerator<Tile> GetEnumerator()
		{
			return ((IEnumerable<Tile>)_connection.GetAllTiles()).GetEnumerator();
		}

		public bool Remove(Tile item)
		{
			return _connection.RemoveTile(item);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public Tile? GetTileAt(int row, int column)
		{
			return _connection.GetTileAt(row, column);
		}

		public Tile[] GetAllTiles()
		{
			return _connection.GetAllTiles();
		}

		public async Task FillTilesAsync(TilesetFillSettings settings, IProgress<double>? progressReporter = default)
		{
			await _connection.FillTilesAsync(settings, progressReporter);
		}

		public void Update(Tile tile)
		{
			_connection.AddOrReplaceTile(tile);
		}
	}
}
