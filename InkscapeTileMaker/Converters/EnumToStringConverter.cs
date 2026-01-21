using System.Globalization;
using System.Text.RegularExpressions;

namespace InkscapeTileMaker.Converters
{
    public class EnumToStringConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var enumType = value?.GetType() ?? parameter as Type;
            if (enumType == null || !enumType.IsEnum || value == null)
                return value?.ToString() ?? "unknown";

            var rawName = Enum.GetName(enumType, value) ?? "unknown";
            var spaced = _enumWordRegex.Replace(rawName, "$1 $2");
            spaced = spaced.Replace("_", " ");
            return spaced;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();

        private static readonly Regex _enumWordRegex = new(@"([a-z0-9])([A-Z])", RegexOptions.Compiled);
	}
}