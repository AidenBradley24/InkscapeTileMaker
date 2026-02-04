namespace InkscapeTileMaker.Models
{
	public static class TileTransformationHelpers
	{
		public static TileTransformation GetTransformationForAlignment(TileAlignment sourceAlignment, TileAlignment targetAlignment)
		{
			if (sourceAlignment == targetAlignment)
				return TileTransformation.None;

			(int x, int y) src = sourceAlignment.GetFaceDirection();
			(int x, int y) dst = targetAlignment.GetFaceDirection();

			// Core cannot be reached from / mapped to directional alignments
			if (src == (0, 0) || dst == (0, 0))
				return TileTransformation.None;

			TileTransformation best = TileTransformation.None;
			bool found = false;

			foreach (var transform in GetAllTransformations())
			{
				var rotated = ApplyTransformation(src, transform);
				if (rotated == dst)
				{
					best = transform;
					found = true;
					break;
				}
			}

			return found ? best : TileTransformation.None;
		}

		private static (int x, int y) ApplyTransformation((int x, int y) v, TileTransformation t)
		{
			var rotation = (int)(t & (TileTransformation)3);
			bool flipH = (t & TileTransformation.FlipHorizontal) != 0;
			bool flipV = (t & TileTransformation.FlipVertical) != 0;

			int x = v.x;
			int y = v.y;

			switch (rotation)
			{
				case 0: // 0°
					break;
				case 1: // 90°: (x, y) -> (y, -x)
					{
						int nx = y;
						int ny = -x;
						x = nx;
						y = ny;
					}
					break;
				case 2: // 180°: (x, y) -> (-x, -y)
					x = -x;
					y = -y;
					break;
				case 3: // 270°: (x, y) -> (-y, x)
					{
						int nx = -y;
						int ny = x;
						x = nx;
						y = ny;
					}
					break;
			}

			if (flipH)
				x = -x;
			if (flipV)
				y = -y;

			return (x, y);
		}

		private static TileTransformation[] GetAllTransformations()
		{
			// All combinations of rotation (0,90,180,270) and flips (none, H, V, HV)
			return new[]
			{
				TileTransformation.Rotate0 | TileTransformation.NoFlip,
				TileTransformation.Rotate0 | TileTransformation.FlipHorizontal,
				TileTransformation.Rotate0 | TileTransformation.FlipVertical,
				TileTransformation.Rotate0 | (TileTransformation.FlipHorizontal | TileTransformation.FlipVertical),

				TileTransformation.Rotate90 | TileTransformation.NoFlip,
				TileTransformation.Rotate90 | TileTransformation.FlipHorizontal,
				TileTransformation.Rotate90 | TileTransformation.FlipVertical,
				TileTransformation.Rotate90 | (TileTransformation.FlipHorizontal | TileTransformation.FlipVertical),

				TileTransformation.Rotate180 | TileTransformation.NoFlip,
				TileTransformation.Rotate180 | TileTransformation.FlipHorizontal,
				TileTransformation.Rotate180 | TileTransformation.FlipVertical,
				TileTransformation.Rotate180 | (TileTransformation.FlipHorizontal | TileTransformation.FlipVertical),

				TileTransformation.Rotate270 | TileTransformation.NoFlip,
				TileTransformation.Rotate270 | TileTransformation.FlipHorizontal,
				TileTransformation.Rotate270 | TileTransformation.FlipVertical,
				TileTransformation.Rotate270 | (TileTransformation.FlipHorizontal | TileTransformation.FlipVertical)
			};
		}
	}
}