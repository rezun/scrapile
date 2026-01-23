namespace Scrapile.Desktop.ViewModels;

using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Scrapile.Application.Services;
using Scrapile.Desktop.Services;

/// <summary>
/// ViewModel for the settings dialog.
/// </summary>
public partial class SettingsViewModel : ViewModelBase
{
    private readonly SettingsService _settingsService;
    private readonly ThemeService _themeService;
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
        "System",
        "Light",
        "Dark"
    };

    /// <summary>
    /// Available tab positions.
    /// </summary>
    public ObservableCollection<string> TabPositions { get; } = new()
    {
        "Left",
        "Right"
    };

    [ObservableProperty]
    private string? _storageDirectory;

    [ObservableProperty]
    private string _selectedTabPosition = "Left";

    [ObservableProperty]
    private string _selectedFontFamily = "Default";

    [ObservableProperty]
    private int _fontSize = 14;

    [ObservableProperty]
    private string _selectedTheme = "System";

    [ObservableProperty]
    private int _autoSaveDelayMs = 500;

    [ObservableProperty]
    private string _settingsFilePath = string.Empty;

    /// <summary>
    /// Event raised when the dialog should close.
    /// </summary>
    public event EventHandler? CloseRequested;

    /// <summary>
    /// Event raised when a folder browser should be shown.
    /// </summary>
    public event EventHandler<FolderBrowserEventArgs>? FolderBrowserRequested;

    public SettingsViewModel(SettingsService settingsService, ThemeService themeService)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
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
            SelectedTheme = settings.Theme;
            AutoSaveDelayMs = settings.AutoSaveDelayMs;
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

    partial void OnSelectedThemeChanged(string value)
    {
        if (_isInitializing) return;
        _ = ApplyThemeAsync(value);
    }

    partial void OnAutoSaveDelayMsChanged(int value)
    {
        if (_isInitializing) return;
        if (value >= 100 && value <= 5000)
        {
            _ = _settingsService.SetAutoSaveDelayMsAsync(value);
        }
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

    [RelayCommand]
    private void ClearStorageDirectory()
    {
        _ = SetStorageDirectoryAsync(null);
    }

    [RelayCommand]
    private async Task ResetToDefaults()
    {
        await _settingsService.ResetToDefaultsAsync();
        await _themeService.SetThemeAsync("System");
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
