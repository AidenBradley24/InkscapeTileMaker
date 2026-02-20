using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace InkscapeTileMaker.Models
{
	/// <summary>
	/// Represents a tilemap that is composed of multiple other tilemaps. This allows for layering and combining different tilemaps together. <br/>
	/// This tilemap does not own the underlying tilemaps, so it does not dispose of them. This tilemap must be disposed when no longer needed to unsubscribe from events.
	/// </summary>
	public sealed class CompositeTilemap : ITilemap, IDisposable
	{
		private readonly ITilemap[] _tilemaps;
		private readonly Rect _rect;
		private bool _disposed;

		public CompositeTilemap(params ITilemap[] tilemaps)
		{
			if (tilemaps == null) throw new ArgumentNullException(nameof(tilemaps));
			if (tilemaps.Length == 0) throw new ArgumentException("At least one tilemap must be provided.", nameof(tilemaps));
			_tilemaps = tilemaps;

			var rect = _tilemaps[0].TileGridRect;
			foreach (var tilemap in _tilemaps)
			{
				tilemap.TilesInAreaChanged += area => TilesInAreaChanged(area);
				rect += tilemap.TileGridRect;
			}
			_rect = rect;
		}

		public Rect TileGridRect => _rect;

		public event Action<Rect> TilesInAreaChanged = delegate { };

		public void Dispose()
		{
			GC.SuppressFinalize(this);
			foreach (var tilemap in _tilemaps)
			{
				tilemap.TilesInAreaChanged -= area => TilesInAreaChanged(area);
			}
			_disposed = true;
		}

		public IEnumerator<(IReadOnlyList<TileData> overlappingTiles, (int x, int y) position)> GetEnumerator()
		{
			if (_disposed) throw new ObjectDisposedException(nameof(CompositeTilemap));
			return GetTilesInArea(TileGridRect)
				.Zip(TileGridRect.GetPositions(), (tiles, pos) => (overlappingTiles: tiles, position: pos))
				.GetEnumerator();
		}

		public IReadOnlyList<TileData> GetTilesAt(int x, int y)
		{
			if (_disposed) throw new ObjectDisposedException(nameof(CompositeTilemap));
			var result = new ConcatReadOnlyList<TileData>();
			foreach (var tilemap in _tilemaps)
			{
				result.Append(tilemap.GetTilesAt(x, y));
			}
			return result;
		}

		public IEnumerable<IReadOnlyList<TileData>> GetTilesInArea(Rect area)
		{
			if (_disposed) throw new ObjectDisposedException(nameof(CompositeTilemap));
			foreach (var (x, y) in area.GetPositions())
			{
				var result = new ConcatReadOnlyList<TileData>();
				foreach (var tilemap in _tilemaps)
				{
					result.Append(tilemap.GetTilesAt(x, y));
				}
				yield return result;
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		private class ConcatReadOnlyList<T> : IReadOnlyList<T>
		{
			private readonly List<IReadOnlyList<T>> _items;

			public ConcatReadOnlyList()
			{
				_items = new List<IReadOnlyList<T>>();
			}

			public T this[int index]
			{
				get
				{
					int currentIndex = 0;
					foreach (var list in _items)
					{
						if (index < currentIndex + list.Count)
						{
							return list[index - currentIndex];
						}
						currentIndex += list.Count;
					}
					throw new IndexOutOfRangeException();
				}
			}

			public int Count => _items.Aggregate(0, (sum, list) => sum + list.Count);

			public IEnumerator<T> GetEnumerator()
			{
				return _items.SelectMany(list => list).GetEnumerator();
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}

			public void Append(IReadOnlyList<T> list)
			{
				_items.Add(list);
			}
		}
	}
}
