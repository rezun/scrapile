namespace Scrapile.Domain.Entities;

using Scrapile.Domain.Constants;

/// <summary>
/// Application settings that persist across sessions.
/// Contains user preferences that rarely change.
/// </summary>
public class AppSettings
{
    /// <summary>
    /// Settings file format version.
    /// </summary>
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// Directory where documents are stored.
    /// Null means use the default platform-specific location.
    /// </summary>
    public string? StorageDirectory { get; set; }

    /// <summary>
    /// Position of the tab list panel.
    /// Valid values: "Left" or "Right".
    /// </summary>
    public string TabPosition { get; set; } = TabPositionValues.Left;

    /// <summary>
    /// Font family for the editor.
    /// Null means use the default monospace font.
    /// </summary>
    public string? FontFamily { get; set; }

    /// <summary>
    /// Font size for the editor in points.
    /// </summary>
    public int FontSize { get; set; } = 14;

    /// <summary>
    /// Word wrap setting for the editor.
    /// Valid values: "Wrap", "NoWrap".
    /// </summary>
    public string WordWrap { get; set; } = WordWrapValues.Wrap;

    /// <summary>
    /// Theme preference.
    /// Valid values: "Light", "Dark", "System".
    /// </summary>
    public string Theme { get; set; } = ThemeValues.System;

    /// <summary>
    /// Auto-save delay in milliseconds.
    /// Content is saved after this delay following the last keystroke.
    /// </summary>
    public int AutoSaveDelayMs { get; set; } = AutoSaveDelayLimits.DefaultMs;

    /// <summary>
    /// Whether to start the application automatically when the user logs in.
    /// </summary>
    public bool AutorunAtStartup { get; set; } = false;

    /// <summary>
    /// Global keyboard shortcut to show/hide the window.
    /// Format: "Ctrl+Alt+S" or "Cmd+Shift+Space" etc.
    /// Null means no shortcut is configured.
    /// </summary>
    public string? GlobalShortcut { get; set; }

    /// <summary>
    /// Whether closing the window minimizes to system tray instead of quitting.
    /// </summary>
    public bool MinimizeToTray { get; set; } = true;

    /// <summary>
    /// Whether to always show line numbers in the editor.
    /// When false, line numbers are only shown when syntax highlighting is enabled.
    /// </summary>
    public bool AlwaysShowLineNumbers { get; set; } = false;

    /// <summary>
    /// Creates a default settings object with sensible defaults.
    /// </summary>
    public static AppSettings CreateDefault()
    {
        return new AppSettings
        {
            Version = "1.0",
            StorageDirectory = null,
            TabPosition = TabPositionValues.Left,
            FontFamily = null,
            FontSize = 14,
            WordWrap = WordWrapValues.Wrap,
            Theme = ThemeValues.System,
            AutoSaveDelayMs = AutoSaveDelayLimits.DefaultMs,
            AutorunAtStartup = false,
            GlobalShortcut = null,
            MinimizeToTray = true,
            AlwaysShowLineNumbers = false
        };
    }

    /// <summary>
    /// Validates and fixes any invalid settings values.
    /// </summary>
    public void Validate()
    {
        // Validate TabPosition
        if (!TabPositionValues.IsValid(TabPosition))
        {
            TabPosition = TabPositionValues.Left;
        }

        // Validate FontSize
        if (FontSize < FontSizeLimits.Min)
        {
            FontSize = FontSizeLimits.Min;
        }
        else if (FontSize > FontSizeLimits.Max)
        {
            FontSize = FontSizeLimits.Max;
        }

        // Validate Theme
        if (!ThemeValues.IsValid(Theme))
        {
            Theme = ThemeValues.System;
        }

        // Validate WordWrap
        if (!WordWrapValues.IsValid(WordWrap))
        {
            WordWrap = WordWrapValues.Wrap;
        }

        // Validate AutoSaveDelayMs
        if (AutoSaveDelayMs < AutoSaveDelayLimits.MinMs)
        {
            AutoSaveDelayMs = AutoSaveDelayLimits.MinMs;
        }
        else if (AutoSaveDelayMs > AutoSaveDelayLimits.MaxMs)
        {
            AutoSaveDelayMs = AutoSaveDelayLimits.MaxMs;
        }
    }
}
