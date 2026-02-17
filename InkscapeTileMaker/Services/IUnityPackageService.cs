using UnityPackageNET;

namespace InkscapeTileMaker.Services
{
	public interface IUnityPackageService
	{
		public Task WriteTilesetPackageAsync(UnityPackageWriter writer, ITilesetConnection conn, ITilesetRenderingService renderingService);
	}
}
