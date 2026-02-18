using System.Diagnostics;

namespace InkscapeTileMaker.Services
{
	public class InkscapeService : IInkscapeService
	{
		private readonly ISettingsService _settingsService;

		public InkscapeService(ISettingsService settingsService)
		{
			_settingsService = settingsService;
		}

		public ProcessStartInfo GetProcessStartInfo()
		{
			return new ProcessStartInfo
			{
				FileName = Environment.ExpandEnvironmentVariables(_settingsService.InkscapePath),
				UseShellExecute = false,
				CreateNoWindow = true,
			};
		}

		public void OpenFileInInkscape(FileInfo file)
		{
			var startInfo = GetProcessStartInfo();
			startInfo.CreateNoWindow = false;
			startInfo.Arguments = $"\"{file.FullName}\"";
			_ = Process.Start(startInfo);
		}
	}
}
