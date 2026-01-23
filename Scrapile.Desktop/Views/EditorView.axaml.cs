using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Scrapile.Desktop.ViewModels;

namespace Scrapile.Desktop.Views;

public partial class EditorView : UserControl
{
    public EditorView()
    {
        InitializeComponent();

        // Attach keyboard handlers after the control is fully loaded
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Handle keyboard shortcuts at the TextBox level using tunneling
        // This intercepts shortcuts before the native text system can consume them
        // Use both Tunnel and Bubble to catch at every stage
        ContentTextBox.AddHandler(KeyDownEvent, OnTextBoxKeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);
        TitleTextBox.AddHandler(KeyDownEvent, OnTextBoxKeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);
    }

    /// <summary>
    /// Intercepts keyboard shortcuts in TextBoxes before native text handling.
    /// </summary>
    private async void OnTextBoxKeyDown(object? sender, KeyEventArgs e)
    {
        // Skip if already handled by MainWindow
        if (e.Handled)
        {
            return;
        }

        // Check for platform-appropriate modifier
        bool modifierPressed;
        if (OperatingSystem.IsMacOS())
        {
            modifierPressed = e.KeyModifiers.HasFlag(KeyModifiers.Meta) &&
                              !e.KeyModifiers.HasFlag(KeyModifiers.Control);
        }
        else
        {
            modifierPressed = e.KeyModifiers.HasFlag(KeyModifiers.Control) &&
                              !e.KeyModifiers.HasFlag(KeyModifiers.Meta);
        }

        if (!modifierPressed)
        {
            return;
        }

        // Get the MainWindowViewModel to execute actions
        var mainWindow = this.FindAncestorOfType<MainWindow>();
        var viewModel = mainWindow?.DataContext as MainWindowViewModel;
        if (viewModel == null)
        {
            return;
        }

        var shiftPressed = e.KeyModifiers.HasFlag(KeyModifiers.Shift);

        // Handle shortcuts - process even if already handled since we want to override
        switch (e.Key)
        {
            case Key.W when !shiftPressed:
                e.Handled = true;
                await viewModel.CloseCurrentTabAsync();
                break;

            case Key.T when !shiftPressed:
                e.Handled = true;
                await viewModel.CreateNewTabAsync();
                FocusContent();
                break;

            case Key.Tab:
                e.Handled = true;
                if (shiftPressed)
                {
                    viewModel.SelectPreviousTab();
                }
                else
                {
                    viewModel.SelectNextTab();
                }
                break;
        }
    }

    /// <summary>
    /// Focuses the content editor.
    /// </summary>
    public void FocusContent()
    {
        ContentTextBox.Focus();
    }

    /// <summary>
    /// Focuses the title editor.
    /// </summary>
    public void FocusTitle()
    {
        TitleTextBox.Focus();
        TitleTextBox.SelectAll();
    }
}
