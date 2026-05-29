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
    public const string AlwaysShowLineNumbers = "AlwaysShowLineNumbers";
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

/// <summary>
/// Numeric limits for font size settings.
/// </summary>
public static class FontSizeLimits
{
    /// <summary>
    /// Minimum readable font size in points.
    /// </summary>
    public const int Min = 8;

    /// <summary>
    /// Maximum practical font size in points.
    /// </summary>
    public const int Max = 72;

    /// <summary>
    /// Checks if the given font size is within valid bounds.
    /// </summary>
    public static bool IsValid(int fontSize) =>
        fontSize >= Min && fontSize <= Max;
}

/// <summary>
/// Numeric limits for auto-save delay settings.
/// </summary>
public static class AutoSaveDelayLimits
{
    /// <summary>
    /// Minimum auto-save delay in milliseconds.
    /// Lower values would cause excessive I/O.
    /// </summary>
    public const int MinMs = 100;

    /// <summary>
    /// Maximum auto-save delay in milliseconds.
    /// Higher values risk losing too much work on crash.
    /// </summary>
    public const int MaxMs = 5000;

    /// <summary>
    /// Default auto-save delay in milliseconds.
    /// </summary>
    public const int DefaultMs = 500;

    /// <summary>
    /// Checks if the given delay is within valid bounds.
    /// </summary>
    public static bool IsValid(int delayMs) =>
        delayMs >= MinMs && delayMs <= MaxMs;
}

/// <summary>
/// Timing constants for UI feedback.
/// </summary>
public static class UiTimingConstants
{
    /// <summary>
    /// Duration to display "Saved" status message in milliseconds
    /// before clearing it automatically.
    /// </summary>
    public const int SaveStatusDisplayDurationMs = 1500;
}

/// <summary>
/// Limits for metadata storage.
/// </summary>
public static class MetadataLimits
{
    /// <summary>
    /// Maximum number of recently closed items to keep in history.
    /// Oldest items are removed when this limit is exceeded.
    /// </summary>
    public const int MaxRecentlyClosedItems = 50;
}
