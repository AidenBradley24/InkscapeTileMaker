using System;

namespace InkscapeTileMaker.Models
{
	public class Tile : IComparable<Tile>
	{
		public string Name { get; set; } = "";

		public TileType Type { get; set; } = TileType.Singular;

		public TileAlignment Allignment { get; set; } = TileAlignment.Core;

		public int Priority { get; set; } = 1;

		public int Row { get; set; }

		public int Column { get; set; }

		public string MaterialName { get; set; } = "";

		public int CompareTo(Tile? other)
		{
			if (other == null) return 1;
			int rowComparison = Row.CompareTo(other.Row);
			if (rowComparison != 0) return rowComparison;
			return Column.CompareTo(other.Column);
		}
	}
}
