namespace Scrapile.Desktop.ViewModels;

using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

/// <summary>
/// Static converter instances for use with x:Static binding.
/// </summary>
public static class Converters
{
    /// <summary>
    /// Converts a boolean to a chevron path data (down when expanded, right when collapsed).
    /// </summary>
    public static readonly IValueConverter BoolToChevronPath = new FuncValueConverter<bool, StreamGeometry>(isExpanded =>
    {
        // Down chevron when expanded, right chevron when collapsed
        var pathData = isExpanded
            ? "M7.41,8.58L12,13.17L16.59,8.58L18,10L12,16L6,10L7.41,8.58Z"  // Down
            : "M8.59,16.58L13.17,12L8.59,7.41L10,6L16,12L10,18L8.59,16.58Z"; // Right
        return StreamGeometry.Parse(pathData);
    });

    /// <summary>
    /// Converts IsTabListOnLeft to the tab list's Grid.Column value.
    /// Returns 0 when true (left), 2 when false (right).
    /// </summary>
    public static readonly IValueConverter BoolToTabListColumn = new FuncValueConverter<bool, int>(isLeft => isLeft ? 0 : 2);

    /// <summary>
    /// Converts IsTabListOnLeft to the editor's Grid.Column value.
    /// Returns 2 when true (tabs on left), 0 when false (tabs on right).
    /// </summary>
    public static readonly IValueConverter BoolToEditorColumn = new FuncValueConverter<bool, int>(isLeft => isLeft ? 2 : 0);
}

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
