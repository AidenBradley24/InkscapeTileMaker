namespace InkscapeTileMaker.Services
{
	public class SettingsService : ISettingsService
	{
		const string InkscapePathKey = "inkscape_path";
		const string InkscapePathDefault = "%ProgramFiles%\\Inkscape\\bin\\inkscape.exe";
		public string InkscapePath
		{
			get => Preferences.Get(InkscapePathKey, InkscapePathDefault);
			set => Preferences.Set(InkscapePathKey, value);
		}

		const string UnityImageExportPathKey = "unity_image_export_path";
		const string UnityImageExportPathDefault = "Assets/Sprites/Tiles";
		public string UnityImageExportPath
		{
			get => Preferences.Get(UnityImageExportPathKey, UnityImageExportPathDefault);
			set => Preferences.Set(UnityImageExportPathKey, value);
		}

		const string UnityExportTilesKey = "unity_export_tiles";
		const bool UnityExportTilesDefault = true;
		public bool UnityExportTiles
		{
			get => Preferences.Get(UnityExportTilesKey, UnityExportTilesDefault);
			set => Preferences.Set(UnityExportTilesKey, value);
		}
		
		const string UnityEditorScriptPathKey = "unity_editor_script_path";
		const string UnityEditorScriptPathDefault = "Assets/Editor/Tiles";
		public string UnityEditorScriptPath
		{
			get => Preferences.Get(UnityEditorScriptPathKey, UnityEditorScriptPathDefault);
			set => Preferences.Set(UnityEditorScriptPathKey, value);
		}

		const string UnityScriptPathKey = "unity_script_path";
		const string UnityScriptPathDefault = "Assets/Scripts/Tiles";
		public string UnityScriptPath
		{
			get => Preferences.Get(UnityScriptPathKey, UnityScriptPathDefault);
			set => Preferences.Set(UnityScriptPathKey, value);
		}

		public void ResetToDefaults()
		{
			Preferences.Clear();
		}
	}
}
