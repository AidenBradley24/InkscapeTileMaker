using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InkscapeTileMaker.Models;

namespace InkscapeTileMaker.ViewModels
{
	public partial class TilemapViewModel : ObservableObject
	{
		private readonly MaterialTilemap _tilemap;

		public MaterialTilemap Tilemap => _tilemap;

		public int Width => _tilemap.Width;
		public int Height => _tilemap.Height;


		public TilemapViewModel(int width, int height)
		{
			_tilemap = new MaterialTilemap(width, height);
		}

		[RelayCommand]
		public void AddSampleMaterial(Material material)
		{
			if (Width < 8 || Height < 8) throw new Exception("Tilemap too small for sample material.");

			(int x, int y)[] positions =
			[
				(0, 0), (2, 0),
				(0, 1), (1, 1), (2, 1), (3, 1), (5, 1), (6, 1), (7, 1),
				(2, 2), (3, 2), (6, 2),
				(6, 3),
				(2, 4), (3, 4),
				(2, 5), (3, 5)
			];

			_tilemap.Paint(material, positions);
		}

		[RelayCommand]
		public void Clear()
		{
			_tilemap.Clear();
		}
	}
}
