using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Scrapile.Desktop.ViewModels;

namespace Scrapile.Desktop.Views;

public partial class TabListView : UserControl
{
    public TabListView()
    {
        InitializeComponent();
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
    /// Handles pointer press on the recently closed header to toggle expansion.
    /// </summary>
    private async void OnRecentlyClosedHeaderPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is TabListViewModel listViewModel)
        {
            await listViewModel.ToggleRecentlyClosedAsync();
        }
    }

    /// <summary>
    /// Handles pointer press on a recently closed item to reopen it.
    /// </summary>
    private void OnRecentlyClosedItemPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border border && border.DataContext is RecentlyClosedItemViewModel itemViewModel)
        {
            itemViewModel.ReopenCommand.Execute(null);
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
}
