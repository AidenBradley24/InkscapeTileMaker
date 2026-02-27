using System;
using System.Collections.Generic;

namespace InkscapeTileMaker.Models
{
	public readonly struct Rect
	{
		public int Left => left;
		public int Top => top;
		public int Right => right;
		public int Bottom => bottom;
		public (int x, int y) Position => (left, top);
		public int Width => right - left + 1;
		public int Height => bottom - top + 1;
		public Scale Scale => new Scale(Width, Height);

		private readonly int left, top, right, bottom;

		public Rect(int left, int top, int right, int bottom)
		{
			this.left = left;
			this.top = top;
			this.right = right;
			this.bottom = bottom;
		}

		public Rect((int x, int y) position, Scale scale)
		{
			this.left = position.x;
			this.top = position.y;
			this.right = position.x + scale.Width - 1;
			this.bottom = position.y + scale.Height - 1;
		}

		public Rect(Scale scale) : this((0, 0), scale) { }

		/// <summary>
		/// Get a square rectagle with the specified number of cells, where the width and height are equal and as small as possible to fit all cells. The rectangle will be positioned at the origin (0, 0).
		/// </summary>
		public Rect(int minimumCells)
		{
			int size = (int)Math.Ceiling(Math.Sqrt(minimumCells));
			this.left = 0;
			this.top = 0;
			this.right = size - 1;
			this.bottom = size - 1;
		}

		/// <summary>
		/// Get all positions within an area, inclusive of the edges.<br/>
		/// The positions are returned in row-major order, starting from the top-left corner.
		/// </summary>
		public readonly IEnumerable<(int x, int y)> GetPositions()
		{
			for (int y = top; y <= bottom; y++)
			{
				for (int x = left; x <= right; x++)
				{
					yield return (x, y);
				}
			}
		}

		public static Rect operator +(Rect a, Rect b)
		{
			var left = a.Left < b.Left ? a.Left : b.Left;
			var right = a.Right > b.Right ? a.Right : b.Right;
			var top = a.Top > b.Top ? a.Top : b.Top;
			var bottom = a.Bottom < b.Bottom ? a.Bottom : b.Bottom;

			return new Rect(left, top, right, bottom);
		}
	}
}
