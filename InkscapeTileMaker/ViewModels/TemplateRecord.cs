using InkscapeTileMaker.Utility;

namespace InkscapeTileMaker.ViewModels
{
	public record TemplateRecord(string Name, string Path)
	{
		public Scale TilesetSize { get; init; }
		public Scale TileSize { get; init; }
	}
}
