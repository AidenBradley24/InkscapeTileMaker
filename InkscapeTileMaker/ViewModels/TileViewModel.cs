using CommunityToolkit.Mvvm.ComponentModel;
using InkscapeTileMaker.Models;
using System.ComponentModel.DataAnnotations;

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
		[Required]
		[MinLength(2)]
		[MaxLength(50)]
		[CustomValidation(typeof(TileViewModel), nameof(ValidateNameUnique))]
		public partial string Name { get; set; }

		[ObservableProperty]
		[CustomValidation(typeof(TileViewModel), nameof(ValidatePosition))]
		public partial (int row, int col) Position { get; set; }

		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(IsMaterial))]
		public partial TileType Type { get; set; }

		[ObservableProperty]
		[CustomValidation(typeof(TileViewModel), nameof(ValidateAllignment))]
		public partial TileAlignment Allignment { get; set; }

		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(IsMaterial))]
		[CustomValidation(typeof(TileViewModel), nameof(ValidateMaterialName))]
		public partial string MaterialName { get; set; }

		public Tile Value => _tile;

		public bool IsMaterial => !string.IsNullOrEmpty(_tile.MaterialName) &&
			(Type == TileType.MatCore || Type == TileType.MatEdge || Type == TileType.MatOuterCorner || Type == TileType.MatInnerCorner || Type == TileType.MatDiagonal);

		public TileViewModel(Tile tile, DesignerViewModel designerViewModel, Action<Tile> syncFunction)
		{
			_tile = tile;
			_designerViewModel = designerViewModel;
			_syncFunction = syncFunction;

			Name = _tile.Name;
			Position = (_tile.Row, _tile.Column);
			Type = _tile.Type;
			Allignment = _tile.Allignment;
			MaterialName = _tile.MaterialName;
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
		}

		partial void OnAllignmentChanged(TileAlignment value)
		{
			_designerViewModel.HasUnsavedChanges |= _tile.Allignment != value;
			_tile.Allignment = value;
		}

		partial void OnMaterialNameChanged(string value)
		{
			_designerViewModel.HasUnsavedChanges |= _tile.MaterialName != value;
			_tile.MaterialName = value;
		}

		private static ValidationResult? ValidateNameUnique(string name, ValidationContext context)
		{
			var instance = (TileViewModel)context.ObjectInstance;
			foreach (var tile in instance._designerViewModel.Tiles)
			{
				if (tile != instance && tile.Name == name)
				{
					throw new ValidationException("Tile name must be unique.");
				}
			}

			return ValidationResult.Success;
		}

		private static ValidationResult? ValidatePosition((int row, int col) position, ValidationContext context)
		{
			var instance = (TileViewModel)context.ObjectInstance;
			var size = instance._designerViewModel.TileSetSize;
			if (position.row < 0 || position.row > size.height || position.col < 0 || position.col > size.width)
			{
				throw new ValidationException("Tile position must be within the bounds of the tile set.");
			}
			return ValidationResult.Success;
		}

		private static ValidationResult? ValidateMaterialName(string materialName, ValidationContext context)
		{
			var instance = (TileViewModel)context.ObjectInstance;
			// TODO complete this validation
			return ValidationResult.Success;
		}

		private static ValidationResult? ValidateAllignment(TileAlignment alignment, ValidationContext context)
		{
			var instance = (TileViewModel)context.ObjectInstance;
			// TODO complete this validation
			return ValidationResult.Success;
		}
	}
}
