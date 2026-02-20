using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InkscapeTileMaker.Models;

namespace InkscapeTileMaker.ViewModels
{
	public partial class TilemapViewModel : ObservableObject
	{
		private readonly CompositeTilemap _compositeTilemap;
		private readonly Tilemap _tilemap;
		private readonly DuelGridMaterialTilemap _duelGridMaterialTilemap;

		public event Action NeedsRedraw = delegate { };

		public CompositeTilemap Composite => _compositeTilemap;
		public Tilemap Regular => _tilemap;
		public DuelGridMaterialTilemap DuelGridMaterial => _duelGridMaterialTilemap;

		public Models.Rect Rect => _compositeTilemap.TileGridRect;

		public TilemapViewModel(int width, int height)
		{
			_tilemap = new Tilemap(width, height);
			_duelGridMaterialTilemap = new DuelGridMaterialTilemap(width, height);
			_compositeTilemap = new CompositeTilemap(_duelGridMaterialTilemap, _tilemap);
		}

		[RelayCommand]
		public void AddSampleDuelGridMaterial(Material material)
		{
			if (DuelGridMaterial.Width < 8 || DuelGridMaterial.Height < 8) throw new Exception("Tilemap too small for sample material.");

			(int x, int y)[] positions =
			[
				(0, 0), (2, 0),
				(0, 1), (1, 1), (2, 1), (3, 1), (5, 1), (6, 1), (7, 1),
				(2, 2), (3, 2), (6, 2),
				(6, 3),
				(2, 4), (3, 4),
				(2, 5), (3, 5)
			];

			_duelGridMaterialTilemap.Paint(material, positions);
			NeedsRedraw.Invoke();
		}

		[RelayCommand]
		public void Clear()
		{
			_tilemap.Clear();
			_duelGridMaterialTilemap.Clear();
			NeedsRedraw.Invoke();
		}
	}
}
