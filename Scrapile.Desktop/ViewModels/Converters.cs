namespace Scrapile.Desktop.ViewModels;

using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

/// <summary>
/// Converts a boolean to a FontWeight (true = Bold, false = Normal).
/// Used for tabs with titles to display them in bold.
/// </summary>
public class BoolToFontWeightConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool hasTitle && hasTitle)
        {
            return FontWeight.SemiBold;
        }
        return FontWeight.Normal;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts a boolean to a Brush for selection highlighting.
/// </summary>
public class BoolToBrushConverter : IValueConverter
{
    public IBrush? TrueBrush { get; set; }
    public IBrush? FalseBrush { get; set; }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isSelected && isSelected)
        {
            return TrueBrush;
        }
        return FalseBrush;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
