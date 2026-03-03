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
			if (element.Name != InkscapeSvg.TileName) throw new ArgumentException("Invalid element name", nameof(element));
			return new Tile
			{
				Name = (string?)element.Attribute(TileXNames.Name) ?? "",
				Type = Enum.TryParse<TileType>((string?)element.Attribute(TileXNames.Type) ?? "Singular", out var type) ? type : TileType.Singular,
				Variant = Enum.TryParse<TileVariant>((string?)element.Attribute(TileXNames.Variant) ?? "Core", out var variant) ? variant : TileVariant.Core,
				Alignment = Enum.TryParse<TileAlignment>((string?)element.Attribute(TileXNames.Alignment) ?? "Core", out var alignment) ? alignment : TileAlignment.Core,
				SecondaryAlignments = ((string?)element.Attribute(TileXNames.SecondaryAlignments) ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).Select(a => Enum.TryParse<TileAlignment>(a, out var sa) ? sa : TileAlignment.Core).ToList(),
				Priority = (int?)element.Attribute(TileXNames.Priority) ?? 1,
				Row = (int?)element.Attribute(TileXNames.Row) ?? 0,
				Column = (int?)element.Attribute(TileXNames.Column) ?? 0,
				MaterialName = (string?)element.Attribute(TileXNames.MaterialName) ?? ""
			};
		}

		public static XElement ToXElement(this Tile tile)
		{
			return new XElement(InkscapeSvg.TileName,
				new XAttribute(TileXNames.Name, tile.Name),
				new XAttribute(TileXNames.Type, tile.Type.ToString()),
				new XAttribute(TileXNames.Variant, tile.Variant.ToString()),
				new XAttribute(TileXNames.Alignment, tile.Alignment.ToString()),
				new XAttribute(TileXNames.SecondaryAlignments, string.Join(',', tile.SecondaryAlignments)),
				new XAttribute(TileXNames.Priority, tile.Priority),
				new XAttribute(TileXNames.Row, tile.Row),
				new XAttribute(TileXNames.Column, tile.Column),
				new XAttribute(TileXNames.MaterialName, tile.MaterialName)
			);
		}
	}
}
