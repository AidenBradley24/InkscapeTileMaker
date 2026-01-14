namespace InkscapeTileMaker.Services
{
	public class TempDirectoryService : ITempDirectoryService, IDisposable
	{
		private readonly DirectoryInfo _tempDir;

		public TempDirectoryService()
		{
			_tempDir = Directory.CreateTempSubdirectory("inkscape_tile_maker_");
		}

		public DirectoryInfo TempDir => _tempDir;

		public void Dispose()
		{
			GC.SuppressFinalize(this);
			try
			{
				_tempDir.Delete(true);
			}
			catch
			{
				// Ignore any exceptions during deletion
			}
		}
	}
}
