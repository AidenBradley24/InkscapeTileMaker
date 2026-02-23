using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InkscapeTileMaker.Models;

namespace InkscapeTileMaker.ViewModels
{
	public partial class TilemapViewModel : ObservableObject
	{
		private readonly CompositeTilemap _compositeTilemap;
		private readonly Tilemap _tilemap;
		private readonly DualGridMaterialTilemap _dualGridMaterialTilemap;

		public event Action NeedsRedraw = delegate { };

		public CompositeTilemap Composite => _compositeTilemap;
		public Tilemap Regular => _tilemap;
		public DualGridMaterialTilemap DualGridMaterial => _dualGridMaterialTilemap;

		public Models.Rect Rect => _compositeTilemap.TileGridRect;

		public TilemapViewModel(int width, int height)
		{
			_tilemap = new Tilemap(width, height);
			_dualGridMaterialTilemap = new DualGridMaterialTilemap(width, height);
			_compositeTilemap = new CompositeTilemap(_dualGridMaterialTilemap, _tilemap);
		}

		[RelayCommand]
		public void AddSampleDualGridMaterial(Material material)
		{
			if (DualGridMaterial.Width < 8 || DualGridMaterial.Height < 8) throw new Exception("Tilemap too small for sample material.");

			(int x, int y)[] positions =
			[
				(0, 0), (2, 0),
				(0, 1), (1, 1), (2, 1), (3, 1), (5, 1), (6, 1), (7, 1),
				(2, 2), (3, 2), (6, 2),
				(6, 3),
				(2, 4), (3, 4),
				(2, 5), (3, 5)
			];

			_dualGridMaterialTilemap.Paint(material, positions);
			NeedsRedraw.Invoke();
		}

		[RelayCommand]
		public void Clear()
		{
			_tilemap.Clear();
			_dualGridMaterialTilemap.Clear();
			NeedsRedraw.Invoke();
		}
	}
}
