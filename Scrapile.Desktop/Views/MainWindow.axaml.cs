using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Scrapile.Desktop.ViewModels;

namespace Scrapile.Desktop.Views;

public partial class MainWindow : Window
{
    private NativeMenu? _recentlyClosedNativeMenu;
    private NativeMenuItem? _recentlyClosedMenuItem;

    public MainWindow()
    {
        InitializeComponent();

        // Initialize the view model when the window is loaded
        Loaded += OnLoaded;

        // Handle keyboard shortcuts using tunneling
        // EditorView has its own handlers for when focus is in TextBoxes
        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);

        // Handle window closing for minimize-to-tray
        Closing += OnWindowClosing;
    }

    private void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        // If the app is already quitting, allow the close to proceed
        if (Avalonia.Application.Current is App { IsQuitting: true })
        {
            return;
        }

        // Check if run in background is enabled
        if (DataContext is MainWindowViewModel viewModel)
        {
            var runInBackground = viewModel.SettingsService.GetRunInBackground();

            if (runInBackground)
            {
                // Cancel the close and hide to tray instead
                e.Cancel = true;

                // Get the App instance and hide the window
                if (Avalonia.Application.Current is App app)
                {
                    app.HideWindow();
                }
            }
            else
            {
                // Not running in background, so actually quit
                if (Avalonia.Application.Current is App app)
                {
                    app.QuitApplication();
                }
            }
        }
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

            // Subscribe to delete confirmation request
            viewModel.DeleteConfirmationRequested += OnDeleteConfirmationRequested;

            // Subscribe to property changes to update layout when tab position changes
            viewModel.PropertyChanged += OnViewModelPropertyChanged;

            // Set up native menu commands and dynamic items
            SetupNativeMenu(viewModel);

            await viewModel.InitializeAsync();

            // Apply initial tab position layout
            UpdateTabPositionLayout(viewModel.IsTabListOnLeft);
        }
    }

    /// <summary>
    /// Sets up native menu commands and dynamic menu items.
    /// </summary>
    private void SetupNativeMenu(MainWindowViewModel viewModel)
    {
        // Get the native menu from the attached property
        var nativeMenu = NativeMenu.GetMenu(this);
        if (nativeMenu == null) return;

        // Find menu items by traversing the menu structure
        NativeMenuItem? reopenLastClosedItem = null;
        NativeMenuItem? searchItem = null;
        NativeMenuItem? settingsItem = null;
        NativeMenuItem? recentlyClosedItem = null;
        NativeMenuItem? versionItem = null;
        NativeMenuItem? quitItem = null;
        NativeMenuItem? appMenuSettingsItem = null;

        foreach (var topLevelItem in nativeMenu.Items)
        {
            if (topLevelItem is NativeMenuItem menuItem)
            {
                if (menuItem.Header == "Scrapile" && menuItem.Menu != null)
                {
                    foreach (var subItem in menuItem.Menu.Items)
                    {
                        if (subItem is NativeMenuItem subMenuItem)
                        {
                            if (subMenuItem.Header == "Quit Scrapile")
                            {
                                quitItem = subMenuItem;
                            }
                            else if (subMenuItem.Header == "Settings...")
                            {
                                appMenuSettingsItem = subMenuItem;
                            }
                        }
                    }
                }
                else if (menuItem.Header == "Tab" && menuItem.Menu != null)
                {
                    foreach (var subItem in menuItem.Menu.Items)
                    {
                        if (subItem is NativeMenuItem subMenuItem)
                        {
                            if (subMenuItem.Header == "Recently Closed")
                            {
                                recentlyClosedItem = subMenuItem;
                                _recentlyClosedNativeMenu = subMenuItem.Menu;
                            }
                            else if (subMenuItem.Header == "Reopen Last Closed")
                            {
                                reopenLastClosedItem = subMenuItem;
                            }
                        }
                    }
                }
                else if (menuItem.Header == "Edit" && menuItem.Menu != null)
                {
                    foreach (var subItem in menuItem.Menu.Items)
                    {
                        if (subItem is NativeMenuItem subMenuItem)
                        {
                            if (subMenuItem.Header == "Search...")
                            {
                                searchItem = subMenuItem;
                            }
                            else if (subMenuItem.Header == "Settings...")
                            {
                                settingsItem = subMenuItem;
                            }
                        }
                    }
                }
                else if (menuItem.Header == "Info" && menuItem.Menu != null)
                {
                    foreach (var subItem in menuItem.Menu.Items)
                    {
                        if (subItem is NativeMenuItem subMenuItem && subMenuItem.Header == "Version")
                        {
                            versionItem = subMenuItem;
                        }
                    }
                }
            }
        }

        // Wire up commands
        if (quitItem != null)
        {
            quitItem.Click += (_, _) =>
            {
                if (Avalonia.Application.Current is App app)
                {
                    app.QuitApplication();
                }
            };
            quitItem.Gesture = OperatingSystem.IsMacOS()
                ? new KeyGesture(Key.Q, KeyModifiers.Meta)
                : new KeyGesture(Key.Q, KeyModifiers.Control);
        }

        if (appMenuSettingsItem != null)
        {
            appMenuSettingsItem.Command = viewModel.OpenSettingsCommand;
            appMenuSettingsItem.Gesture = OperatingSystem.IsMacOS()
                ? new KeyGesture(Key.OemComma, KeyModifiers.Meta)
                : new KeyGesture(Key.OemComma, KeyModifiers.Control);
        }

        if (reopenLastClosedItem != null)
        {
            reopenLastClosedItem.Command = viewModel.ReopenLastClosedCommand;
            // Set platform-appropriate gesture
            reopenLastClosedItem.Gesture = OperatingSystem.IsMacOS()
                ? new KeyGesture(Key.T, KeyModifiers.Meta | KeyModifiers.Shift)
                : new KeyGesture(Key.T, KeyModifiers.Control | KeyModifiers.Shift);
        }

        if (searchItem != null)
        {
            searchItem.Command = viewModel.ShowSearchCommand;
            searchItem.Gesture = OperatingSystem.IsMacOS()
                ? new KeyGesture(Key.P, KeyModifiers.Meta)
                : new KeyGesture(Key.P, KeyModifiers.Control);
        }

        if (settingsItem != null)
        {
            settingsItem.Command = viewModel.OpenSettingsCommand;
            settingsItem.Gesture = OperatingSystem.IsMacOS()
                ? new KeyGesture(Key.OemComma, KeyModifiers.Meta)
                : new KeyGesture(Key.OemComma, KeyModifiers.Control);
        }

        // Set version from assembly
        if (versionItem != null)
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            var versionString = version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "Unknown";
            versionItem.Header = $"Version {versionString}";
        }

        // Store reference for dynamic updates
        _recentlyClosedMenuItem = recentlyClosedItem;

        // Subscribe to collection changes
        viewModel.RecentlyClosedMenuItems.CollectionChanged += OnRecentlyClosedMenuItemsChanged;

        // Initial population
        UpdateRecentlyClosedNativeMenu(viewModel);
    }

    /// <summary>
    /// Handles changes to the RecentlyClosedMenuItems collection.
    /// </summary>
    private void OnRecentlyClosedMenuItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            UpdateRecentlyClosedNativeMenu(viewModel);
        }
    }

    /// <summary>
    /// Updates the native Recently Closed submenu with current items.
    /// </summary>
    private void UpdateRecentlyClosedNativeMenu(MainWindowViewModel viewModel)
    {
        if (_recentlyClosedNativeMenu == null) return;

        _recentlyClosedNativeMenu.Items.Clear();

        foreach (var item in viewModel.RecentlyClosedMenuItems)
        {
            _recentlyClosedNativeMenu.Items.Add(new NativeMenuItem
            {
                Header = item.MenuHeader,
                Command = item.ReopenCommand
            });
        }

        // Update enabled state
        if (_recentlyClosedMenuItem != null)
        {
            _recentlyClosedMenuItem.IsEnabled = viewModel.HasRecentlyClosedMenuItems;
        }
    }

    /// <summary>
    /// Handles property changes from the view model.
    /// </summary>
    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.IsTabListOnLeft) &&
            sender is MainWindowViewModel viewModel)
        {
            UpdateTabPositionLayout(viewModel.IsTabListOnLeft);
        }
    }

    /// <summary>
    /// Updates the grid column definitions based on tab position setting.
    /// </summary>
    private void UpdateTabPositionLayout(bool isTabListOnLeft)
    {
        if (MainLayoutGrid?.ColumnDefinitions == null || MainLayoutGrid.ColumnDefinitions.Count < 3)
        {
            return;
        }

        var col0 = MainLayoutGrid.ColumnDefinitions[0];
        var col2 = MainLayoutGrid.ColumnDefinitions[2];

        if (isTabListOnLeft)
        {
            // Tab list on left: col0 = fixed width, col2 = flexible
            col0.Width = new GridLength(220);
            col0.MinWidth = 150;
            col0.MaxWidth = 400;
            col2.Width = new GridLength(1, GridUnitType.Star);
            col2.MinWidth = 300;
            col2.MaxWidth = double.PositiveInfinity;
        }
        else
        {
            // Tab list on right: col0 = flexible, col2 = fixed width
            col0.Width = new GridLength(1, GridUnitType.Star);
            col0.MinWidth = 300;
            col0.MaxWidth = double.PositiveInfinity;
            col2.Width = new GridLength(220);
            col2.MinWidth = 150;
            col2.MaxWidth = 400;
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

        var settingsViewModel = new SettingsViewModel(viewModel.SettingsService, viewModel.ThemeService, viewModel.AutorunService);
        var settingsWindow = new SettingsWindow
        {
            DataContext = settingsViewModel
        };

        await settingsWindow.ShowDialog(this);
    }

    /// <summary>
    /// Handles delete confirmation requests from the view model.
    /// Shows a confirmation dialog and deletes the tab if confirmed.
    /// </summary>
    private async void OnDeleteConfirmationRequested(object? sender, DeleteConfirmationRequestEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        // Build the confirmation message based on whether the document has a title
        var message = string.IsNullOrEmpty(e.DisplayTitle)
            ? "Are you sure you want to permanently delete this document?\n\nThis cannot be undone."
            : $"Are you sure you want to permanently delete \"{e.DisplayTitle}\"?\n\nThis cannot be undone.";

        var dialogViewModel = new MessageDialogViewModel
        {
            Title = "Delete Document",
            Message = message,
            PrimaryButtonText = "Delete",
            SecondaryButtonText = "Cancel"
        };

        var dialog = new MessageDialog
        {
            DataContext = dialogViewModel
        };

        await dialog.ShowDialog(this);

        if (dialog.Result == MessageDialogResult.Primary)
        {
            await viewModel.ConfirmDeleteAsync(e.Tab);
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
        if (e.Key == Key.Escape)
        {
            // First check if find bar is visible
            if (viewModel.EditorViewModel.IsFindBarVisible)
            {
                e.Handled = true;
                viewModel.EditorViewModel.HideFindBar();
                EditorView?.FocusContent();
                return;
            }

            // Then check if search overlay is visible
            if (viewModel.IsSearchVisible)
            {
                e.Handled = true;
                viewModel.HideSearch();
                return;
            }

            return;
        }

        // Handle F3 for find navigation (no modifier needed)
        if (e.Key == Key.F3 && viewModel.EditorViewModel.IsFindBarVisible)
        {
            e.Handled = true;
            if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
            {
                viewModel.EditorViewModel.FindPrevious();
            }
            else
            {
                viewModel.EditorViewModel.FindNext();
            }
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
                if (shiftPressed)
                {
                    // Ctrl/Cmd+Shift+W: Hide window to tray (only when running in background)
                    if (viewModel.SettingsService.GetRunInBackground())
                    {
                        e.Handled = true;
                        if (Avalonia.Application.Current is App app)
                        {
                            app.HideWindow();
                        }
                    }
                }
                else
                {
                    // Ctrl/Cmd+W: Close current tab
                    // Mark as handled BEFORE await to prevent duplicate handling
                    e.Handled = true;
                    await viewModel.CloseCurrentTabAsync();
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

            case Key.F:
                // Ctrl/Cmd+F: Find in document
                if (!shiftPressed && viewModel.SelectedTab != null)
                {
                    e.Handled = true;
                    viewModel.EditorViewModel.ShowFindBar();
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

            case Key.Q:
                // Cmd+Q on macOS: Always quit (bypass minimize-to-tray)
                if (OperatingSystem.IsMacOS() && !shiftPressed)
                {
                    e.Handled = true;
                    if (Avalonia.Application.Current is App app)
                    {
                        app.QuitApplication();
                    }
                }
                break;
        }
    }
}
