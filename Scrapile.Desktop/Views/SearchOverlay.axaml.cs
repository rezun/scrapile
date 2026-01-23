using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Scrapile.Desktop.ViewModels;

namespace Scrapile.Desktop.Views;

/// <summary>
/// Code-behind for the SearchOverlay user control.
/// Handles keyboard navigation and mouse click events.
/// </summary>
public partial class SearchOverlay : UserControl
{
    private SearchViewModel? ViewModel => DataContext as SearchViewModel;

    public SearchOverlay()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Called when the control is loaded.
    /// Sets up event handlers.
    /// </summary>
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        // Focus the search input when the overlay is shown
        SearchInput?.Focus();

        // Handle click on result items
        AddHandler(PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
    }

    /// <summary>
    /// Handles key down events for keyboard navigation.
    /// </summary>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        switch (e.Key)
        {
            case Key.Down:
                ViewModel?.SelectNext();
                e.Handled = true;
                break;

            case Key.Up:
                ViewModel?.SelectPrevious();
                e.Handled = true;
                break;

            case Key.Enter:
                ViewModel?.OpenSelectedResult();
                e.Handled = true;
                break;

            case Key.Escape:
                ViewModel?.RequestClose();
                e.Handled = true;
                break;
        }
    }

    /// <summary>
    /// Handles pointer pressed events.
    /// Opens result on click, closes on backdrop click.
    /// </summary>
    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var source = e.Source as Control;

        // Check if clicked on a result item
        var resultBorder = FindParentResultItem(source);
        if (resultBorder != null)
        {
            // Get the result item's DataContext
            if (resultBorder.DataContext is SearchResultItemViewModel result)
            {
                ViewModel?.OpenResult(result);
                e.Handled = true;
                return;
            }
        }

        // Check if clicked on the backdrop (semi-transparent panel)
        // The search modal itself has a solid background
        if (source is Panel panel && panel.Background?.ToString()?.Contains("40000000") == true)
        {
            ViewModel?.RequestClose();
            e.Handled = true;
        }
    }

    /// <summary>
    /// Finds the parent result item Border for a control.
    /// </summary>
    private static Border? FindParentResultItem(Control? control)
    {
        while (control != null)
        {
            if (control is Border border && border.Name == "ResultItem")
            {
                return border;
            }
            control = control.Parent as Control;
        }
        return null;
    }

    /// <summary>
    /// Focuses the search input field.
    /// </summary>
    public void FocusSearchInput()
    {
        SearchInput?.Focus();
        SearchInput?.SelectAll();
    }
}
