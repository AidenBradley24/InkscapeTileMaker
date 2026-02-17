namespace InkscapeTileMaker.Services
{
	public interface ISettingsService
	{
		public void ResetToDefaults();

		// general settings
		public string InkscapePath { get; set; }

		// Unity export settings
		public string UnityImageExportPath { get; set; }
		public bool UnityExportTiles { get; set; }
		public string UnityEditorScriptPath { get; set; }
		public string UnityScriptPath { get; set; }
	}
}
