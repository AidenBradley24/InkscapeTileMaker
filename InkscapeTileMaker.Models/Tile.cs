using System;

namespace InkscapeTileMaker.Models
{
	public class Tile : IComparable<Tile>
	{
		public string Name { get; set; } = "";

		public TileType Type { get; set; } = TileType.Singular;

		public TileVariant Variant { get; set; } = TileVariant.Core;

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
