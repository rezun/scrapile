using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Scrapile.Desktop.ViewModels;

namespace Scrapile.Desktop.Views;

public partial class EditorView : UserControl
{
    private string? _titleBeforeEdit;
    private IDisposable? _caretSubscription;
    private IDisposable? _selectionStartSubscription;
    private IDisposable? _selectionEndSubscription;
    private EditorViewModel? _currentViewModel;

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
        TitleTextBox.AddHandler(KeyDownEvent, OnTitleKeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);

        // Store the title value when the title TextBox gains focus (for Escape to restore)
        TitleTextBox.GotFocus += OnTitleGotFocus;

        // Subscribe to caret and selection changes to update ViewModel
        _caretSubscription = ContentTextBox.GetObservable(TextBox.CaretIndexProperty)
            .Subscribe(new ActionObserver<int>(index => { if (DataContext is EditorViewModel vm) vm.CaretIndex = index; }));
        _selectionStartSubscription = ContentTextBox.GetObservable(TextBox.SelectionStartProperty)
            .Subscribe(new ActionObserver<int>(index => { if (DataContext is EditorViewModel vm) vm.SelectionStart = index; }));
        _selectionEndSubscription = ContentTextBox.GetObservable(TextBox.SelectionEndProperty)
            .Subscribe(new ActionObserver<int>(index => { if (DataContext is EditorViewModel vm) vm.SelectionEnd = index; }));

        // Subscribe to selection and focus events from ViewModel
        if (DataContext is EditorViewModel viewModel)
        {
            SubscribeToViewModel(viewModel);
        }
    }

    /// <inheritdoc/>
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        // Unsubscribe from old view model
        if (_currentViewModel != null)
        {
            _currentViewModel.SelectionRequested -= OnSelectionRequested;
            _currentViewModel.FocusFindBarRequested -= OnFocusFindBarRequested;
        }

        // Subscribe to new view model
        if (DataContext is EditorViewModel viewModel)
        {
            SubscribeToViewModel(viewModel);
        }
    }

    private void SubscribeToViewModel(EditorViewModel viewModel)
    {
        // Avoid double-subscribing
        if (_currentViewModel == viewModel)
        {
            return;
        }

        _currentViewModel = viewModel;
        viewModel.SelectionRequested += OnSelectionRequested;
        viewModel.FocusFindBarRequested += OnFocusFindBarRequested;
    }

    private void OnSelectionRequested(object? sender, SelectionRequestedEventArgs e)
    {
        // Clear selection first by setting both to the same value
        ContentTextBox.SelectionStart = e.StartPosition;
        ContentTextBox.SelectionEnd = e.StartPosition;

        // Now set the actual selection end
        ContentTextBox.SelectionEnd = e.StartPosition + e.Length;
    }

    private void OnFocusFindBarRequested(object? sender, EventArgs e)
    {
        // Use dispatcher to ensure the find bar is rendered before focusing
        Dispatcher.UIThread.Post(() =>
        {
            FindBar?.FocusFindInput();
        }, DispatcherPriority.Loaded);
    }

    private async void OnWordWrapTextPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is EditorViewModel vm)
        {
            await vm.CycleWordWrapAsync();
        }
    }

    /// <summary>
    /// Simple observer that calls an action on each value.
    /// </summary>
    private class ActionObserver<T> : IObserver<T>
    {
        private readonly Action<T> _action;
        public ActionObserver(Action<T> action) => _action = action;
        public void OnCompleted() { }
        public void OnError(Exception error) { }
        public void OnNext(T value) => _action(value);
    }

    private void OnTitleGotFocus(object? sender, GotFocusEventArgs e)
    {
        _titleBeforeEdit = TitleTextBox.Text;
    }

    /// <summary>
    /// Handles keyboard events in the title TextBox.
    /// Enter saves and moves focus to content, Escape cancels and restores original value.
    /// </summary>
    private void OnTitleKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Handled)
        {
            return;
        }

        switch (e.Key)
        {
            case Key.Enter:
                // Enter: Save title (already bound) and move focus to content
                e.Handled = true;
                FocusContent();
                break;

            case Key.Escape:
                // Escape: Cancel edit and restore original value
                e.Handled = true;
                TitleTextBox.Text = _titleBeforeEdit;
                FocusContent();
                break;
        }

        // If not Enter or Escape, check for global shortcuts
        if (!e.Handled)
        {
            OnTextBoxKeyDown(sender, e);
        }
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

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        _caretSubscription?.Dispose();
        _selectionStartSubscription?.Dispose();
        _selectionEndSubscription?.Dispose();

        // Unsubscribe from view model events
        if (_currentViewModel != null)
        {
            _currentViewModel.SelectionRequested -= OnSelectionRequested;
            _currentViewModel.FocusFindBarRequested -= OnFocusFindBarRequested;
            _currentViewModel = null;
        }
    }

    /// <summary>
    /// Focuses the find bar input.
    /// </summary>
    public void FocusFindBar()
    {
        FindBar?.FocusFindInput();
    }
}
