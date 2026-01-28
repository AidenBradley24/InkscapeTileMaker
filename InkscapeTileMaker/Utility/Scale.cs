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

		public static Scale operator +(Scale a, Scale b)
		{
			return new Scale(a.width + b.width, a.height + b.height);
		}

		public static Scale operator -(Scale a, Scale b)
		{
			return new Scale(a.width - b.width, a.height - b.height);
		}

		public static Scale operator *(Scale a, int b)
		{
			return new Scale(a.width * b, a.height * b);
		}

		public static Scale operator /(Scale a, int b)
		{
			return new Scale(a.width / b, a.height / b);
		}
	}
}
