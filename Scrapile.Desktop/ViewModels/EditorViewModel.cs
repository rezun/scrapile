namespace Scrapile.Desktop.ViewModels;

using System;
using CommunityToolkit.Mvvm.ComponentModel;
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
    public EditorViewModel(
        TabManager tabManager,
        DocumentService documentService,
        AutoSaveService autoSaveService)
    {
        _tabManager = tabManager ?? throw new ArgumentNullException(nameof(tabManager));
        _documentService = documentService ?? throw new ArgumentNullException(nameof(documentService));
        _autoSaveService = autoSaveService ?? throw new ArgumentNullException(nameof(autoSaveService));
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
                return;
            }

            HasTab = true;
            Content = _currentTab.TabWithStats.Tab.Content;
            Title = _currentTab.TabWithStats.Tab.Document.Title ?? string.Empty;
            IsDirty = _currentTab.TabWithStats.Tab.IsDirty;
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
