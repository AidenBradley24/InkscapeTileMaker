using CommunityToolkit.Mvvm.ComponentModel;
using InkscapeTileMaker.Services;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace InkscapeTileMaker.ViewModels
{
	public partial class SettingsViewModel : ObservableValidator
	{
		private readonly ISettingsService _settingsService;

		public SettingsViewModel(ISettingsService settingsService)
		{
			_settingsService = settingsService;
			InkscapePath = _settingsService.InkscapePath;
			UnityImageExportPath = _settingsService.UnityImageExportPath;
			UnityExportTiles = _settingsService.UnityExportTiles;
			UnityEditorScriptPath = _settingsService.UnityEditorScriptPath;
			UnityScriptPath = _settingsService.UnityScriptPath;

			ValidateAllProperties();
		}

		#region InkscapePath

		[ObservableProperty]
		[NotifyDataErrorInfo]
		[Required(ErrorMessage = "Inkscape path is required.")]
		[CustomValidation(typeof(SettingsViewModel), nameof(ValidateInkscapePath))]
		public partial string InkscapePath { get; set; }

		partial void OnInkscapePathChanged(string value)
		{
			_settingsService.InkscapePath = value;
		}

		public static ValidationResult? ValidateInkscapePath(string path, ValidationContext context)
		{
			if (string.IsNullOrWhiteSpace(path))
			{
				return ValidationResult.Success;
			}

			path = Environment.ExpandEnvironmentVariables(path);

			if (!File.Exists(path))
			{
				return new ValidationResult("Inkscape path must point to an existing file.");
			}

			return ValidationResult.Success;
		}

		#endregion

		#region UnityImageExportPath

		[ObservableProperty]
		[NotifyDataErrorInfo]
		[Required(ErrorMessage = "Unity image export path is required.")]
		[CustomValidation(typeof(SettingsViewModel), nameof(ValidateUnityImageExportPath))]
		public partial string UnityImageExportPath { get; set; }

		partial void OnUnityImageExportPathChanged(string value)
		{
			_settingsService.UnityImageExportPath = value;
		}

		public static ValidationResult? ValidateUnityImageExportPath(string path, ValidationContext context)
		{
			if (string.IsNullOrWhiteSpace(path))
			{
				return ValidationResult.Success;
			}

			if (!UnityPathRegex().IsMatch(path))
			{
				return new ValidationResult("Unity image export path must be a valid Unity Assets path (e.g. 'Assets/Folder/SubFolder').");
			}

			return ValidationResult.Success;
		}

		#endregion

		#region UnityExportTiles

		[ObservableProperty]
		public partial bool UnityExportTiles { get; set; }

		partial void OnUnityExportTilesChanged(bool value)
		{
			_settingsService.UnityExportTiles = value;
		}

		#endregion

		#region UnityEditorScriptPath

		[ObservableProperty]
		[NotifyDataErrorInfo]
		[Required(ErrorMessage = "Unity editor script path is required.")]
		[CustomValidation(typeof(SettingsViewModel), nameof(ValidateUnityEditorScriptPath))]
		public partial string UnityEditorScriptPath { get; set; }

		partial void OnUnityEditorScriptPathChanged(string value)
		{
			_settingsService.UnityEditorScriptPath = value;
		}

		public static ValidationResult? ValidateUnityEditorScriptPath(string path, ValidationContext context)
		{
			if (string.IsNullOrWhiteSpace(path))
			{
				return ValidationResult.Success;
			}

			if (!UnityPathRegex().IsMatch(path))
			{
				return new ValidationResult("Unity editor script path must be a valid Unity Assets path (e.g. 'Assets/Folder/SubFolder').");
			}

			return ValidationResult.Success;
		}

		#endregion

		#region UnityScriptPath

		[ObservableProperty]
		[NotifyDataErrorInfo]
		[Required(ErrorMessage = "Unity script path is required.")]
		[CustomValidation(typeof(SettingsViewModel), nameof(ValidateUnityScriptPath))]
		public partial string UnityScriptPath { get; set; }

		partial void OnUnityScriptPathChanged(string value)
		{
			_settingsService.UnityScriptPath = value;
		}

		public static ValidationResult? ValidateUnityScriptPath(string path, ValidationContext context)
		{
			if (string.IsNullOrWhiteSpace(path))
			{
				return ValidationResult.Success;
			}

			if (!UnityPathRegex().IsMatch(path))
			{
				return new ValidationResult("Unity script path must be a valid Unity Assets path (e.g. 'Assets/Folder/SubFolder').");
			}

			return ValidationResult.Success;
		}

		#endregion

		[GeneratedRegex(@"^Assets\/[^\/]+(?:\/[^\/]+)*$")]
		private static partial Regex UnityPathRegex();
	}
}
