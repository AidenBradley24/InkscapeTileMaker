using CommunityToolkit.Mvvm.ComponentModel;
using InkscapeTileMaker.Models;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Collections;

namespace InkscapeTileMaker.ViewModels
{
	public partial class TileViewModel : ObservableValidator
	{
		private readonly Tile _tile;
		private readonly DesignerViewModel _designerViewModel;
		private readonly Action<Tile> _syncFunction;

		[ObservableProperty]
		public partial ImageSource? PreviewImage { get; set; }

		[ObservableProperty]
		[NotifyDataErrorInfo]
		[Required]
		[MinLength(2)]
		[MaxLength(50)]
		[CustomValidation(typeof(TileViewModel), nameof(ValidateNameUnique))]
		public partial string Name { get; set; }

		[ObservableProperty]
		[NotifyDataErrorInfo]
		[CustomValidation(typeof(TileViewModel), nameof(ValidatePosition))]
		public partial (int row, int col) Position { get; set; }

		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(IsMaterial))]
		[NotifyPropertyChangedFor(nameof(VariantOptions))]
		public partial TileType Type { get; set; }

		[ObservableProperty]
		[NotifyDataErrorInfo]
		[CustomValidation(typeof(TileViewModel), nameof(ValidateVariant))]
		[NotifyPropertyChangedFor(nameof(AlignmentOptions))]
		[NotifyPropertyChangedFor(nameof(SecondaryAlignmentOptions))]
		public partial TileVariant Variant { get; set; }

		public IList<TileVariant> VariantOptions
		{
			get
			{
				var list = Type.GetValidVariants().ToList();
				list.Remove(Variant);
				list.Insert(0, Variant);
				return list;
			}
		}

		[ObservableProperty]
		[NotifyDataErrorInfo]
		[CustomValidation(typeof(TileViewModel), nameof(ValidateAlignment))]
		[NotifyPropertyChangedFor(nameof(AlignmentOptions))]
		[NotifyPropertyChangedFor(nameof(SecondaryAlignmentOptions))]
		public partial TileAlignment Alignment { get; set; }

		public IList<TileAlignment> AlignmentOptions
		{
			get
			{
				var list = Variant.GetValidAlignments().ToList();
				list.Remove(Alignment);
				list.Insert(0, Alignment);
				return list;
			}
		}

		public IList<TileAlignment> SecondaryAlignmentOptions
		{
			get
			{
				var validAlignments = Variant.GetValidAlignments().Except([Alignment]);
				var list = validAlignments.ToList();
				foreach (TileAlignment alignment in SecondaryAlignments)
				{
					list.Remove(alignment);
					list.Insert(0, alignment);
				}
				return list;
			}
		}

		[ObservableProperty]
		[NotifyDataErrorInfo]
		[CustomValidation(typeof(TileViewModel), nameof(ValidateSecondaryAlignments))]
		public partial IList SecondaryAlignments { get; set; }

		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(PriorityString))]
		public partial int Priority { get; set; }

		public string PriorityString
		{
			get => Priority.ToString();
			set
			{
				if (int.TryParse(value, out int result))
				{
					Priority = result;
				}
				else if (value == "")
				{
					Priority = 0;
				}
				else
				{
					Priority = 1;
					OnPropertyChanged(nameof(PriorityString));
				}
			}
		}

		[ObservableProperty]
		[NotifyDataErrorInfo]
		[NotifyPropertyChangedFor(nameof(IsMaterial))]
		[CustomValidation(typeof(TileViewModel), nameof(ValidateMaterialName))]
		public partial string MaterialName { get; set; }

		public Tile Value => _tile;

		public bool IsMaterial => Type != TileType.Singular;

		public string? CurrentErrorMessage
		{
			get
			{
				var errors = GetErrors();
				if (errors == null || !errors.Any()) return null;
				var firstError = errors.First().ErrorMessage;
				int remaining = errors.Count() - 1;
				if (remaining == 0)
					return $"Error: {firstError}";
				else
					return $"Error: {firstError} (+{remaining})";
			}
		}

		public void RunValidation()
		{
			ValidateAllProperties();
		}

		public TileViewModel(Tile tile, DesignerViewModel designerViewModel, Action<Tile> syncFunction)
		{
			_tile = tile;
			_designerViewModel = designerViewModel;
			_syncFunction = syncFunction;

			Name = _tile.Name;
			Position = (_tile.Row, _tile.Column);
			Type = _tile.Type;
			Variant = _tile.Variant;
			Alignment = _tile.Alignment;
			SecondaryAlignments = new List<TileAlignment>(_tile.SecondaryAlignments);
			MaterialName = _tile.MaterialName;
			Priority = _tile.Priority;

			ErrorsChanged += (_, _) => OnPropertyChanged(nameof(CurrentErrorMessage));
		}

		public void Sync()
		{
			_syncFunction.Invoke(_tile);
		}

		partial void OnNameChanged(string value)
		{
			_designerViewModel.HasUnsavedChanges |= _tile.Name != value;
			_tile.Name = value;
		}

		partial void OnPositionChanged((int row, int col) value)
		{
			_designerViewModel.HasUnsavedChanges |= _tile.Row != value.row || _tile.Column != value.col;
			_tile.Row = value.row;
			_tile.Column = value.col;
		}

		partial void OnTypeChanged(TileType value)
		{
			_designerViewModel.HasUnsavedChanges |= _tile.Type != value;
			_tile.Type = value;
			ValidateProperty(Variant, nameof(Variant));
		}

		partial void OnVariantChanged(TileVariant value)
		{
			_designerViewModel.HasUnsavedChanges |= _tile.Variant != value;
			_tile.Variant = value;
			ValidateProperty(Alignment, nameof(Alignment));
			ValidateProperty(SecondaryAlignments, nameof(SecondaryAlignments));
		}

		partial void OnAlignmentChanged(TileAlignment value)
		{
			_designerViewModel.HasUnsavedChanges |= _tile.Alignment != value;
			_tile.Alignment = value;
		}

		partial void OnSecondaryAlignmentsChanged(IList value)
		{
			_designerViewModel.HasUnsavedChanges |= !_tile.SecondaryAlignments.SequenceEqual(SecondaryAlignments.Cast<TileAlignment>());
			_tile.SecondaryAlignments.Clear();
			_tile.SecondaryAlignments.AddRange(SecondaryAlignments.Cast<TileAlignment>());
		}

		partial void OnPriorityChanged(int value)
		{
			_designerViewModel.HasUnsavedChanges |= _tile.Priority != value;
			_tile.Priority = value;
		}

		partial void OnMaterialNameChanged(string value)
		{
			_designerViewModel.HasUnsavedChanges |= _tile.MaterialName != value;
			_tile.MaterialName = value;
		}

		public static ValidationResult? ValidateNameUnique(string name, ValidationContext context)
		{
			var instance = (TileViewModel)context.ObjectInstance;

			foreach (var tile in instance._designerViewModel.Tiles)
			{
				if (tile != instance && tile.Name == name)
				{
					return new ValidationResult("Tile name must be unique.");
				}
			}

			foreach (var invalidChar in Path.GetInvalidFileNameChars())
			{
				if (name.Contains(invalidChar))
				{
					return new ValidationResult(
						$"Tile name cannot contain invalid file name characters (e.g. '{invalidChar}').");
				}
			}

			return ValidationResult.Success;
		}

		public static ValidationResult? ValidatePosition((int row, int col) position, ValidationContext context)
		{
			var instance = (TileViewModel)context.ObjectInstance;
			var size = instance._designerViewModel.TileSetSize;

			if (position.row < 0 || position.row > size.Height || position.col < 0 || position.col > size.Width)
			{
				return new ValidationResult("Tile position must be within the bounds of the tile set.");
			}

			return ValidationResult.Success;
		}

		public static ValidationResult? ValidateMaterialName(string materialName, ValidationContext context)
		{
			var instance = (TileViewModel)context.ObjectInstance;
			if (!instance.IsMaterial)
				return ValidationResult.Success;

			if (string.IsNullOrWhiteSpace(materialName))
			{
				return new ValidationResult("Material name is required for non-singular tiles.");
			}

			foreach (var invalidChar in Path.GetInvalidFileNameChars())
			{
				if (materialName.Contains(invalidChar))
				{
					return new ValidationResult(
						$"Tile name cannot contain invalid file name characters (e.g. '{invalidChar}').");
				}
			}

			foreach (var tile in instance._designerViewModel.Tiles)
			{
				if (tile.MaterialName == materialName && tile.Type != instance._tile.Type)
				{
					return new ValidationResult("Tiles in the same material must have the same type.");
				}
			}

			return ValidationResult.Success;
		}

		public static ValidationResult? ValidateAlignment(TileAlignment alignment, ValidationContext context)
		{
			var instance = (TileViewModel)context.ObjectInstance;

			var validAlignments = instance.Variant.GetValidAlignments();
			var isValid = false;

			foreach (var validAlignment in validAlignments)
			{
				if (validAlignment == alignment)
				{
					isValid = true;
					break;
				}
			}

			if (!isValid)
			{
				return new ValidationResult(
					$"Alignment '{alignment}' is not valid for tile variant '{instance.Variant}'.");
			}

			return ValidationResult.Success;
		}

		public static ValidationResult? ValidateSecondaryAlignments(IList alignments, ValidationContext context)
		{
			if (alignments == null) return ValidationResult.Success;
			var instance = (TileViewModel)context.ObjectInstance;
			var validAlignments = instance.Variant.GetValidAlignments().Except([instance.Alignment]);
			foreach (var item in alignments)
			{
				var alignment = item as TileAlignment?;
				if (alignment != null && !validAlignments.Contains(alignment.Value))
				{
					return new ValidationResult(
						$"Secondary alignment '{item}' is not valid for tile variant '{instance.Variant}'.");
				}
			}

			return ValidationResult.Success;
		}

		public static ValidationResult? ValidateVariant(TileVariant variant, ValidationContext context)
		{
			var instance = (TileViewModel)context.ObjectInstance;

			var validVariants = instance.Type.GetValidVariants();
			var isValid = false;

			foreach (var validVariant in validVariants)
			{
				if (validVariant == variant)
				{
					isValid = true;
					break;
				}
			}

			if (!isValid)
			{
				return new ValidationResult(
					$"Variant '{variant}' is not valid for tile type '{instance.Type}'.");
			}

			return ValidationResult.Success;
		}
	}
}
