using InkscapeTileMaker.Models;
using InkscapeTileMaker.Utility;
using InkscapeTileMaker.ViewModels;
using System.Collections;

namespace InkscapeTileMaker.Services
{
	public partial class InkscapeSvgTileset : ITileset, ICollection<Tile>
	{
		private readonly InkscapeSvgConnectionService _connection;

		public Scale TileSize => _connection.GetTileSize();

		public Scale Size => _connection.GetSvgSize();

		public int Count => _connection.GetTileCount();

		public bool IsReadOnly => false;

		public InkscapeSvgTileset(InkscapeSvgConnectionService connection)
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
			return _connection.Tiles.Contains(item);
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
			return _connection.Tiles.GetEnumerator();
		}

		public bool Remove(Tile item)
		{
			return _connection.RemoveTile(item);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public TileViewModel? GetTileViewModelAt(int row, int column, DesignerViewModel designerViewModel)
		{
			return _connection.GetTile(row, column, designerViewModel);
		}

		public TileViewModel[] GetAllTileViewModels(DesignerViewModel designerViewModel)
		{
			return _connection.GetAllTiles(designerViewModel);
		}

		public async Task FillTilesAsync(TilesetFillSettings settings, IProgress<double>? progressReporter = default)
		{
			await _connection.FillTilesAsync(settings, progressReporter);
		}
	}
}
