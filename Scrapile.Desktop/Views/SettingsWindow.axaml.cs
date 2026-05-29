using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Scrapile.Desktop.Services;
using Scrapile.Desktop.ViewModels;

using AvaloniaKey = Avalonia.Input.Key;

namespace Scrapile.Desktop.Views;

/// <summary>
/// Settings window code-behind.
/// </summary>
public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        // Close window with Cmd+W (macOS) or Ctrl+W (Windows/Linux)
        if (e.Key == AvaloniaKey.W && (e.KeyModifiers.HasFlag(KeyModifiers.Meta) || e.KeyModifiers.HasFlag(KeyModifiers.Control)))
        {
            e.Handled = true;
            Close();
        }
        // Also close with Escape
        else if (e.Key == AvaloniaKey.Escape)
        {
            e.Handled = true;
            Close();
        }
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is SettingsViewModel viewModel)
        {
            viewModel.CloseRequested += OnCloseRequested;
            viewModel.FolderBrowserRequested += OnFolderBrowserRequested;
            viewModel.StorageDirectoryDialogRequested += OnStorageDirectoryDialogRequested;
            viewModel.ResetConfirmationRequested += OnResetConfirmationRequested;
            viewModel.Initialize();
        }
    }

    private void OnCloseRequested(object? sender, EventArgs e)
    {
        Close();
    }

    private async void OnFolderBrowserRequested(object? sender, FolderBrowserEventArgs e)
    {
        var viewModel = DataContext as SettingsViewModel;
        if (viewModel == null) return;

        var options = new FolderPickerOpenOptions
        {
            Title = "Select Document Storage Directory",
            AllowMultiple = false
        };

        // Try to set initial folder if provided
        if (!string.IsNullOrEmpty(e.InitialDirectory) && Directory.Exists(e.InitialDirectory))
        {
            try
            {
                var folder = await StorageProvider.TryGetFolderFromPathAsync(e.InitialDirectory);
                if (folder != null)
                {
                    options.SuggestedStartLocation = folder;
                }
            }
            catch
            {
                // Ignore if we can't get the folder
            }
        }

        var result = await StorageProvider.OpenFolderPickerAsync(options);

        if (result.Count > 0)
        {
            var selectedPath = result[0].Path.LocalPath;
            viewModel.ValidateAndRequestStorageDirectoryChange(selectedPath);
        }
    }

    private async void OnStorageDirectoryDialogRequested(object? sender, StorageDirectoryDialogEventArgs e)
    {
        var viewModel = DataContext as SettingsViewModel;
        if (viewModel == null) return;

        switch (e.ValidationResult)
        {
            case StorageDirectoryValidationResult.ExistingScrapileFolder:
                await HandleExistingScrapileFolderAsync(viewModel, e);
                break;

            case StorageDirectoryValidationResult.EmptyFolder:
                await HandleEmptyFolderAsync(viewModel, e);
                break;

            case StorageDirectoryValidationResult.InvalidFolder:
                await HandleInvalidFolderAsync();
                break;
        }
    }

    private async Task HandleExistingScrapileFolderAsync(SettingsViewModel viewModel, StorageDirectoryDialogEventArgs e)
    {
        var dialogViewModel = new MessageDialogViewModel
        {
            Title = "Use Existing Folder",
            Message = "This folder already contains Scrapile data. Do you want to use this folder?",
            PrimaryButtonText = "Use This Folder",
            SecondaryButtonText = "Cancel"
        };

        var dialog = new MessageDialog { DataContext = dialogViewModel };
        await dialog.ShowDialog(this);

        if (dialog.Result == MessageDialogResult.Primary)
        {
            try
            {
                await viewModel.CompleteStorageDirectoryChangeAsync(e.SelectedPath, copyData: false);
                await PromptRestartAsync();
            }
            catch (Exception ex)
            {
                await ShowErrorDialogAsync("Error", $"Failed to change storage directory: {ex.Message}");
            }
        }
    }

    private async Task HandleEmptyFolderAsync(SettingsViewModel viewModel, StorageDirectoryDialogEventArgs e)
    {
        if (e.HasExistingData)
        {
            // Show choice dialog: Copy Data, Start Fresh, or Cancel
            var dialogViewModel = new MessageDialogViewModel
            {
                Title = "Storage Directory",
                Message = "You have existing data. Would you like to copy it to the new folder or start fresh?",
                PrimaryButtonText = "Copy Data",
                SecondaryButtonText = "Start Fresh",
                TertiaryButtonText = "Cancel"
            };

            var dialog = new MessageDialog { DataContext = dialogViewModel };
            await dialog.ShowDialog(this);

            try
            {
                switch (dialog.Result)
                {
                    case MessageDialogResult.Primary:
                        // Copy Data
                        await viewModel.CompleteStorageDirectoryChangeAsync(e.SelectedPath, copyData: true);
                        await PromptRestartAsync();
                        break;

                    case MessageDialogResult.Secondary:
                        // Start Fresh
                        await viewModel.CompleteStorageDirectoryChangeAsync(e.SelectedPath, copyData: false);
                        await PromptRestartAsync();
                        break;

                    // Tertiary or Cancelled - do nothing
                }
            }
            catch (UnauthorizedAccessException)
            {
                await ShowErrorDialogAsync("Permission Denied", "You don't have permission to access this folder. Please choose a different folder.");
            }
            catch (IOException ex)
            {
                await ShowErrorDialogAsync("Copy Failed", $"Failed to copy data to the new folder: {ex.Message}");
            }
            catch (Exception ex)
            {
                await ShowErrorDialogAsync("Error", $"An error occurred: {ex.Message}");
            }
        }
        else
        {
            // No existing data, just set the directory directly
            try
            {
                await viewModel.CompleteStorageDirectoryChangeAsync(e.SelectedPath, copyData: false);
                await PromptRestartAsync();
            }
            catch (Exception ex)
            {
                await ShowErrorDialogAsync("Error", $"Failed to change storage directory: {ex.Message}");
            }
        }
    }

    private async Task HandleInvalidFolderAsync()
    {
        await ShowErrorDialogAsync(
            "Invalid Folder",
            "This folder contains other files and cannot be used as a Scrapile storage directory. Please choose an empty folder or an existing Scrapile folder.");
    }

    private async void OnResetConfirmationRequested(object? sender, ResetConfirmationEventArgs e)
    {
        var viewModel = DataContext as SettingsViewModel;
        if (viewModel == null) return;

        var dialogViewModel = new MessageDialogViewModel
        {
            Title = "Reset Settings",
            Message = "Are you sure you want to reset all settings to their default values?",
            PrimaryButtonText = "Reset",
            SecondaryButtonText = "Cancel"
        };

        var dialog = new MessageDialog { DataContext = dialogViewModel };
        await dialog.ShowDialog(this);

        if (dialog.Result == MessageDialogResult.Primary)
        {
            await viewModel.ConfirmResetToDefaultsAsync();
        }
    }

    private async Task ShowErrorDialogAsync(string title, string message)
    {
        var dialogViewModel = new MessageDialogViewModel
        {
            Title = title,
            Message = message,
            PrimaryButtonText = "OK"
        };

        var dialog = new MessageDialog { DataContext = dialogViewModel };
        await dialog.ShowDialog(this);
    }

    private async Task PromptRestartAsync()
    {
        var dialogViewModel = new MessageDialogViewModel
        {
            Title = "Restart Required",
            Message = "The storage directory change will take effect after restarting the app. Would you like to restart now?",
            PrimaryButtonText = "Restart Now",
            SecondaryButtonText = "Later"
        };

        var dialog = new MessageDialog { DataContext = dialogViewModel };
        await dialog.ShowDialog(this);

        if (dialog.Result == MessageDialogResult.Primary)
        {
            RestartApplication();
        }
    }

    private void RestartApplication()
    {
        var executablePath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(executablePath))
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = executablePath,
                UseShellExecute = true
            });

            if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Shutdown();
            }
        }
        catch
        {
            // If restart fails, the user can manually restart
        }
    }
}
