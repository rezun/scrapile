namespace Scrapile.Desktop.ViewModels;

using System;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Scrapile.Application.Helpers;
using Scrapile.Application.Services;

/// <summary>
/// ViewModel for the text editor component.
/// Handles binding to the current tab's content and title.
/// </summary>
public partial class EditorViewModel : ViewModelBase
{
    private readonly TabManager _tabManager;
    private readonly DocumentService _documentService;
    private readonly AutoSaveService _autoSaveService;
    private readonly SettingsService _settingsService;

    private TabItemViewModel? _currentTab;
    private bool _isUpdatingFromTab;

    [ObservableProperty]
    private string _content = string.Empty;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private bool _hasTab;

    [ObservableProperty]
    private bool _isDirty;

    [ObservableProperty]
    private string _saveStatus = string.Empty;

    [ObservableProperty]
    private FontFamily _editorFontFamily = new FontFamily("Consolas, Menlo, Monaco, monospace");

    [ObservableProperty]
    private double _editorFontSize = 14;

    [ObservableProperty]
    private int _caretIndex;

    [ObservableProperty]
    private int _selectionStart;

    [ObservableProperty]
    private int _selectionEnd;

    [ObservableProperty]
    private string _statusText = string.Empty;

    [ObservableProperty]
    private string _cursorPositionText = string.Empty;

    [ObservableProperty]
    private string _selectionText = string.Empty;

    [ObservableProperty]
    private bool _hasSelection;

    /// <summary>
    /// Event raised when content changes for auto-save purposes.
    /// </summary>
    public event EventHandler<ContentChangedEventArgs>? ContentChanged;

    /// <summary>
    /// Event raised when the title changes.
    /// </summary>
    public event EventHandler<TitleChangedEventArgs>? TitleChanged;

