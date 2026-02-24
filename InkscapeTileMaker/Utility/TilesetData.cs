using InkscapeTileMaker.Models;
using InkscapeTileMaker.Services;
using InkscapeTileMaker.ViewModels;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace InkscapeTileMaker.Utility
{
	public partial class TilesetData : ITileset
	{
		private readonly List<Tile> _tiles;

		public string Name { get; set; }

		public Scale TilePixelSize { get; set; }

		public Scale ImagePixelSize { get; set; }

		public int Count => _tiles.Count;

		public bool IsReadOnly => false;

		public TilesetData(string name, Scale tileSize, Scale size)
		{
			Name = name;
			TilePixelSize = tileSize;
			ImagePixelSize = size;
			_tiles = [];
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

		public Task FillTilesAsync(TilesetFillSettings settings, IProgress<double>? progressReporter = null)
		{
			throw new NotSupportedException();
		}

		public TileViewModel[] GetAllTileViewModels(DesignerViewModel designerViewModel)
		{
			throw new NotSupportedException();
		}

		public IEnumerator<Tile> GetEnumerator()
		{
			return _tiles.GetEnumerator();
		}

		public TileViewModel? GetTileViewModelAt(int row, int column, DesignerViewModel designerViewModel)
		{
			throw new NotSupportedException();
		}

		public bool Remove(Tile item)
		{
			return _tiles.Remove(item);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
