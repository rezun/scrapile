using System;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Scrapile.Desktop.ViewModels;

namespace Scrapile.Desktop.Views;

public partial class TabListView : UserControl, INotifyPropertyChanged
{
    private bool _isScrollable;

    public new event PropertyChangedEventHandler? PropertyChanged;

    public bool IsScrollable
    {
        get => _isScrollable;
        private set
        {
            if (_isScrollable != value)
            {
                _isScrollable = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsScrollable)));
            }
        }
    }

    public TabListView()
    {
        InitializeComponent();

        // Set shortcut hint based on platform
        var shortcut = OperatingSystem.IsMacOS() ? "⌘T" : "Ctrl+T";
        if (this.FindControl<TextBlock>("InlineShortcutHint") is { } hint)
            hint.Text = shortcut;
        if (this.FindControl<TextBlock>("FixedShortcutHint") is { } fixedHint)
            fixedHint.Text = shortcut;
    }

    /// <summary>
    /// Handles scroll changes to detect when tabs overflow and scrolling is needed.
    /// </summary>
    private void OnTabScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (sender is ScrollViewer scrollViewer)
        {
            IsScrollable = scrollViewer.Extent.Height > scrollViewer.Viewport.Height;
        }
    }

    /// <summary>
    /// Handles double-tap on the tab area to create a new tab.
    /// </summary>
    private void OnTabAreaDoubleTapped(object? sender, TappedEventArgs e)
    {
        // Only create tab if double-click was on empty space (not on a tab item)
        if (DataContext is TabListViewModel vm)
        {
            var source = e.Source as Control;
            while (source != null && source != this)
            {
                if (source.DataContext is TabItemViewModel)
                    return; // Clicked on a tab, don't create new
                source = source.Parent as Control;
            }
            vm.CreateNewTabCommand.Execute(null);
        }
    }

    /// <summary>
    /// Handles click on the new tab button (both inline and fixed).
    /// </summary>
    private void OnNewTabButtonPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is TabListViewModel vm)
        {
            vm.CreateNewTabCommand.Execute(null);
        }
    }

    /// <summary>
    /// Handles pointer press on a tab item to select it.
    /// </summary>
    private void OnTabPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border border && border.DataContext is TabItemViewModel tabViewModel)
        {
            if (DataContext is TabListViewModel listViewModel)
            {
                listViewModel.SelectTab(tabViewModel);
            }
        }
    }

    /// <summary>
    /// Gets the TabItemViewModel from a context menu event sender.
    /// The DataContext is inherited from the Border through the MenuFlyout.
    /// </summary>
    private TabItemViewModel? GetTabFromContextMenu(object? sender)
    {
        if (sender is MenuItem menuItem && menuItem.DataContext is TabItemViewModel tabViewModel)
        {
            return tabViewModel;
        }
        return null;
    }

    /// <summary>
    /// Handles the "Close Tab" context menu item click.
    /// </summary>
    private async void OnContextMenuClose(object? sender, RoutedEventArgs e)
    {
        var tabViewModel = GetTabFromContextMenu(sender);
        if (tabViewModel != null && DataContext is TabListViewModel listViewModel)
        {
            await listViewModel.CloseTabAsync(tabViewModel);
        }
    }

    /// <summary>
    /// Handles the "Close All Tabs" context menu item click.
    /// </summary>
    private async void OnContextMenuCloseAll(object? sender, RoutedEventArgs e)
    {
        if (DataContext is TabListViewModel listViewModel)
        {
            await listViewModel.CloseAllTabsAsync();
        }
    }

    /// <summary>
    /// Handles the "Close Tabs Above" context menu item click.
    /// </summary>
    private async void OnContextMenuCloseAbove(object? sender, RoutedEventArgs e)
    {
        var tabViewModel = GetTabFromContextMenu(sender);
        if (tabViewModel != null && DataContext is TabListViewModel listViewModel)
        {
            await listViewModel.CloseTabsAboveAsync(tabViewModel);
        }
    }

    /// <summary>
    /// Handles the "Close Tabs Below" context menu item click.
    /// </summary>
    private async void OnContextMenuCloseBelow(object? sender, RoutedEventArgs e)
    {
        var tabViewModel = GetTabFromContextMenu(sender);
        if (tabViewModel != null && DataContext is TabListViewModel listViewModel)
        {
            await listViewModel.CloseTabsBelowAsync(tabViewModel);
        }
    }

    /// <summary>
    /// Handles the "Duplicate Tab" context menu item click.
    /// </summary>
    private async void OnContextMenuDuplicate(object? sender, RoutedEventArgs e)
    {
        var tabViewModel = GetTabFromContextMenu(sender);
        if (tabViewModel != null && DataContext is TabListViewModel listViewModel)
        {
            await listViewModel.DuplicateTabAsync(tabViewModel);
        }
    }

    /// <summary>
    /// Handles the "Edit Title" context menu item click.
    /// </summary>
    private void OnContextMenuEditTitle(object? sender, RoutedEventArgs e)
    {
        var tabViewModel = GetTabFromContextMenu(sender);
        if (tabViewModel != null && DataContext is TabListViewModel listViewModel)
        {
            // First select the tab, then request edit title
            listViewModel.SelectTab(tabViewModel);
            listViewModel.RequestEditTitle();
        }
    }

    /// <summary>
    /// Handles the "Copy to Clipboard" context menu item click.
    /// </summary>
    private void OnContextMenuCopyToClipboard(object? sender, RoutedEventArgs e)
    {
        var tabViewModel = GetTabFromContextMenu(sender);
        if (tabViewModel != null && DataContext is TabListViewModel listViewModel)
        {
            listViewModel.RequestCopyToClipboard(tabViewModel);
        }
    }

    /// <summary>
    /// Handles the "Save As..." context menu item click.
    /// </summary>
    private void OnContextMenuSaveAs(object? sender, RoutedEventArgs e)
    {
        var tabViewModel = GetTabFromContextMenu(sender);
        if (tabViewModel != null && DataContext is TabListViewModel listViewModel)
        {
            listViewModel.RequestSaveAs(tabViewModel);
        }
    }

    /// <summary>
    /// Handles the "Delete" context menu item click.
    /// </summary>
    private void OnContextMenuDelete(object? sender, RoutedEventArgs e)
    {
        var tabViewModel = GetTabFromContextMenu(sender);
        if (tabViewModel != null && DataContext is TabListViewModel listViewModel)
        {
            listViewModel.RequestDelete(tabViewModel);
        }
    }
}
