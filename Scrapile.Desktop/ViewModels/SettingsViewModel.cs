namespace Scrapile.Desktop.ViewModels;

using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Scrapile.Application.Services;
using Scrapile.Desktop.Services;
using Scrapile.Domain.Constants;

/// <summary>
/// ViewModel for the settings dialog.
/// </summary>
public partial class SettingsViewModel : ViewModelBase
{
    private readonly SettingsService _settingsService;
    private readonly ThemeService _themeService;
    private readonly AutorunService _autorunService;
    private readonly StorageDirectoryValidator _storageDirectoryValidator;
    private bool _isInitializing;

    /// <summary>
    /// Available font families.
    /// </summary>
    public ObservableCollection<string> FontFamilies { get; } = new()
    {
        "Default",
        "Consolas",
        "Menlo",
        "Monaco",
        "Courier New",
        "Source Code Pro",
        "JetBrains Mono",
        "Fira Code"
    };

    /// <summary>
    /// Available themes.
    /// </summary>
    public ObservableCollection<string> Themes { get; } = new()
    {
        ThemeValues.System,
        ThemeValues.Light,
        ThemeValues.Dark
    };

    /// <summary>
    /// Available tab positions.
    /// </summary>
    public ObservableCollection<string> TabPositions { get; } = new()
    {
        TabPositionValues.Left,
        TabPositionValues.Right
    };

    /// <summary>
    /// Available word wrap options.
    /// </summary>
    public ObservableCollection<string> WordWrapOptions { get; } = new()
    {
        WordWrapValues.Wrap,
        "No Wrap"  // Display label for WordWrapValues.NoWrap
    };

    [ObservableProperty]
    private string? _storageDirectory;

    [ObservableProperty]
    private string _selectedTabPosition = TabPositionValues.Left;

    [ObservableProperty]
    private string _selectedFontFamily = "Default";

    [ObservableProperty]
    private int _fontSize = 14;

    [ObservableProperty]
    private string _selectedTheme = ThemeValues.System;

    [ObservableProperty]
    private int _autoSaveDelayMs = AutoSaveDelayLimits.DefaultMs;

    [ObservableProperty]
    private string _selectedWordWrap = WordWrapValues.Wrap;

    [ObservableProperty]
    private string _settingsFilePath = string.Empty;

    [ObservableProperty]
    private bool _autorunAtStartup;

    [ObservableProperty]
    private bool _alwaysShowLineNumbers;

    /// <summary>
    /// Event raised when the dialog should close.
    /// </summary>
    public event EventHandler? CloseRequested;

    /// <summary>
    /// Event raised when a folder browser should be shown.
    /// </summary>
    public event EventHandler<FolderBrowserEventArgs>? FolderBrowserRequested;

    /// <summary>
    /// Event raised when storage directory validation needs user interaction.
    /// </summary>
    public event EventHandler<StorageDirectoryDialogEventArgs>? StorageDirectoryDialogRequested;

    /// <summary>
    /// Event raised when reset to defaults needs confirmation.
    /// </summary>
    public event EventHandler<ResetConfirmationEventArgs>? ResetConfirmationRequested;

    public SettingsViewModel(SettingsService settingsService, ThemeService themeService, AutorunService autorunService)
        : this(settingsService, themeService, autorunService, new StorageDirectoryValidator())
    {
    }

