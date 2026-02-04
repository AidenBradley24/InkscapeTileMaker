using InkscapeTileMaker.Models;
using SkiaSharp;
using System.Xml.Linq;

namespace InkscapeTileMaker.Utility
{
	public static class TileExtensions
	{
		public static SKMatrix ToSKMatrix(this TileTransformation transformation)
		{
			var matrix = SKMatrix.CreateIdentity();

			if (transformation.HasFlag(TileTransformation.FlipHorizontal))
			{
				var flipMatrix = SKMatrix.CreateScale(-1, 1);
				matrix = SKMatrix.Concat(matrix, flipMatrix);
			}

			if (transformation.HasFlag(TileTransformation.FlipVertical))
			{
				var flipMatrix = SKMatrix.CreateScale(1, -1);
				matrix = SKMatrix.Concat(matrix, flipMatrix);
			}

			if (transformation.HasFlag(TileTransformation.Rotate90))
			{
				var rotateMatrix = SKMatrix.CreateRotationDegrees(90);
				matrix = SKMatrix.Concat(matrix, rotateMatrix);
			}
			else if (transformation.HasFlag(TileTransformation.Rotate180))
			{
				var rotateMatrix = SKMatrix.CreateRotationDegrees(180);
				matrix = SKMatrix.Concat(matrix, rotateMatrix);
			}
			else if (transformation.HasFlag(TileTransformation.Rotate270))
			{
				var rotateMatrix = SKMatrix.CreateRotationDegrees(270);
				matrix = SKMatrix.Concat(matrix, rotateMatrix);
			}

			return matrix;
		}

		public static Tile GetTileFromXElement(XElement element)
		{
			ArgumentNullException.ThrowIfNull(element);
			if (element.Name != InkscapeSvg.tileName) throw new ArgumentException("Invalid element name", nameof(element));
			return new Tile
			{
				Name = (string?)element.Attribute("name") ?? "",
				Type = Enum.TryParse<TileType>((string?)element.Attribute("type") ?? "Singular", out var type) ? type : TileType.Singular,
				Allignment = Enum.TryParse<TileAlignment>((string?)element.Attribute("allignment") ?? "Core", out var alignment) ? alignment : TileAlignment.Core,
				Priority = (int?)element.Attribute("priority") ?? 1,
				Row = (int?)element.Attribute("row") ?? 0,
				Column = (int?)element.Attribute("column") ?? 0,
				MaterialName = (string?)element.Attribute("materialname") ?? ""
			};
		}

		public static XElement ToXElement(this Tile tile)
		{
			return new XElement(InkscapeSvg.tileName,
				new XAttribute("name", tile.Name),
				new XAttribute("type", tile.Type.ToString()),
				new XAttribute("allignment", tile.Allignment.ToString()),
				new XAttribute("priority", tile.Priority),
				new XAttribute("row", tile.Row),
				new XAttribute("column", tile.Column),
				new XAttribute("materialname", tile.MaterialName)
			);
		}
	}
}