    /// <summary>
    /// Creates a new EditorViewModel.
    /// </summary>
    /// <param name="tabManager">The tab manager service.</param>
    /// <param name="documentService">The document service.</param>
    /// <param name="autoSaveService">The auto-save service.</param>
    /// <param name="settingsService">The settings service.</param>
    public EditorViewModel(
        TabManager tabManager,
        DocumentService documentService,
        AutoSaveService autoSaveService,
        SettingsService settingsService)
    {
        _tabManager = tabManager ?? throw new ArgumentNullException(nameof(tabManager));
        _documentService = documentService ?? throw new ArgumentNullException(nameof(documentService));
        _autoSaveService = autoSaveService ?? throw new ArgumentNullException(nameof(autoSaveService));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));

        // Subscribe to settings changes
        _settingsService.SettingsChanged += OnSettingsChanged;

        // Apply initial font settings
        ApplyFontSettings();
    }

    /// <summary>
    /// Handles settings change events to update font properties.
    /// </summary>
    private void OnSettingsChanged(object? sender, SettingsChangedEventArgs e)
    {
        if (e.SettingName == "FontFamily" || e.SettingName == "FontSize" || e.SettingName == "All")
        {
            ApplyFontSettings();
        }
    }

    /// <summary>
    /// Applies the current font settings from the settings service.
    /// </summary>
    private void ApplyFontSettings()
    {
        var fontFamily = _settingsService.GetFontFamily();
        if (string.IsNullOrWhiteSpace(fontFamily))
        {
            EditorFontFamily = new FontFamily("Consolas, Menlo, Monaco, monospace");
        }
        else
        {
            // Use the specified font with fallbacks
            EditorFontFamily = new FontFamily($"{fontFamily}, Consolas, Menlo, Monaco, monospace");
        }

        EditorFontSize = _settingsService.GetFontSize();
    }

    /// <summary>
    /// The current tab being edited, or null if no tab is selected.
    /// </summary>
    public TabItemViewModel? CurrentTab
    {
        get => _currentTab;
        set
        {
            if (_currentTab == value) return;

            _currentTab = value;
            OnPropertyChanged();
            LoadTabContent();
        }
    }

    /// <summary>
    /// Loads the content from the current tab into the editor.
    /// </summary>
    private void LoadTabContent()
    {
        _isUpdatingFromTab = true;
        try
        {
            if (_currentTab == null)
            {
                Content = string.Empty;
                Title = string.Empty;
                HasTab = false;
                IsDirty = false;
                CaretIndex = 0;
                SelectionStart = 0;
                SelectionEnd = 0;
                UpdateStatusBarProperties();
                return;
            }

            HasTab = true;
            Content = _currentTab.TabWithStats.Tab.Content;
            Title = _currentTab.TabWithStats.Tab.Document.Title ?? string.Empty;
            IsDirty = _currentTab.TabWithStats.Tab.IsDirty;

            // Reset caret and selection for new tab
            CaretIndex = 0;
            SelectionStart = 0;
            SelectionEnd = 0;
            UpdateStatusBarProperties();
        }
        finally
        {
            _isUpdatingFromTab = false;
        }
    }

    /// <summary>
    /// Called when the Content property changes.
    /// Updates the underlying tab content and triggers auto-save.
    /// </summary>
    partial void OnContentChanged(string value)
    {
        // Always update status bar when content changes
        UpdateStatusBarProperties();

        if (_isUpdatingFromTab || _currentTab == null) return;

        // Update the tab's content
        _tabManager.UpdateTabContent(_currentTab.TabId, value);
        IsDirty = true;

        // Raise event for auto-save
        ContentChanged?.Invoke(this, new ContentChangedEventArgs(_currentTab.DocumentId, value));
    }

    /// <summary>
    /// Called when the Title property changes.
    /// Updates the underlying document title.
    /// </summary>
    partial void OnTitleChanged(string value)
    {
        if (_isUpdatingFromTab || _currentTab == null) return;

        // Normalize empty/whitespace to null
        var normalizedTitle = string.IsNullOrWhiteSpace(value) ? null : value;

        // Raise event for title update
        TitleChanged?.Invoke(this, new TitleChangedEventArgs(_currentTab.DocumentId, normalizedTitle));
    }

    /// <summary>
    /// Refreshes the editor content from the current tab (e.g., after external changes).
    /// </summary>
    public void RefreshFromTab()
    {
        LoadTabContent();
    }

    /// <summary>
    /// Updates the dirty state.
    /// </summary>
    public void SetDirty(bool isDirty)
    {
        IsDirty = isDirty;
    }

    /// <summary>
    /// Updates all status bar properties based on current content and cursor position.
    /// </summary>
    private void UpdateStatusBarProperties()
    {
        if (!HasTab)
        {
            StatusText = string.Empty;
            CursorPositionText = string.Empty;
            SelectionText = string.Empty;
            HasSelection = false;
            return;
        }

        // Calculate content stats
        var words = ContentHelper.CountWords(Content);
        var chars = ContentHelper.CountCharacters(Content);
        var lines = ContentHelper.CountLines(Content);
        StatusText = $"{ContentHelper.FormatCount(words)} words  {ContentHelper.FormatCount(chars)} chars  {ContentHelper.FormatCount(lines)} lines";

        // Calculate cursor position (1-based)
        var (line, col) = CalculateCursorPosition(Content, CaretIndex);
        CursorPositionText = $"Ln {line}, Col {col}";

        // Calculate selection stats if any
        var selLength = Math.Abs(SelectionEnd - SelectionStart);
        HasSelection = selLength > 0;
        if (HasSelection)
        {
            var selStart = Math.Min(SelectionStart, SelectionEnd);
            var safeSelLength = Math.Min(selLength, Content.Length - selStart);
            if (selStart >= 0 && selStart < Content.Length && safeSelLength > 0)
            {
                var selectedText = Content.Substring(selStart, safeSelLength);
                var selWords = ContentHelper.CountWords(selectedText);
                var selChars = ContentHelper.CountCharacters(selectedText);
                SelectionText = $"Sel: {ContentHelper.FormatCount(selWords)} words, {ContentHelper.FormatCount(selChars)} chars";
            }
            else
            {
                SelectionText = string.Empty;
                HasSelection = false;
            }
        }
        else
        {
            SelectionText = string.Empty;
        }
    }

    /// <summary>
    /// Calculates the line and column position from a caret index.
    /// </summary>
    private static (int line, int column) CalculateCursorPosition(string content, int caretIndex)
    {
        if (string.IsNullOrEmpty(content) || caretIndex <= 0)
            return (1, 1);

        int line = 1, col = 1;
        for (int i = 0; i < Math.Min(caretIndex, content.Length); i++)
        {
            if (content[i] == '\n')
            {
                line++;
                col = 1;
            }
            else
            {
                col++;
            }
        }
        return (line, col);
    }

    partial void OnCaretIndexChanged(int value)
    {
        UpdateStatusBarProperties();
    }

    partial void OnSelectionStartChanged(int value)
    {
        UpdateStatusBarProperties();
    }

    partial void OnSelectionEndChanged(int value)
    {
        UpdateStatusBarProperties();
    }
}

/// <summary>
/// Event args for content changes.
/// </summary>
public class ContentChangedEventArgs : EventArgs
{
    public Guid DocumentId { get; }
    public string Content { get; }

    public ContentChangedEventArgs(Guid documentId, string content)
    {
        DocumentId = documentId;
        Content = content;
    }
}

/// <summary>
/// Event args for title changes.
/// </summary>
public class TitleChangedEventArgs : EventArgs
{
    public Guid DocumentId { get; }
    public string? Title { get; }

    public TitleChangedEventArgs(Guid documentId, string? title)
    {
        DocumentId = documentId;
        Title = title;
    }
}
