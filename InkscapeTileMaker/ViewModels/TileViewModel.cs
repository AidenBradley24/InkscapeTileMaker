using CommunityToolkit.Mvvm.ComponentModel;
using InkscapeTileMaker.Models;

namespace InkscapeTileMaker.ViewModels
{
	public partial class TileViewModel : ObservableObject
	{
		private readonly Tile _tile;
		private readonly DesignerViewModel _designerViewModel;
		private readonly Action<Tile> _syncFunction;

		[ObservableProperty]
		public partial ImageSource? PreviewImage { get; set; }

		[ObservableProperty]
		public partial string Name { get; set; }

		[ObservableProperty]
		public partial (int row, int col) Position { get; set; }

		[ObservableProperty]
		public partial TileType Type { get; set; }

		[ObservableProperty]
		public partial TileAlignment Allignment { get; set; }

		[ObservableProperty]
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
	}
}
