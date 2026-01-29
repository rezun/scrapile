namespace Scrapile.Desktop.ViewModels;

using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Scrapile.Desktop.DependencyInjection;
using Scrapile.Desktop.Services;

/// <summary>
/// ViewModel for the first-run welcome window.
/// Allows users to configure their document storage directory before using the app.
/// </summary>
public partial class WelcomeViewModel : ViewModelBase
{
    private readonly StorageDirectoryValidator _validator = new();

    [ObservableProperty]
    private string _storageDirectory;

    [ObservableProperty]
    private bool _showFolderWarning;

    [ObservableProperty]
    private bool _autorunAtStartup = false;

    [ObservableProperty]
    private string? _globalShortcut;

    [ObservableProperty]
    private bool _isRecordingShortcut;

    [ObservableProperty]
    private string? _shortcutConflictMessage;

    /// <summary>
    /// Whether the current platform is macOS (for showing Accessibility permission note).
    /// </summary>
    public bool IsMacOS => OperatingSystem.IsMacOS();

    /// <summary>
    /// Whether macOS Accessibility permission is NOT granted (for showing warning).
    /// </summary>
    public bool NeedsAccessibilityPermission => OperatingSystem.IsMacOS() && !Services.GlobalHotkeyService.HasAccessibilityPermission();

    /// <summary>
    /// Whether running on Wayland (global hotkeys don't work).
    /// </summary>
    public bool IsWayland => Services.GlobalHotkeyService.IsWayland();

    /// <summary>
    /// Opens macOS System Settings to the Accessibility section.
    /// </summary>
    [RelayCommand]
    private void OpenAccessibilitySettings()
    {
        if (OperatingSystem.IsMacOS())
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "open",
                    Arguments = "x-apple.systempreferences:com.apple.preference.security?Privacy_Accessibility",
                    UseShellExecute = false
                });
            }
            catch
            {
                // Fallback to general Security & Privacy
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "open",
                        Arguments = "/System/Library/PreferencePanes/Security.prefPane",
                        UseShellExecute = false
                    });
                }
                catch { }
            }
        }
    }

    /// <summary>
    /// Event raised when the folder browser should be shown.
    /// </summary>
    public event EventHandler<FolderBrowserEventArgs>? FolderBrowserRequested;

    /// <summary>
    /// Event raised when the user clicks Continue.
    /// </summary>
    public event EventHandler? ContinueRequested;

    public WelcomeViewModel()
    {
        _storageDirectory = ServiceCollectionExtensions.GetDefaultStorageDirectory();
        ValidateDirectory();
    }

    /// <summary>
    /// Sets the storage directory from the folder browser.
    /// </summary>
    /// <param name="directory">The selected directory path.</param>
    public void SetStorageDirectory(string directory)
    {
        StorageDirectory = directory;
        ValidateDirectory();
    }

    private void ValidateDirectory()
    {
        var result = _validator.ValidateDirectory(StorageDirectory);
        ShowFolderWarning = result == StorageDirectoryValidationResult.InvalidFolder;
    }

    [RelayCommand]
    private void Browse()
    {
        var args = new FolderBrowserEventArgs(StorageDirectory);
        FolderBrowserRequested?.Invoke(this, args);
    }

    [RelayCommand]
    private void Continue()
    {
        ContinueRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Sets the global shortcut from an external source (e.g., shortcut recorder).
    /// </summary>
    public void SetGlobalShortcut(string? shortcut)
    {
        GlobalShortcut = shortcut;
        ShortcutConflictMessage = Services.GlobalHotkeyService.CheckForConflicts(shortcut);
    }

    /// <summary>
    /// Starts recording a keyboard shortcut.
    /// </summary>
    [RelayCommand]
    private void RecordShortcut()
    {
        IsRecordingShortcut = true;
    }

    /// <summary>
    /// Stops recording and applies the shortcut.
    /// </summary>
    public void StopRecording(string? shortcut)
    {
        IsRecordingShortcut = false;
        if (!string.IsNullOrEmpty(shortcut))
        {
            SetGlobalShortcut(shortcut);
        }
    }

    /// <summary>
    /// Cancels shortcut recording without applying.
    /// </summary>
    public void CancelRecording()
    {
        IsRecordingShortcut = false;
    }

    /// <summary>
    /// Clears the global shortcut.
    /// </summary>
    [RelayCommand]
    private void ClearShortcut()
    {
        SetGlobalShortcut(null);
    }
}
