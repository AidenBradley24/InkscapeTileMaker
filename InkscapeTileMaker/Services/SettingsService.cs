namespace InkscapeTileMaker.Services
{
	public class SettingsService : ISettingsService
	{
		const string InkscapePathKey = "inkscape_path";
		public string InkscapePath
		{
			get => Preferences.Get(InkscapePathKey, "%ProgramFiles%\\Inkscape\\bin\\inkscape.exe");
			set => Preferences.Set(InkscapePathKey, value);
		}
	}
}
