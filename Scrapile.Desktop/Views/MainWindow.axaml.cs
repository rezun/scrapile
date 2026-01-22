using Avalonia.Controls;
using Avalonia.Input;
using Scrapile.Desktop.ViewModels;

namespace Scrapile.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // Initialize the view model when the window is loaded
        Loaded += OnLoaded;

        // Handle keyboard shortcuts
        KeyDown += OnKeyDown;
    }

    private async void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            await viewModel.InitializeAsync();
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

        // Check for Ctrl modifier (Cmd on macOS)
        var ctrlPressed = e.KeyModifiers.HasFlag(KeyModifiers.Control) ||
                          e.KeyModifiers.HasFlag(KeyModifiers.Meta);

        if (!ctrlPressed)
        {
            return;
        }

        var shiftPressed = e.KeyModifiers.HasFlag(KeyModifiers.Shift);

        switch (e.Key)
        {
            case Key.Tab:
                // Ctrl+Tab: Next tab, Ctrl+Shift+Tab: Previous tab
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
                // Ctrl+T: New tab, Ctrl+Shift+T will be for reopen (later phase)
                if (!shiftPressed)
                {
                    await viewModel.CreateNewTabAsync();
                    // Focus the editor after creating a new tab
                    EditorView?.FocusContent();
                    e.Handled = true;
                }
                break;

            case Key.W:
                // Ctrl+W: Close current tab
                if (!shiftPressed)
                {
                    await viewModel.CloseCurrentTabAsync();
                    e.Handled = true;
                }
                break;
        }
    }
}
