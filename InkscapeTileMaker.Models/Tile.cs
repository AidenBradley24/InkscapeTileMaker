using System;

namespace InkscapeTileMaker.Models
{
	public class Tile : IComparable<Tile>
	{
		// TODO enforce name is unique, not null or whitespace or empty, only file path valid characters
		public string Name { get; set; } = "";

		public TileType Type { get; set; } = TileType.Singular;

		public TileAlignment Allignment { get; set; } = TileAlignment.Core;

		public int Priority { get; set; } = 1;

		public int Row { get; set; }

		public int Column { get; set; }

		// TODO enforce material name is not null or whitespace, only file path valid characters (but can be empty)
		// also make it case-sensitive everywhere
		public string MaterialName { get; set; } = "";

		public int CompareTo(Tile? other)
		{
			if (other == null) return 1;
			int rowComparison = Row.CompareTo(other.Row);
			if (rowComparison != 0) return rowComparison;
			return Column.CompareTo(other.Column);
		}

		public override bool Equals(object? obj)
		{
			if (obj is Tile other)
			{
				return Name == other.Name && Row == other.Row && Column == other.Column;
			}
			return false;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Name, Row, Column);
		}
	}
}
