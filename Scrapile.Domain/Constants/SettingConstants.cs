namespace Scrapile.Domain.Constants;

/// <summary>
/// Constants for setting names used in change notifications.
/// </summary>
public static class SettingNames
{
    public const string All = "All";
    public const string StorageDirectory = "StorageDirectory";
    public const string TabPosition = "TabPosition";
    public const string FontFamily = "FontFamily";
    public const string FontSize = "FontSize";
    public const string Theme = "Theme";
    public const string WordWrap = "WordWrap";
    public const string AutoSaveDelayMs = "AutoSaveDelayMs";
    public const string AutorunAtStartup = "AutorunAtStartup";
    public const string GlobalShortcut = "GlobalShortcut";
    public const string MinimizeToTray = "MinimizeToTray";
}

/// <summary>
/// Valid values for tab position setting.
/// </summary>
public static class TabPositionValues
{
    public const string Left = "Left";
    public const string Right = "Right";

    /// <summary>
    /// Checks if the given value is a valid tab position.
    /// </summary>
    public static bool IsValid(string? value) =>
        value == Left || value == Right;
}

/// <summary>
/// Valid values for theme setting.
/// </summary>
public static class ThemeValues
{
    public const string Light = "Light";
    public const string Dark = "Dark";
    public const string System = "System";

    /// <summary>
    /// Checks if the given value is a valid theme.
    /// </summary>
    public static bool IsValid(string? value) =>
        value == Light || value == Dark || value == System;
}

/// <summary>
/// Valid values for word wrap setting.
/// </summary>
public static class WordWrapValues
{
    public const string Wrap = "Wrap";
    public const string NoWrap = "NoWrap";

    /// <summary>
    /// Checks if the given value is a valid word wrap mode.
    /// </summary>
    public static bool IsValid(string? value) =>
        value == Wrap || value == NoWrap;
}
