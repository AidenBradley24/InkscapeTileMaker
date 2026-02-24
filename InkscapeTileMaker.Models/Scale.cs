using System;

namespace InkscapeTileMaker.Models
{
	public readonly struct Scale
	{
		public int Width { get; }
		public int Height { get; }

		public Scale(int width, int height)
		{
			Width = width;
			Height = height;

			if (width < 0) throw new ArgumentOutOfRangeException(nameof(width), "Width must be non-negative.");
			if (height < 0) throw new ArgumentOutOfRangeException(nameof(height), "Height must be non-negative.");
		}

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

		public static implicit operator (int width, int height)(Scale scale)
		{
			return (scale.Width, scale.Height);
		}

		public static implicit operator Scale((int width, int height) tuple)
		{
			return new Scale(tuple.width, tuple.height);
		}
	}
}
