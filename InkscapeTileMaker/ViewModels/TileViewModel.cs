using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
		public partial bool IsEditingSelectedTileName { get; set; }

		public Tile Value => _tile;

		public TileViewModel(XElement tileElement, XElement collectionElement, DesignerViewModel designerViewModel)
		{
			var serializer = new XmlSerializer(typeof(Tile));
			using var reader = tileElement.CreateReader();
			_tile = (Tile)serializer.Deserialize(reader)!;
			_collection = collectionElement;
			_element = tileElement;
			_designerViewModel = designerViewModel;

			Name = _tile.Name;
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

		[RelayCommand]
		public void EditSelectedTileName()
		{
			IsEditingSelectedTileName = true;
		}
	}
}
