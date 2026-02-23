namespace InkscapeTileMaker.Utility
{
	public struct Scale(int width, int height)
	{
		public int Width { get; set; } = width;
		public int Height { get; set; } = height;

		public override readonly string ToString()
		{
			return $"{Width}x{Height}";
		}

		public static Scale operator +(Scale a, Scale b)
		{
			return new Scale(a.Width + b.Width, a.Height + b.Height);
		}

		public static Scale operator -(Scale a, Scale b)
		{
			return new Scale(a.Width - b.Width, a.Height - b.Height);
		}

		public static Scale operator *(Scale a, int b)
		{
			return new Scale(a.Width * b, a.Height * b);
		}

		public static Scale operator /(Scale a, int b)
		{
			return new Scale(a.Width / b, a.Height / b);
		}

		public static Scale operator *(Scale a, Scale b)
		{
			return new Scale(a.Width * b.Width, a.Height * b.Height);
		}

		public static Scale operator /(Scale a, Scale b)
		{
			return new Scale(a.Width / b.Width, a.Height / b.Height);
		}

		public override readonly int GetHashCode()
		{
			return HashCode.Combine(Width, Height);
		}
	}
}
