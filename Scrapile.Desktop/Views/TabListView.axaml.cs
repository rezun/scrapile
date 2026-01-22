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
}
