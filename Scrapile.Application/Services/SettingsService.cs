namespace Scrapile.Application.Services;

using Scrapile.Domain.Entities;
using Scrapile.Domain.Interfaces;

/// <summary>
/// Service for managing application settings.
/// Provides typed access to settings with change notifications.
/// </summary>
public class SettingsService
{
    private readonly ISettingsStore _settingsStore;
    private AppSettings _currentSettings;

    /// <summary>
    /// Event raised when any setting changes.
    /// </summary>
    public event EventHandler<SettingsChangedEventArgs>? SettingsChanged;

    /// <summary>
    /// Gets the current settings.
    /// </summary>
    public AppSettings CurrentSettings => _currentSettings;

    /// <summary>
    /// Gets the path to the settings file.
    /// </summary>
    public string SettingsFilePath => _settingsStore.SettingsFilePath;

    public SettingsService(ISettingsStore settingsStore)
    {
        _settingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));
        _currentSettings = AppSettings.CreateDefault();
    }

    /// <summary>
    /// Initializes the settings service by loading saved settings.
    /// </summary>
    public async Task InitializeAsync()
    {
        _currentSettings = await _settingsStore.LoadAsync();

        // Notify listeners that settings have been loaded
        SettingsChanged?.Invoke(this, new SettingsChangedEventArgs("All"));
    }

    /// <summary>
    /// Gets the storage directory setting, or the default if not set.
    /// </summary>
    public string GetStorageDirectory(string defaultDirectory)
    {
        return string.IsNullOrWhiteSpace(_currentSettings.StorageDirectory)
            ? defaultDirectory
            : _currentSettings.StorageDirectory;
    }

    /// <summary>
    /// Sets the storage directory.
    /// </summary>
    public async Task SetStorageDirectoryAsync(string? directory)
    {
        var normalized = string.IsNullOrWhiteSpace(directory) ? null : directory.Trim();
        if (_currentSettings.StorageDirectory == normalized)
        {
            return;
        }

        _currentSettings.StorageDirectory = normalized;
        await SaveAndNotifyAsync("StorageDirectory");
    }

    /// <summary>
    /// Gets the tab position setting.
    /// </summary>
    public string GetTabPosition()
    {
        return _currentSettings.TabPosition;
    }

    /// <summary>
    /// Sets the tab position.
    /// </summary>
    public async Task SetTabPositionAsync(string position)
    {
        if (position != "Left" && position != "Right")
        {
            throw new ArgumentException("Tab position must be 'Left' or 'Right'.", nameof(position));
        }

        if (_currentSettings.TabPosition == position)
        {
            return;
        }

        _currentSettings.TabPosition = position;
        await SaveAndNotifyAsync("TabPosition");
    }

    /// <summary>
    /// Gets the font family setting.
    /// </summary>
    public string? GetFontFamily()
    {
        return _currentSettings.FontFamily;
    }

    /// <summary>
    /// Sets the font family.
    /// </summary>
    public async Task SetFontFamilyAsync(string? fontFamily)
    {
        var normalized = string.IsNullOrWhiteSpace(fontFamily) ? null : fontFamily.Trim();
        if (_currentSettings.FontFamily == normalized)
        {
            return;
        }

        _currentSettings.FontFamily = normalized;
        await SaveAndNotifyAsync("FontFamily");
    }

    /// <summary>
    /// Gets the font size setting.
    /// </summary>
    public int GetFontSize()
    {
        return _currentSettings.FontSize;
    }

    /// <summary>
    /// Sets the font size.
    /// </summary>
    public async Task SetFontSizeAsync(int fontSize)
    {
        if (fontSize < 8 || fontSize > 72)
        {
            throw new ArgumentOutOfRangeException(nameof(fontSize), "Font size must be between 8 and 72.");
        }

        if (_currentSettings.FontSize == fontSize)
        {
            return;
        }

        _currentSettings.FontSize = fontSize;
        await SaveAndNotifyAsync("FontSize");
    }

    /// <summary>
    /// Gets the theme setting.
    /// </summary>
    public string GetTheme()
    {
        return _currentSettings.Theme;
    }

    /// <summary>
    /// Sets the theme.
    /// </summary>
    public async Task SetThemeAsync(string theme)
    {
        if (theme != "Light" && theme != "Dark" && theme != "System")
        {
            throw new ArgumentException("Theme must be 'Light', 'Dark', or 'System'.", nameof(theme));
        }

        if (_currentSettings.Theme == theme)
        {
            return;
        }

        _currentSettings.Theme = theme;
        await SaveAndNotifyAsync("Theme");
    }

    /// <summary>
    /// Gets the word wrap setting.
    /// </summary>
    public string GetWordWrap()
    {
        return _currentSettings.WordWrap;
    }

    /// <summary>
    /// Sets the word wrap setting.
    /// </summary>
    public async Task SetWordWrapAsync(string wordWrap)
    {
        if (wordWrap != "Wrap" && wordWrap != "NoWrap")
        {
            throw new ArgumentException("Word wrap must be 'Wrap' or 'NoWrap'.", nameof(wordWrap));
        }

        if (_currentSettings.WordWrap == wordWrap)
        {
            return;
        }

        _currentSettings.WordWrap = wordWrap;
        await SaveAndNotifyAsync("WordWrap");
    }

    /// <summary>
    /// Gets the auto-save delay setting in milliseconds.
    /// </summary>
    public int GetAutoSaveDelayMs()
    {
        return _currentSettings.AutoSaveDelayMs;
    }

    /// <summary>
    /// Sets the auto-save delay in milliseconds.
    /// </summary>
    public async Task SetAutoSaveDelayMsAsync(int delayMs)
    {
        if (delayMs < 100 || delayMs > 5000)
        {
            throw new ArgumentOutOfRangeException(nameof(delayMs), "Auto-save delay must be between 100 and 5000 ms.");
        }

        if (_currentSettings.AutoSaveDelayMs == delayMs)
        {
            return;
        }

        _currentSettings.AutoSaveDelayMs = delayMs;
        await SaveAndNotifyAsync("AutoSaveDelayMs");
    }

    /// <summary>
    /// Gets the autorun at startup setting.
    /// </summary>
    public bool GetAutorunAtStartup()
    {
        return _currentSettings.AutorunAtStartup;
    }

    /// <summary>
    /// Sets whether to automatically run at startup.
    /// </summary>
    public async Task SetAutorunAtStartupAsync(bool enabled)
    {
        if (_currentSettings.AutorunAtStartup == enabled)
        {
            return;
        }

        _currentSettings.AutorunAtStartup = enabled;
        await SaveAndNotifyAsync("AutorunAtStartup");
    }

    /// <summary>
    /// Saves all current settings.
    /// </summary>
    public async Task SaveAsync()
    {
        await _settingsStore.SaveAsync(_currentSettings);
    }

    /// <summary>
    /// Resets all settings to their default values.
    /// </summary>
    public async Task ResetToDefaultsAsync()
    {
        _currentSettings = AppSettings.CreateDefault();
        await _settingsStore.SaveAsync(_currentSettings);
        SettingsChanged?.Invoke(this, new SettingsChangedEventArgs("All"));
    }

    /// <summary>
    /// Saves settings and notifies listeners.
    /// </summary>
    private async Task SaveAndNotifyAsync(string settingName)
    {
        await _settingsStore.SaveAsync(_currentSettings);
        SettingsChanged?.Invoke(this, new SettingsChangedEventArgs(settingName));
    }
}

/// <summary>
/// Event arguments for settings changes.
/// </summary>
public class SettingsChangedEventArgs : EventArgs
{
    /// <summary>
    /// The name of the setting that changed, or "All" if all settings changed.
    /// </summary>
    public string SettingName { get; }

    public SettingsChangedEventArgs(string settingName)
    {
        SettingName = settingName;
    }
}
