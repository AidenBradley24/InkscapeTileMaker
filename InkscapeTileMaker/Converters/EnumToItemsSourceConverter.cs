using System.Globalization;

namespace InkscapeTileMaker.Converters
{
	public class EnumToItemsSourceConverter : IValueConverter
	{
		public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			var enumType = value?.GetType() ?? parameter as Type;
			if (enumType == null || !enumType.IsEnum)
				return Array.Empty<object>();

			return Enum.GetValues(enumType).Cast<object>().ToList();
		}

		public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> throw new NotSupportedException();
	}
}