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
}
