using InkscapeTileMaker.Services;
using SkiaSharp;
using System.Xml.Serialization;

namespace InkscapeTileMaker.Models;

[XmlRoot("tile", Namespace = SvgConnectionService.appNamespacePrefix)]
public class Tile : IComparable<Tile>
{
	public string Name { get; set; } = "";
	public TileType Type { get; set; } = TileType.Singular;
	public RotationAlignment Rotation { get; set; } = RotationAlignment.None;
	public int Row { get; set; }
	public int Column { get; set; }

	public int CompareTo(Tile? other)
	{
		if (other == null) return 1;
		int rowComparison = Row.CompareTo(other.Row);
		if (rowComparison != 0) return rowComparison;
		return Column.CompareTo(other.Column);
	}
}
