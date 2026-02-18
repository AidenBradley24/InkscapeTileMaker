using CommunityToolkit.Mvvm.ComponentModel;
using InkscapeTileMaker.Services;
using System.Collections;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace InkscapeTileMaker.ViewModels
{
	public partial class SettingsViewModel : ObservableObject, INotifyDataErrorInfo
	{
		private readonly ISettingsService _settingsService;
		private readonly Dictionary<string, List<string>> _errors = new();

		public SettingsViewModel(ISettingsService settingsService)
		{
			_settingsService = settingsService;
			ValidateInkscapePath();
			ValidateUnityImageExportPath();
			ValidateUnityEditorScriptPath();
			ValidateUnityScriptPath();
		}

		#region InkscapePath

		public string InkscapePath
		{
			get => _settingsService.InkscapePath;
			set
			{
				if (value != _settingsService.InkscapePath)
				{
					_settingsService.InkscapePath = value;
					OnPropertyChanged(nameof(InkscapePath));
					ValidateInkscapePath();
				}
			}
		}

		[ObservableProperty]
		public partial string? InkscapePathError { get; set; }

		private void ValidateInkscapePath()
		{
			const string propertyName = nameof(InkscapePath);
			var errors = new List<string>();

			var path = _settingsService.InkscapePath;
			path = Environment.ExpandEnvironmentVariables(path);

			if (string.IsNullOrWhiteSpace(path))
			{
				errors.Add("Inkscape path is required.");
			}
			else if (!File.Exists(path))
			{
				errors.Add("Inkscape path must point to an existing file.");
			}

			UpdateErrors(propertyName, errors);
			InkscapePathError = errors.FirstOrDefault();
		}

		#endregion

		#region UnityImageExportPath

		public string UnityImageExportPath
		{
			get => _settingsService.UnityImageExportPath;
			set
			{
				if (value != _settingsService.UnityImageExportPath)
				{
					_settingsService.UnityImageExportPath = value;
					OnPropertyChanged(nameof(UnityImageExportPath));
					ValidateUnityImageExportPath();
				}
			}
		}

		[ObservableProperty]
		public partial string? UnityImageExportPathError { get; set; }

		private void ValidateUnityImageExportPath()
		{
			const string propertyName = nameof(UnityImageExportPath);
			var errors = new List<string>();

			var path = _settingsService.UnityImageExportPath;

			if (string.IsNullOrWhiteSpace(path))
			{
				errors.Add("Unity image export path is required.");
			}
			else if (!UnityPathRegex().IsMatch(path))
			{
				errors.Add("Unity image export path must be a valid Unity Assets path (e.g. 'Assets/Folder/SubFolder').");
			}

			UpdateErrors(propertyName, errors);
			UnityImageExportPathError = errors.FirstOrDefault();
		}

		#endregion

		#region UnityExportTiles

		public bool UnityExportTiles
		{
			get => _settingsService.UnityExportTiles;
			set
			{
				if (value != _settingsService.UnityExportTiles)
				{
					_settingsService.UnityExportTiles = value;
					OnPropertyChanged(nameof(UnityExportTiles));
				}
			}
		}

		#endregion

		#region UnityEditorScriptPath

		public string UnityEditorScriptPath
		{
			get => _settingsService.UnityEditorScriptPath;
			set
			{
				if (value != _settingsService.UnityEditorScriptPath)
				{
					_settingsService.UnityEditorScriptPath = value;
					OnPropertyChanged(nameof(UnityEditorScriptPath));
					ValidateUnityEditorScriptPath();
				}
			}
		}

		[ObservableProperty]
		public partial string? UnityEditorScriptPathError { get; set; }

		private void ValidateUnityEditorScriptPath()
		{
			const string propertyName = nameof(UnityEditorScriptPath);
			var errors = new List<string>();

			var path = _settingsService.UnityEditorScriptPath;

			if (string.IsNullOrWhiteSpace(path))
			{
				errors.Add("Unity editor script path is required.");
			}
			else if (!UnityPathRegex().IsMatch(path))
			{
				errors.Add("Unity editor script path must be a valid Unity Assets path (e.g. 'Assets/Folder/SubFolder').");
			}

			UpdateErrors(propertyName, errors);
			UnityEditorScriptPathError = errors.FirstOrDefault();
		}

		#endregion

		#region UnityScriptPath

		public string UnityScriptPath
		{
			get => _settingsService.UnityScriptPath;
			set
			{
				if (value != _settingsService.UnityScriptPath)
				{
					_settingsService.UnityScriptPath = value;
					OnPropertyChanged(nameof(UnityScriptPath));
					ValidateUnityScriptPath();
				}
			}
		}

		[ObservableProperty]
		public partial string? UnityScriptPathError { get; set; }

		private void ValidateUnityScriptPath()
		{
			const string propertyName = nameof(UnityScriptPath);
			var errors = new List<string>();

			var path = _settingsService.UnityScriptPath;

			if (string.IsNullOrWhiteSpace(path))
			{
				errors.Add("Unity script path is required.");
			}
			else if (!UnityPathRegex().IsMatch(path))
			{
				errors.Add("Unity script path must be a valid Unity Assets path (e.g. 'Assets/Folder/SubFolder').");
			}

			UpdateErrors(propertyName, errors);
			UnityScriptPathError = errors.FirstOrDefault();
		}

		#endregion

		[GeneratedRegex(@"^Assets\/[^\/]+(?:\/[^\/]+)*$")]
		private static partial Regex UnityPathRegex();

		public bool HasErrors => _errors.Count > 0;

		public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

		public IEnumerable GetErrors(string? propertyName)
		{
			if (string.IsNullOrEmpty(propertyName))
			{
				foreach (var kvp in _errors)
				{
					foreach (var error in kvp.Value)
					{
						yield return error;
					}
				}
				yield break;
			}

			if (_errors.TryGetValue(propertyName, out var propertyErrors))
			{
				foreach (var error in propertyErrors)
				{
					yield return error;
				}
			}
		}

		private void UpdateErrors(string propertyName, List<string> propertyErrors)
		{
			var hadErrors = _errors.ContainsKey(propertyName);

			if (propertyErrors.Count == 0)
			{
				if (hadErrors)
				{
					_errors.Remove(propertyName);
					ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
				}
			}
			else
			{
				var hadDifferent =
					!hadErrors ||
					_errors[propertyName].Count != propertyErrors.Count ||
					!_errors[propertyName].SequenceEqual(propertyErrors);

				if (hadDifferent)
				{
					_errors[propertyName] = propertyErrors;
					ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
				}
			}
		}
	}
}
