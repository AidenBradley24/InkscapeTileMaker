using System.Globalization;

namespace InkscapeTileMaker.Converters
{
	public class CurrentIsSelectedConverter : IMultiValueConverter
	{
		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			if (values.Length != 2)
				return false;
			var current = values[0];
			var selected = values[1];
			if (current is null || selected is null)
				return false;
			return Equals(current, selected);
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}
