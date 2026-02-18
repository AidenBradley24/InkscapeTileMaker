using System.Diagnostics;

namespace InkscapeTileMaker.Services
{
	public interface IInkscapeService
	{
		public ProcessStartInfo GetProcessStartInfo();

		public void OpenFileInInkscape(FileInfo file);
	}
}
