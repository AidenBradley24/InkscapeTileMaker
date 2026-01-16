using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace InkscapeTileMaker.Converters
{
    public sealed class DecimalToPercentageConverter : IValueConverter
    {
        // value: decimal/double like 0.25
        // parameter (optional): number of decimal places, e.g. "1"
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is null)
                return string.Empty;

            if (!TryToDouble(value, out var number))
                return BindableProperty.UnsetValue;

            var decimals = 0;
            if (parameter is string p && int.TryParse(p, out var parsedDecimals))
                decimals = parsedDecimals;

            var percent = number * 100;
            var format = "F" + decimals; // e.g. F0, F1, F2
            return percent.ToString(format, culture) + "%";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // Expect something like "25" or "25%" and return 0.25
            if (value is not string s || string.IsNullOrWhiteSpace(s))
                return 0m;

            s = s.Trim().TrimEnd('%');

            if (double.TryParse(s, NumberStyles.Float, culture, out var percent))
                return percent / 100.0;

            return 0m;
        }

        private static bool TryToDouble(object value, out double result)
        {
            switch (value)
            {
                case double d:
                    result = d;
                    return true;
                case float f:
                    result = f;
                    return true;
                case decimal m:
                    result = (double)m;
                    return true;
                case int i:
                    result = i;
                    return true;
                case long l:
                    result = l;
                    return true;
                case string s when double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed):
                    result = parsed;
                    return true;
                default:
                    result = 0;
                    return false;
            }
        }
    }
}