    public SettingsViewModel(SettingsService settingsService, ThemeService themeService, AutorunService autorunService, StorageDirectoryValidator storageDirectoryValidator)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
        _autorunService = autorunService ?? throw new ArgumentNullException(nameof(autorunService));
        _storageDirectoryValidator = storageDirectoryValidator ?? throw new ArgumentNullException(nameof(storageDirectoryValidator));
    }

    /// <summary>
    /// Initializes the view model with current settings.
    /// </summary>
    public void Initialize()
    {
        _isInitializing = true;
        try
        {
            var settings = _settingsService.CurrentSettings;
            StorageDirectory = settings.StorageDirectory;
            SelectedTabPosition = settings.TabPosition;
            SelectedFontFamily = settings.FontFamily ?? "Default";
            FontSize = settings.FontSize;
            SelectedWordWrap = settings.WordWrap == WordWrapValues.NoWrap ? "No Wrap" : WordWrapValues.Wrap;
            SelectedTheme = settings.Theme;
            AutoSaveDelayMs = settings.AutoSaveDelayMs;
            AutorunAtStartup = settings.AutorunAtStartup;
            AlwaysShowLineNumbers = settings.AlwaysShowLineNumbers;
            SettingsFilePath = _settingsService.SettingsFilePath;
        }
        finally
        {
            _isInitializing = false;
        }
    }

    partial void OnSelectedTabPositionChanged(string value)
    {
        if (_isInitializing) return;
        _ = _settingsService.SetTabPositionAsync(value);
    }

    partial void OnSelectedFontFamilyChanged(string value)
    {
        if (_isInitializing) return;
        var fontFamily = value == "Default" ? null : value;
        _ = _settingsService.SetFontFamilyAsync(fontFamily);
    }

    partial void OnFontSizeChanged(int value)
    {
        if (_isInitializing) return;
        if (value >= 8 && value <= 72)
        {
            _ = _settingsService.SetFontSizeAsync(value);
        }
    }

    partial void OnSelectedWordWrapChanged(string value)
    {
        if (_isInitializing) return;
        var wordWrap = value == "No Wrap" ? WordWrapValues.NoWrap : WordWrapValues.Wrap;
        _ = _settingsService.SetWordWrapAsync(wordWrap);
    }

    partial void OnSelectedThemeChanged(string value)
    {
        if (_isInitializing) return;
        _ = ApplyThemeAsync(value);
    }

    partial void OnAutoSaveDelayMsChanged(int value)
    {
        if (_isInitializing) return;
        if (AutoSaveDelayLimits.IsValid(value))
        {
            _ = _settingsService.SetAutoSaveDelayMsAsync(value);
        }
    }

    partial void OnAutorunAtStartupChanged(bool value)
    {
        if (_isInitializing) return;
        _autorunService.SetAutorunEnabled(value);
        _ = _settingsService.SetAutorunAtStartupAsync(value);
    }

    partial void OnAlwaysShowLineNumbersChanged(bool value)
    {
        if (_isInitializing) return;
        _ = _settingsService.SetAlwaysShowLineNumbersAsync(value);
    }

    private async Task ApplyThemeAsync(string theme)
    {
        await _settingsService.SetThemeAsync(theme);
        await _themeService.SetThemeAsync(theme);
    }

    [RelayCommand]
    private void BrowseStorageDirectory()
    {
        var args = new FolderBrowserEventArgs(StorageDirectory);
        FolderBrowserRequested?.Invoke(this, args);
    }

    /// <summary>
    /// Sets the storage directory from the folder browser.
    /// </summary>
    public async Task SetStorageDirectoryAsync(string? directory)
    {
        StorageDirectory = directory;
        await _settingsService.SetStorageDirectoryAsync(directory);
    }

    /// <summary>
    /// Validates a selected directory and raises the appropriate dialog event.
    /// </summary>
    /// <param name="path">The selected path to validate.</param>
    public void ValidateAndRequestStorageDirectoryChange(string path)
    {
        var validationResult = _storageDirectoryValidator.ValidateDirectory(path);
        var hasExistingData = _storageDirectoryValidator.HasDataToCopy(StorageDirectory);

        var args = new StorageDirectoryDialogEventArgs(path, validationResult, StorageDirectory, hasExistingData);
        StorageDirectoryDialogRequested?.Invoke(this, args);
    }

    /// <summary>
    /// Completes the storage directory change after user confirmation.
    /// </summary>
    /// <param name="newPath">The new storage directory path.</param>
    /// <param name="copyData">Whether to copy data from the current directory.</param>
    public async Task CompleteStorageDirectoryChangeAsync(string newPath, bool copyData)
    {
        if (copyData && !string.IsNullOrEmpty(StorageDirectory))
        {
            await _storageDirectoryValidator.CopyDataAsync(StorageDirectory, newPath);
        }

        await SetStorageDirectoryAsync(newPath);
    }

    [RelayCommand]
    private void ClearStorageDirectory()
    {
        _ = SetStorageDirectoryAsync(null);
    }

    [RelayCommand]
    private void ResetToDefaults()
    {
        ResetConfirmationRequested?.Invoke(this, new ResetConfirmationEventArgs());
    }

    /// <summary>
    /// Performs the actual reset after user confirmation.
    /// </summary>
    public async Task ConfirmResetToDefaultsAsync()
    {
        await _settingsService.ResetToDefaultsAsync();
        await _themeService.SetThemeAsync(ThemeValues.System);
        Initialize();
    }

    [RelayCommand]
    private void Close()
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}

/// <summary>
/// Event args for folder browser requests.
/// </summary>
public class FolderBrowserEventArgs : EventArgs
{
    /// <summary>
    /// The initial directory to show in the browser.
    /// </summary>
    public string? InitialDirectory { get; }

    public FolderBrowserEventArgs(string? initialDirectory)
    {
        InitialDirectory = initialDirectory;
    }
}

/// <summary>
/// Event args for storage directory validation dialog requests.
/// </summary>
public class StorageDirectoryDialogEventArgs : EventArgs
{
    /// <summary>
    /// The path the user selected.
    /// </summary>
    public string SelectedPath { get; }

    /// <summary>
    /// The validation result for the selected path.
    /// </summary>
    public StorageDirectoryValidationResult ValidationResult { get; }

    /// <summary>
    /// The current storage directory (may be null).
    /// </summary>
    public string? CurrentStorageDirectory { get; }

    /// <summary>
    /// Whether there is existing data that could be copied.
    /// </summary>
    public bool HasExistingData { get; }

    public StorageDirectoryDialogEventArgs(
        string selectedPath,
        StorageDirectoryValidationResult validationResult,
        string? currentStorageDirectory,
        bool hasExistingData)
    {
        SelectedPath = selectedPath;
        ValidationResult = validationResult;
        CurrentStorageDirectory = currentStorageDirectory;
        HasExistingData = hasExistingData;
    }
}

/// <summary>
/// Event args for reset confirmation requests.
/// </summary>
public class ResetConfirmationEventArgs : EventArgs
{
}
