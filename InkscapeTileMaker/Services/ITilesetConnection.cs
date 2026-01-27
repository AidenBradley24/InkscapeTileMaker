namespace InkscapeTileMaker.Services
{
	public interface ITilesetConnection
	{
		public ITileset? Tileset { get; }

		public FileInfo? CurrentFile { get; }

		public event Action<ITileset> TilesetChanged;

		public void Save(FileInfo file);

		public Task SaveToStreamAsync(Stream stream);

		public void Load(FileInfo file);
	}
}
