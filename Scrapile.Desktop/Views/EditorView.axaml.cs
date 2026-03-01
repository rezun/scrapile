using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaEdit;
using AvaloniaEdit.TextMate;
using TextMateSharp.Grammars;
using Scrapile.Desktop.ViewModels;
using Scrapile.Desktop.Services;
using Scrapile.Domain.Constants;

namespace Scrapile.Desktop.Views;

public partial class EditorView : UserControl
{
    private string? _titleBeforeEdit;
    private EditorViewModel? _currentViewModel;

    // TextMate for syntax highlighting
    private TextMate.Installation? _textMateInstallation;
    private RegistryOptions? _registryOptions;

    public EditorView()
    {
        InitializeComponent();

        // Attach keyboard handlers after the control is fully loaded
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Initialize TextMate for syntax highlighting
        InitializeTextMate();

        // Add padding after line numbers
        ConfigureLineNumberMargin();

        // Handle keyboard shortcuts at the TextEditor level using tunneling
        ContentEditor.AddHandler(KeyDownEvent, OnEditorKeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);
        TitleTextBox.AddHandler(KeyDownEvent, OnTitleKeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);

        // Store the title value when the title TextBox gains focus (for Escape to restore)
        TitleTextBox.GotFocus += OnTitleGotFocus;

        // Subscribe to caret and selection changes from AvaloniaEdit
        ContentEditor.TextArea.Caret.PositionChanged += OnCaretPositionChanged;
        ContentEditor.TextArea.SelectionChanged += OnSelectionChanged;

        // Subscribe to ViewModel events
        if (DataContext is EditorViewModel viewModel)
        {
            SubscribeToViewModel(viewModel);
        }
    }

    private void InitializeTextMate()
    {
        // Get current theme from ThemeService
        var themeName = GetTextMateTheme();
        _registryOptions = new RegistryOptions(themeName);
        _textMateInstallation = ContentEditor.InstallTextMate(_registryOptions);

        // Set link color appropriate for the current theme
        var linkColor = themeName switch
        {
            ThemeName.LightPlus => Avalonia.Media.Color.Parse("#0066CC"),
            _ => Avalonia.Media.Color.Parse("#4FC1FF")
        };
        ContentEditor.TextArea.TextView.LinkTextForegroundBrush = new Avalonia.Media.SolidColorBrush(linkColor);
    }

    private void ConfigureLineNumberMargin()
    {
        // Add left padding to text content for visual separation from line numbers.
        // The negative top margin (-2) compensates for a vertical offset in AvaloniaEdit
        // where line numbers render slightly higher than the corresponding text.
        // This is a known issue - see: https://github.com/AvaloniaUI/AvaloniaEdit/issues/434
        ContentEditor.TextArea.TextView.Margin = new Thickness(6, -1, 0, 0);
    }

    private ThemeName GetTextMateTheme()
    {
        // Try to get the theme service from the app
        if (App.Current is App app)
        {
            var themeService = app.Services?.GetService(typeof(ThemeService)) as ThemeService;
            if (themeService != null)
            {
                return themeService.CurrentTheme switch
                {
                    ThemeValues.Light => ThemeName.LightPlus,
                    ThemeValues.Dark => ThemeName.DarkPlus,
                    _ => ThemeName.DarkPlus // Default to dark
                };
            }
        }
        return ThemeName.DarkPlus;
    }

    private void SetTextMateLanguage(string languageId)
    {
        if (_textMateInstallation == null || _registryOptions == null)
            return;

        if (string.IsNullOrEmpty(languageId) || languageId == "PlainText")
        {
            _textMateInstallation.SetGrammar(null);
            return;
        }

        try
        {
            var extension = GetExtensionForLanguage(languageId);
            var language = _registryOptions.GetLanguageByExtension(extension);
            if (language != null)
            {
                var scope = _registryOptions.GetScopeByLanguageId(language.Id);
                _textMateInstallation.SetGrammar(scope);
            }
        }
        catch
        {
            // If language not found, fall back to plain text
            _textMateInstallation.SetGrammar(null);
        }
    }

