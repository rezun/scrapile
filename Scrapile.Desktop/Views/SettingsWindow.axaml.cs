using System;
using System.Collections.Generic;
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
using AvaloniaKeyModifiers = Avalonia.Input.KeyModifiers;

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

        var viewModel = DataContext as SettingsViewModel;

        // Handle shortcut recording
        if (viewModel?.IsRecordingShortcut == true)
        {
            e.Handled = true;

            // Cancel on Escape
            if (e.Key == AvaloniaKey.Escape)
            {
                viewModel.CancelRecording();
                return;
            }

            // Ignore modifier-only presses
            if (IsModifierKey(e.Key))
            {
                return;
            }

            // Need at least one modifier
            var mods = e.KeyModifiers & (AvaloniaKeyModifiers.Control | AvaloniaKeyModifiers.Alt |
                                         AvaloniaKeyModifiers.Shift | AvaloniaKeyModifiers.Meta);
            if (mods == AvaloniaKeyModifiers.None)
            {
                return;
            }

            // Format the shortcut
            var shortcut = FormatShortcut(mods, e.Key);
            viewModel.StopRecording(shortcut);
            return;
        }

        // Close window with Cmd+W (macOS) or Ctrl+W (Windows/Linux)
        if (e.Key == AvaloniaKey.W && (e.KeyModifiers.HasFlag(AvaloniaKeyModifiers.Meta) || e.KeyModifiers.HasFlag(AvaloniaKeyModifiers.Control)))
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

    private static bool IsModifierKey(AvaloniaKey key)
    {
        return key == AvaloniaKey.LeftCtrl || key == AvaloniaKey.RightCtrl ||
               key == AvaloniaKey.LeftAlt || key == AvaloniaKey.RightAlt ||
               key == AvaloniaKey.LeftShift || key == AvaloniaKey.RightShift ||
               key == AvaloniaKey.LWin || key == AvaloniaKey.RWin;
    }

    private static string FormatShortcut(AvaloniaKeyModifiers modifiers, AvaloniaKey key)
    {
        var parts = new List<string>();
        var isMac = OperatingSystem.IsMacOS();

        if (modifiers.HasFlag(AvaloniaKeyModifiers.Control))
        {
            parts.Add("Ctrl");
        }

        if (modifiers.HasFlag(AvaloniaKeyModifiers.Alt))
        {
            parts.Add(isMac ? "Option" : "Alt");
        }

        if (modifiers.HasFlag(AvaloniaKeyModifiers.Shift))
        {
            parts.Add("Shift");
        }

        if (modifiers.HasFlag(AvaloniaKeyModifiers.Meta))
        {
            parts.Add(isMac ? "Cmd" : "Win");
        }

        // Get key name
        var keyName = key switch
        {
            AvaloniaKey.Space => "Space",
            AvaloniaKey.Enter => "Enter",
            AvaloniaKey.Tab => "Tab",
            AvaloniaKey.Escape => "Escape",
            AvaloniaKey.Back => "Backspace",
            AvaloniaKey.Delete => "Delete",
            AvaloniaKey.Home => "Home",
            AvaloniaKey.End => "End",
            AvaloniaKey.PageUp => "PageUp",
            AvaloniaKey.PageDown => "PageDown",
            AvaloniaKey.Up => "Up",
            AvaloniaKey.Down => "Down",
            AvaloniaKey.Left => "Left",
            AvaloniaKey.Right => "Right",
            AvaloniaKey.F1 => "F1",
            AvaloniaKey.F2 => "F2",
            AvaloniaKey.F3 => "F3",
            AvaloniaKey.F4 => "F4",
            AvaloniaKey.F5 => "F5",
            AvaloniaKey.F6 => "F6",
            AvaloniaKey.F7 => "F7",
            AvaloniaKey.F8 => "F8",
            AvaloniaKey.F9 => "F9",
            AvaloniaKey.F10 => "F10",
            AvaloniaKey.F11 => "F11",
            AvaloniaKey.F12 => "F12",
            AvaloniaKey.OemTilde => "`",
            AvaloniaKey.OemMinus => "-",
            AvaloniaKey.OemPlus => "=",
            AvaloniaKey.OemOpenBrackets => "[",
            AvaloniaKey.OemCloseBrackets => "]",
            AvaloniaKey.OemPipe => "\\",
            AvaloniaKey.OemSemicolon => ";",
            AvaloniaKey.OemQuotes => "'",
            AvaloniaKey.OemComma => ",",
            AvaloniaKey.OemPeriod => ".",
            AvaloniaKey.OemQuestion => "/",
            _ => GetKeyName(key)
        };

        parts.Add(keyName);
        return string.Join("+", parts);
    }

    private static string GetKeyName(AvaloniaKey key)
    {
        // Handle letter keys
        if (key >= AvaloniaKey.A && key <= AvaloniaKey.Z)
        {
            return ((char)('A' + (key - AvaloniaKey.A))).ToString();
        }

        // Handle number keys
        if (key >= AvaloniaKey.D0 && key <= AvaloniaKey.D9)
        {
            return ((char)('0' + (key - AvaloniaKey.D0))).ToString();
        }

        // Handle numpad
        if (key >= AvaloniaKey.NumPad0 && key <= AvaloniaKey.NumPad9)
        {
            return "Num" + ((char)('0' + (key - AvaloniaKey.NumPad0)));
        }

        return key.ToString();
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
