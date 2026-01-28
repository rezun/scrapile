namespace Scrapile.Infrastructure.Storage;

using System.Text.Json;
using System.Text.Json.Serialization;
using Scrapile.Domain.Entities;
using Scrapile.Domain.Interfaces;

/// <summary>
/// JSON file-based implementation of ISettingsStore.
/// Settings are stored in a platform-specific configuration directory.
/// </summary>
public class JsonSettingsStore : ISettingsStore
{
    private const string SettingsFilename = "settings.json";
    private const string BackupExtension = ".backup";

    private readonly string _settingsDirectory;
    private readonly string _settingsFilePath;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly SemaphoreSlim _lock = new(1, 1);

    // In-memory cache to reduce disk I/O
    private AppSettings? _cachedSettings;

    /// <inheritdoc />
    public string SettingsFilePath => _settingsFilePath;

    /// <inheritdoc />
    public bool SettingsFileExists() => File.Exists(_settingsFilePath);

    /// <summary>
    /// Creates a new JsonSettingsStore using the platform-specific settings directory.
    /// </summary>
    public JsonSettingsStore() : this(GetPlatformSettingsDirectory())
    {
    }

    /// <summary>
    /// Creates a new JsonSettingsStore with a custom settings directory.
    /// </summary>
    /// <param name="settingsDirectory">Directory where the settings file will be stored.</param>
    public JsonSettingsStore(string settingsDirectory)
    {
        if (string.IsNullOrWhiteSpace(settingsDirectory))
        {
            throw new ArgumentException("Settings directory cannot be null or empty.", nameof(settingsDirectory));
        }

        _settingsDirectory = settingsDirectory;
        _settingsFilePath = Path.Combine(_settingsDirectory, SettingsFilename);

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        // Ensure settings directory exists
        Directory.CreateDirectory(_settingsDirectory);
    }

    /// <summary>
    /// Gets the platform-specific settings directory.
    /// Windows: %APPDATA%/Scrapile
    /// macOS/Linux: ~/.config/Scrapile
    /// </summary>
    public static string GetPlatformSettingsDirectory()
    {
        if (OperatingSystem.IsWindows())
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "Scrapile");
        }
        else
        {
            // macOS and Linux use ~/.config/Scrapile
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, ".config", "Scrapile");
        }
    }

    /// <inheritdoc />
    public async Task<AppSettings> LoadAsync()
    {
        await _lock.WaitAsync();
        try
        {
            // Return cached if available
            if (_cachedSettings != null)
            {
                return _cachedSettings;
            }

            _cachedSettings = await LoadFromFileAsync();
            return _cachedSettings;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task SaveAsync(AppSettings settings)
    {
        if (settings == null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        // Validate before saving
        settings.Validate();

        await _lock.WaitAsync();
        try
        {
            await SaveToFileAsync(settings);
            _cachedSettings = settings;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Loads settings from the JSON file.
    /// Returns default settings if file doesn't exist or is corrupted.
    /// </summary>
    private async Task<AppSettings> LoadFromFileAsync()
    {
        if (!File.Exists(_settingsFilePath))
        {
            return AppSettings.CreateDefault();
        }

        try
        {
            var json = await File.ReadAllTextAsync(_settingsFilePath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions);

            if (settings != null)
            {
                // Validate loaded settings
                settings.Validate();
                return settings;
            }
        }
        catch (JsonException)
        {
            // JSON is corrupted, try to recover from backup
            var backupPath = _settingsFilePath + BackupExtension;
            if (File.Exists(backupPath))
            {
                try
                {
                    var backupJson = await File.ReadAllTextAsync(backupPath);
                    var backupSettings = JsonSerializer.Deserialize<AppSettings>(backupJson, _jsonOptions);

                    if (backupSettings != null)
                    {
                        // Restore from backup
                        backupSettings.Validate();
                        await SaveToFileAsync(backupSettings);
                        return backupSettings;
                    }
                }
                catch
                {
                    // Backup also corrupted, fall through to default
                }
            }
        }
        catch (IOException)
        {
            // File may be inaccessible, return default
        }

        return AppSettings.CreateDefault();
    }

    /// <summary>
    /// Saves settings to the JSON file using atomic write.
    /// Creates a backup before writing.
    /// </summary>
    private async Task SaveToFileAsync(AppSettings settings)
    {
        var json = JsonSerializer.Serialize(settings, _jsonOptions);

        // Create backup of existing file if it exists
        if (File.Exists(_settingsFilePath))
        {
            var backupPath = _settingsFilePath + BackupExtension;
            try
            {
                File.Copy(_settingsFilePath, backupPath, overwrite: true);
            }
            catch
            {
                // Best effort backup, continue even if backup fails
            }
        }

        // Atomic write: write to temp file, then rename
        var tempPath = Path.Combine(_settingsDirectory, $".tmp_{Guid.NewGuid():N}.json");

        try
        {
            await File.WriteAllTextAsync(tempPath, json);
            File.Move(tempPath, _settingsFilePath, overwrite: true);
        }
        finally
        {
            // Clean up temp file if it still exists
            if (File.Exists(tempPath))
            {
                try
                {
                    File.Delete(tempPath);
                }
                catch
                {
                    // Best effort cleanup
                }
            }
        }
    }
}
