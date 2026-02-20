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

		private readonly int left, top, right, bottom;

		public Rect(int left, int top, int right, int bottom)
		{
			this.left = left;
			this.top = top;
			this.right = right;
			this.bottom = bottom;
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

		public static Rect operator+(Rect a, Rect b)
		{
			var left = a.Left < b.Left ? a.Left : b.Left;
			var right = a.Right > b.Right ? a.Right : b.Right;
			var top = a.Top > b.Top ? a.Top : b.Top;
			var bottom = a.Bottom < b.Bottom ? a.Bottom : b.Bottom;

			return new Rect(left, top, right, bottom);
		}
	}
}
