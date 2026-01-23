namespace Scrapile.Domain.Interfaces;

using Scrapile.Domain.Entities;

/// <summary>
/// Interface for application settings storage.
/// Settings are user preferences that persist across sessions.
/// </summary>
public interface ISettingsStore
{
    /// <summary>
    /// Loads settings from storage.
    /// Creates default settings if none exist.
    /// </summary>
    /// <returns>The loaded or default settings.</returns>
    Task<AppSettings> LoadAsync();

    /// <summary>
    /// Saves settings to storage.
    /// </summary>
    /// <param name="settings">The settings to save.</param>
    Task SaveAsync(AppSettings settings);

    /// <summary>
    /// Gets the path to the settings file.
    /// </summary>
    string SettingsFilePath { get; }
}
