using CommunityToolkit.Mvvm.ComponentModel;
using InkscapeTileMaker.Models;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace InkscapeTileMaker.ViewModels
{
	public partial class TileViewModel : ObservableObject
	{
		private readonly Tile _tile;
		private readonly XElement _element;
		private readonly XElement _collection;
		private readonly DesignerViewModel _designerViewModel;

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

		public TileViewModel(XElement tileElement, XElement collectionElement, DesignerViewModel designerViewModel)
		{
			var serializer = new XmlSerializer(typeof(Tile));
			using var reader = tileElement.CreateReader();
			_tile = (Tile)serializer.Deserialize(reader)!;
			_collection = collectionElement;
			_element = tileElement;
			_designerViewModel = designerViewModel;

			Name = _tile.Name;
			Position = (_tile.Row, _tile.Column);
			Type = _tile.Type;
			Allignment = _tile.Allignment;
			MaterialName = _tile.MaterialName;
		}

		public void Sync()
		{
			_element.Remove();

			var serializer = new XmlSerializer(typeof(Tile));
			XElement tileElement;

			using (var writer = new StringWriter())
			{
				serializer.Serialize(writer, _tile);
				tileElement = XElement.Parse(writer.ToString());
			}

			_collection.Add(tileElement);
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
