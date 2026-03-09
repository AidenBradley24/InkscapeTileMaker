using System.Globalization;
using System.Text.RegularExpressions;

namespace InkscapeTileMaker.Converters
{
	public partial class EnumToStringConverter : IValueConverter
	{
		public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			var enumType = value?.GetType() ?? parameter as Type;
			if (enumType == null || !enumType.IsEnum || value == null)
				return value?.ToString() ?? "unknown";

			var rawName = Enum.GetName(enumType, value) ?? "unknown";

			// Insert spaces between words and replace underscores
			var spaced = _enumWordRegex.Replace(rawName, "$1 $2");
			spaced = spaced.Replace("_", " ");

			// Capitalize each word
			return CapitalizeWords(spaced, culture);
		}

		public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> throw new NotSupportedException();

		private static string CapitalizeWords(string input, CultureInfo culture)
		{
			if (string.IsNullOrWhiteSpace(input))
				return input ?? string.Empty;

			var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);

			for (var i = 0; i < parts.Length; i++)
			{
				var word = parts[i];
				if (word.Length == 0)
					continue;

				var first = char.ToUpper(word[0], culture);
				parts[i] = word.Length == 1
					? first.ToString(culture)
					: first + word[1..];
			}

			return string.Join(' ', parts);
		}

		private static readonly Regex _enumWordRegex = MyRegex();

		[GeneratedRegex(@"([a-z0-9])([A-Z])", RegexOptions.Compiled)]
		private static partial Regex MyRegex();
	}
}