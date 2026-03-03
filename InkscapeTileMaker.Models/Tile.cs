using System;
using System.Collections.Generic;
using System.Xml.Serialization;

#if NET5_0_OR_GREATER
using System.Text.Json.Serialization;
#endif

namespace InkscapeTileMaker.Models
{
	public class Tile : IComparable<Tile>
	{
		public string Name { get; set; } = "";

		public TileType Type { get; set; } = TileType.Singular;

		public TileVariant Variant { get; set; } = TileVariant.Core;

		public TileAlignment Alignment { get; set; } = TileAlignment.Core;

		public List<TileAlignment> SecondaryAlignments
		{
			get;
#if NET5_0_OR_GREATER
			init;
#else
			set;
#endif
		} = new List<TileAlignment>();

		public int Priority { get; set; } = 1;

		public int Row { get; set; }

		public int Column { get; set; }

		[XmlIgnore]
#if NET5_0_OR_GREATER
		[JsonIgnore]
#endif
		public (int x, int y) Position
		{
			get
			{
				return (Column, Row);
			}

			set
			{
				Column = value.x;
				Row = value.y;
			}
		}

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

		public Tile Clone(int row, int column)
		{
			return new Tile
			{
				Name = this.Name,
				Type = this.Type,
				Variant = this.Variant,
				Alignment = this.Alignment,
				Row = row,
				Column = column,
				MaterialName = this.MaterialName,
				SecondaryAlignments = new List<TileAlignment>(this.SecondaryAlignments),
				Priority = this.Priority
			};
		}
	}
}
