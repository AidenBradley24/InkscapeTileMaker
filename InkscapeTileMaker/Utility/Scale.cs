namespace InkscapeTileMaker.Utility
{
	public struct Scale
	{
		public int width;
		public int height;

		public Scale(int width, int height)
		{
			this.width = width;
			this.height = height;
		}

		public override readonly string ToString()
		{
			return $"{width}x{height}";
		}
	}
}
