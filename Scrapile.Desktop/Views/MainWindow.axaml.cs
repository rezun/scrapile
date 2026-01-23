using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
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

            await viewModel.InitializeAsync();
        }
    }

    /// <summary>
    /// Handles focus title requests from the view model.
    /// </summary>
    private void OnFocusTitleRequested(object? sender, EventArgs e)
    {
        EditorView?.FocusTitle();
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
        }
    }
}
