using System.Xml.Serialization;

namespace InkscapeTileMaker.Models;

[XmlRoot("tile", Namespace = "https://github.com/AidenBradley24/InkscapeTileMaker")]
public class Tile : IComparable<Tile>
{
	[XmlAttribute("name")]
	public string Name { get; set; } = "";

	[XmlAttribute("type")]
	public TileType Type { get; set; } = TileType.Singular;

	[XmlAttribute("rotation")]
	public RotationAlignment Rotation { get; set; } = RotationAlignment.None;

	[XmlAttribute("row")]
	public int Row { get; set; }

	[XmlAttribute("column")]
	public int Column { get; set; }

	[XmlAttribute("materialName")]
	public string MaterialName { get; set; } = "";

	public int CompareTo(Tile? other)
	{
		if (other == null) return 1;
		int rowComparison = Row.CompareTo(other.Row);
		if (rowComparison != 0) return rowComparison;
		return Column.CompareTo(other.Column);
	}
}
