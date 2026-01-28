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
}
