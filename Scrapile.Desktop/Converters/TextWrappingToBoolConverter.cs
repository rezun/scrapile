namespace Scrapile.Desktop.Converters;

using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

/// <summary>
/// Converts Avalonia TextWrapping enum to bool for AvaloniaEdit's WordWrap property.
/// TextWrapping.Wrap -> true, TextWrapping.NoWrap -> false
/// </summary>
public class TextWrappingToBoolConverter : IValueConverter
{
    public static readonly TextWrappingToBoolConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is TextWrapping wrapping && wrapping == TextWrapping.Wrap;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? TextWrapping.Wrap : TextWrapping.NoWrap;
    }
}
