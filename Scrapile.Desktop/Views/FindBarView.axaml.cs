using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Scrapile.Desktop.ViewModels;

namespace Scrapile.Desktop.Views;

public partial class FindBarView : UserControl
{
    public FindBarView()
    {
        InitializeComponent();

        // Handle keyboard events in the find input
        FindInput.AddHandler(KeyDownEvent, OnFindInputKeyDown, RoutingStrategies.Tunnel);
    }

    /// <summary>
    /// Focuses the find input text box.
    /// </summary>
    public void FocusFindInput()
    {
        FindInput.Focus();
        FindInput.SelectAll();
    }

    private void OnFindInputKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not FindViewModel viewModel)
        {
            return;
        }

        switch (e.Key)
        {
            case Key.Enter:
                e.Handled = true;
                if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                {
                    viewModel.FindPrevious();
                }
                else
                {
                    viewModel.FindNext();
                }
                break;

            case Key.Escape:
                e.Handled = true;
                viewModel.RequestClose();
                break;
        }
    }

    private void OnPreviousClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is FindViewModel viewModel)
        {
            viewModel.FindPrevious();
        }
    }

    private void OnNextClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is FindViewModel viewModel)
        {
            viewModel.FindNext();
        }
    }

    private void OnCloseClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is FindViewModel viewModel)
        {
            viewModel.RequestClose();
        }
    }
}
