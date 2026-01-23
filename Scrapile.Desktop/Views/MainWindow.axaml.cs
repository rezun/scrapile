using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Scrapile.Desktop.ViewModels;

namespace Scrapile.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // Initialize the view model when the window is loaded
        Loaded += OnLoaded;

        // Handle keyboard shortcuts using tunneling
        // EditorView has its own handlers for when focus is in TextBoxes
        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
    }

    private async void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            // Subscribe to FocusTitleRequested event
            viewModel.FocusTitleRequested += OnFocusTitleRequested;

            // Subscribe to clipboard and save as events
            viewModel.ClipboardCopyRequested += OnClipboardCopyRequested;
            viewModel.SaveAsRequested += OnSaveAsRequested;

            // Subscribe to settings window request
            viewModel.OpenSettingsRequested += OnOpenSettingsRequested;

            await viewModel.InitializeAsync();
        }
    }

    /// <summary>
    /// Handles open settings requests from the view model.
    /// </summary>
    private async void OnOpenSettingsRequested(object? sender, EventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        var settingsViewModel = new SettingsViewModel(viewModel.SettingsService, viewModel.ThemeService);
        var settingsWindow = new SettingsWindow
        {
            DataContext = settingsViewModel
        };

        await settingsWindow.ShowDialog(this);
    }

    /// <summary>
    /// Handles focus title requests from the view model.
    /// </summary>
    private void OnFocusTitleRequested(object? sender, EventArgs e)
    {
        EditorView?.FocusTitle();
    }

    /// <summary>
    /// Handles clipboard copy requests from the view model.
    /// </summary>
    private async void OnClipboardCopyRequested(object? sender, string content)
    {
        var clipboard = GetTopLevel(this)?.Clipboard;
        if (clipboard != null)
        {
            await clipboard.SetTextAsync(content);
        }
    }

    /// <summary>
    /// Handles Save As requests from the view model.
    /// </summary>
    private async void OnSaveAsRequested(object? sender, SaveAsRequestEventArgs e)
    {
        await ShowSaveAsDialogAsync(e.Content, e.SuggestedFileName);
    }

    /// <summary>
    /// Shows a Save As dialog and saves the content to the selected file.
    /// </summary>
    private async Task ShowSaveAsDialogAsync(string content, string suggestedFileName)
    {
        var storageProvider = GetTopLevel(this)?.StorageProvider;
        if (storageProvider == null)
        {
            return;
        }

        var file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save As",
            SuggestedFileName = suggestedFileName,
            DefaultExtension = "txt",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("Text Files") { Patterns = new[] { "*.txt" } },
                new FilePickerFileType("All Files") { Patterns = new[] { "*" } }
            }
        });

        if (file != null)
        {
            await using var stream = await file.OpenWriteAsync();
            await using var writer = new StreamWriter(stream);
            await writer.WriteAsync(content);
        }
    }

    /// <summary>
    /// Handles keyboard shortcuts for tab operations.
    /// </summary>
    private async void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        // Handle Escape separately (no modifier needed)
        if (e.Key == Key.Escape && viewModel.IsSearchVisible)
        {
            e.Handled = true;
            viewModel.HideSearch();
            return;
        }

        // Handle F2 for Edit Title (no modifier needed)
        if (e.Key == Key.F2 && viewModel.SelectedTab != null)
        {
            e.Handled = true;
            viewModel.RequestEditTitle();
            return;
        }

        // Use platform-appropriate modifier: Cmd on macOS, Ctrl on Windows/Linux
        bool modifierPressed;
        if (OperatingSystem.IsMacOS())
        {
            // On macOS, only respond to Cmd (Meta), not Ctrl
            modifierPressed = e.KeyModifiers.HasFlag(KeyModifiers.Meta) &&
                              !e.KeyModifiers.HasFlag(KeyModifiers.Control);
        }
        else
        {
            // On Windows/Linux, only respond to Ctrl, not Cmd
            modifierPressed = e.KeyModifiers.HasFlag(KeyModifiers.Control) &&
                              !e.KeyModifiers.HasFlag(KeyModifiers.Meta);
        }

        if (!modifierPressed)
        {
            return;
        }

        var shiftPressed = e.KeyModifiers.HasFlag(KeyModifiers.Shift);

        switch (e.Key)
        {
            case Key.Tab:
                // Ctrl/Cmd+Tab: Next tab, Ctrl/Cmd+Shift+Tab: Previous tab
                if (shiftPressed)
                {
                    viewModel.SelectPreviousTab();
                }
                else
                {
                    viewModel.SelectNextTab();
                }
                e.Handled = true;
                break;

            case Key.T:
                // Ctrl/Cmd+T: New tab, Ctrl/Cmd+Shift+T: Reopen last closed tab
                if (shiftPressed)
                {
                    // Ctrl/Cmd+Shift+T: Reopen last closed tab
                    e.Handled = true;
                    var reopened = await viewModel.ReopenLastClosedAsync();
                    if (reopened)
                    {
                        // Focus the editor after reopening a tab
                        EditorView?.FocusContent();
                    }
                }
                else
                {
                    // Ctrl/Cmd+T: New tab
                    e.Handled = true;
                    await viewModel.CreateNewTabAsync();
                    // Focus the editor after creating a new tab
                    EditorView?.FocusContent();
                }
                break;

            case Key.W:
                // Ctrl/Cmd+W: Close current tab
                if (!shiftPressed)
                {
                    // Mark as handled BEFORE await to prevent duplicate handling
                    e.Handled = true;
                    await viewModel.CloseCurrentTabAsync();
                }
                break;

            case Key.L:
                // Ctrl/Cmd+Shift+L: Cycle theme
                if (shiftPressed)
                {
                    e.Handled = true;
                    await viewModel.CycleTheme();
                }
                break;

            case Key.P:
            case Key.K:
                // Ctrl/Cmd+P or Ctrl/Cmd+K: Open search
                if (!shiftPressed)
                {
                    e.Handled = true;
                    viewModel.ShowSearch();
                    // Focus the search input after showing
                    SearchOverlay?.FocusSearchInput();
                }
                break;

            case Key.D:
                // Ctrl/Cmd+Shift+D: Duplicate current tab
                if (shiftPressed)
                {
                    e.Handled = true;
                    await viewModel.DuplicateCurrentTabAsync();
                    // Focus the editor after duplicating
                    EditorView?.FocusContent();
                }
                break;

            case Key.C:
                // Ctrl/Cmd+Shift+C: Copy entire document to clipboard
                if (shiftPressed)
                {
                    e.Handled = true;
                    viewModel.CopyCurrentTabToClipboard();
                }
                break;

            case Key.S:
                // Ctrl/Cmd+Shift+S: Save As
                if (shiftPressed)
                {
                    e.Handled = true;
                    viewModel.RequestSaveAs();
                }
                break;

            case Key.E:
                // Ctrl/Cmd+Shift+E: Edit Title
                if (shiftPressed && viewModel.SelectedTab != null)
                {
                    e.Handled = true;
                    viewModel.RequestEditTitle();
                }
                break;

            case Key.OemComma:
                // Ctrl/Cmd+,: Open Settings
                if (!shiftPressed)
                {
                    e.Handled = true;
                    viewModel.OpenSettingsCommand.Execute(null);
                }
                break;
        }
    }
}