    private static string GetExtensionForLanguage(string languageId)
    {
        return languageId switch
        {
            "csharp" => ".cs",
            "javascript" => ".js",
            "typescript" => ".ts",
            "python" => ".py",
            "sql" => ".sql",
            "json" => ".json",
            "xml" => ".xml",
            "html" => ".html",
            "css" => ".css",
            "markdown" => ".md",
            "shellscript" => ".sh",
            "powershell" => ".ps1",
            "go" => ".go",
            "rust" => ".rs",
            "java" => ".java",
            "ruby" => ".rb",
            "php" => ".php",
            "yaml" => ".yaml",
            _ => ".txt"
        };
    }

    private void OnCaretPositionChanged(object? sender, EventArgs e)
    {
        if (DataContext is EditorViewModel vm)
        {
            vm.CaretIndex = ContentEditor.CaretOffset;
        }
    }

    private void OnSelectionChanged(object? sender, EventArgs e)
    {
        if (DataContext is EditorViewModel vm)
        {
            var selection = ContentEditor.TextArea.Selection;
            if (selection.IsEmpty)
            {
                vm.SelectionStart = ContentEditor.CaretOffset;
                vm.SelectionEnd = ContentEditor.CaretOffset;
            }
            else
            {
                // Get the selection range
                var segments = selection.Segments;
                if (segments.Any())
                {
                    var firstSegment = segments.First();
                    var lastSegment = segments.Last();
                    vm.SelectionStart = firstSegment.StartOffset;
                    vm.SelectionEnd = lastSegment.EndOffset;
                }
            }
        }
    }

    private void OnSyntaxLanguageChanged(object? sender, string languageId)
    {
        SetTextMateLanguage(languageId);
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
            _currentViewModel.SyntaxLanguageChanged -= OnSyntaxLanguageChanged;
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
        viewModel.SyntaxLanguageChanged += OnSyntaxLanguageChanged;

        // Apply current language setting
        SetTextMateLanguage(viewModel.SelectedSyntaxLanguage);
    }

