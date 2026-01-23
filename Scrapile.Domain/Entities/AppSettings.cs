namespace Scrapile.Domain.Entities;

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
    public string TabPosition { get; set; } = "Left";

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
    /// Theme preference.
    /// Valid values: "Light", "Dark", "System".
    /// </summary>
    public string Theme { get; set; } = "System";

    /// <summary>
    /// Auto-save delay in milliseconds.
    /// Content is saved after this delay following the last keystroke.
    /// </summary>
    public int AutoSaveDelayMs { get; set; } = 500;

    /// <summary>
    /// Creates a default settings object with sensible defaults.
    /// </summary>
    public static AppSettings CreateDefault()
    {
        return new AppSettings
        {
            Version = "1.0",
            StorageDirectory = null,
            TabPosition = "Left",
            FontFamily = null,
            FontSize = 14,
            Theme = "System",
            AutoSaveDelayMs = 500
        };
    }

    /// <summary>
    /// Validates and fixes any invalid settings values.
    /// </summary>
    public void Validate()
    {
        // Validate TabPosition
        if (TabPosition != "Left" && TabPosition != "Right")
        {
            TabPosition = "Left";
        }

        // Validate FontSize (reasonable range: 8-72)
        if (FontSize < 8)
        {
            FontSize = 8;
        }
        else if (FontSize > 72)
        {
            FontSize = 72;
        }

        // Validate Theme
        if (Theme != "Light" && Theme != "Dark" && Theme != "System")
        {
            Theme = "System";
        }

        // Validate AutoSaveDelayMs (reasonable range: 100-5000)
        if (AutoSaveDelayMs < 100)
        {
            AutoSaveDelayMs = 100;
        }
        else if (AutoSaveDelayMs > 5000)
        {
            AutoSaveDelayMs = 5000;
        }
    }
}