    private void OnSelectionRequested(object? sender, SelectionRequestedEventArgs e)
    {
        // Set selection in AvaloniaEdit
        ContentEditor.Select(e.StartPosition, e.Length);

        // Scroll to make the selection visible
        var line = ContentEditor.Document.GetLineByOffset(e.StartPosition);
        ContentEditor.ScrollToLine(line.LineNumber);
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

    private async void OnLanguageSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is EditorViewModel vm && LanguageComboBox.SelectedItem is string selectedLanguage)
        {
            await vm.SetSyntaxLanguageAsync(selectedLanguage);
        }
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
            case Key.Tab when !e.KeyModifiers.HasFlag(KeyModifiers.Shift):
                // Tab: Move focus to content without keyboard traversal focus chrome
                e.Handled = true;
                FocusContent();
                break;

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
            OnEditorKeyDown(sender, e);
        }
    }

    /// <summary>
    /// Intercepts keyboard shortcuts in the editor.
    /// Handles Tab/Shift+Tab for indent/unindent and global shortcuts.
    /// </summary>
    private async void OnEditorKeyDown(object? sender, KeyEventArgs e)
    {
        // Skip if already handled
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

        var shiftPressed = e.KeyModifiers.HasFlag(KeyModifiers.Shift);

        // Handle Tab/Shift+Tab for indent/unindent (no modifier required)
        if (e.Key == Key.Tab && !modifierPressed)
        {
            if (HasMultiLineSelection())
            {
                e.Handled = true;
                if (shiftPressed)
                {
                    UnindentSelectedLines();
                }
                else
                {
                    IndentSelectedLines();
                }
                return;
            }
            // Single line or no selection: let AvaloniaEdit handle it naturally
            return;
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

        // Handle shortcuts
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

    private bool HasMultiLineSelection()
    {
        var selection = ContentEditor.TextArea.Selection;
        if (selection.IsEmpty) return false;

        var segments = selection.Segments;
        if (!segments.Any()) return false;

        var firstSegment = segments.First();
        var lastSegment = segments.Last();

        var startLine = ContentEditor.Document.GetLineByOffset(firstSegment.StartOffset);
        var endLine = ContentEditor.Document.GetLineByOffset(lastSegment.EndOffset);

        return startLine.LineNumber != endLine.LineNumber;
    }

    private void IndentSelectedLines()
    {
        var selection = ContentEditor.TextArea.Selection;
        var segments = selection.Segments.ToList();
        if (!segments.Any()) return;

        var firstSegment = segments.First();
        var lastSegment = segments.Last();

        var startLine = ContentEditor.Document.GetLineByOffset(firstSegment.StartOffset).LineNumber;
        var endLine = ContentEditor.Document.GetLineByOffset(lastSegment.EndOffset).LineNumber;

        // If selection ends at the start of a line, don't include that line
        var endOffset = lastSegment.EndOffset;
        var endLineObj = ContentEditor.Document.GetLineByOffset(endOffset);
        if (endOffset == endLineObj.Offset && endLine > startLine)
        {
            endLine--;
        }

        ContentEditor.Document.BeginUpdate();
        try
        {
            for (int i = startLine; i <= endLine; i++)
            {
                var line = ContentEditor.Document.GetLineByNumber(i);
                ContentEditor.Document.Insert(line.Offset, "\t");
            }
        }
        finally
        {
            ContentEditor.Document.EndUpdate();
        }
    }

    private void UnindentSelectedLines()
    {
        var selection = ContentEditor.TextArea.Selection;
        var segments = selection.Segments.ToList();
        if (!segments.Any()) return;

        var firstSegment = segments.First();
        var lastSegment = segments.Last();

        var startLine = ContentEditor.Document.GetLineByOffset(firstSegment.StartOffset).LineNumber;
        var endLine = ContentEditor.Document.GetLineByOffset(lastSegment.EndOffset).LineNumber;

        // If selection ends at the start of a line, don't include that line
        var endOffset = lastSegment.EndOffset;
        var endLineObj = ContentEditor.Document.GetLineByOffset(endOffset);
        if (endOffset == endLineObj.Offset && endLine > startLine)
        {
            endLine--;
        }

        ContentEditor.Document.BeginUpdate();
        try
        {
            for (int i = startLine; i <= endLine; i++)
            {
                var line = ContentEditor.Document.GetLineByNumber(i);
                if (line.Length == 0) continue;

                var firstChar = ContentEditor.Document.GetCharAt(line.Offset);
                if (firstChar == '\t')
                {
                    ContentEditor.Document.Remove(line.Offset, 1);
                }
                else if (firstChar == ' ')
                {
                    // Remove up to 4 spaces
                    int spacesToRemove = 0;
                    for (int j = 0; j < Math.Min(4, line.Length); j++)
                    {
                        if (ContentEditor.Document.GetCharAt(line.Offset + j) == ' ')
                            spacesToRemove++;
                        else
                            break;
                    }
                    if (spacesToRemove > 0)
                        ContentEditor.Document.Remove(line.Offset, spacesToRemove);
                }
            }
        }
        finally
        {
            ContentEditor.Document.EndUpdate();
        }
    }

    /// <summary>
    /// Focuses the content editor.
    /// </summary>
    public void FocusContent()
    {
        ContentEditor.TextArea.Focus();
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

        // Dispose TextMate installation
        _textMateInstallation?.Dispose();

        // Unsubscribe from caret/selection events
        if (ContentEditor != null)
        {
            ContentEditor.TextArea.Caret.PositionChanged -= OnCaretPositionChanged;
            ContentEditor.TextArea.SelectionChanged -= OnSelectionChanged;
        }

        // Unsubscribe from view model events
        if (_currentViewModel != null)
        {
            _currentViewModel.SelectionRequested -= OnSelectionRequested;
            _currentViewModel.FocusFindBarRequested -= OnFocusFindBarRequested;
            _currentViewModel.SyntaxLanguageChanged -= OnSyntaxLanguageChanged;
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
