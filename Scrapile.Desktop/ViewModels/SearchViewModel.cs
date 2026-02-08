namespace Scrapile.Desktop.ViewModels;

using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Scrapile.Application.Services;

/// <summary>
/// ViewModel for the search overlay/modal.
/// Provides real-time search as the user types.
/// </summary>
public partial class SearchViewModel : ViewModelBase
{
    private readonly DocumentService _documentService;
    private CancellationTokenSource? _searchCts;

    /// <summary>
    /// Debounce delay for search (in milliseconds).
    /// </summary>
    private const int SearchDebounceDelayMs = 100;

    /// <summary>
    /// Maximum number of results to display.
    /// </summary>
    private const int MaxResults = 50;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private bool _isSearching;

    [ObservableProperty]
    private bool _hasResults;

    [ObservableProperty]
    private bool _showNoResults;

    [ObservableProperty]
    private int _selectedIndex = -1;

    /// <summary>
    /// The collection of search results.
    /// </summary>
    public ObservableCollection<SearchResultItemViewModel> Results { get; } = new();

    /// <summary>
    /// Event raised when a result is selected and should be opened.
    /// </summary>
    public event EventHandler<SearchResultItemViewModel>? ResultSelected;

    /// <summary>
    /// Event raised when the search overlay should be closed.
    /// </summary>
    public event EventHandler? CloseRequested;

    /// <summary>
    /// Creates a new SearchViewModel.
    /// </summary>
    /// <param name="documentService">The document service for searching.</param>
    public SearchViewModel(DocumentService documentService)
    {
        _documentService = documentService ?? throw new ArgumentNullException(nameof(documentService));
    }

    /// <summary>
    /// Called when the SearchQuery property changes.
    /// Triggers a debounced search.
    /// </summary>
    partial void OnSearchQueryChanged(string value)
    {
        _ = SearchAsync(value);
    }

    /// <summary>
    /// Called when the SelectedIndex property changes.
    /// Updates the IsSelected state of result items.
    /// </summary>
    partial void OnSelectedIndexChanged(int value)
    {
        UpdateSelectionState();
    }

    /// <summary>
    /// Performs a debounced search with the given query.
    /// </summary>
    private async Task SearchAsync(string query)
    {
        // Cancel any pending search
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        // Clear results for empty query
        if (string.IsNullOrWhiteSpace(query))
        {
            Results.Clear();
            HasResults = false;
            ShowNoResults = false;
            SelectedIndex = -1;
            return;
        }

        try
        {
            // Debounce
            await Task.Delay(SearchDebounceDelayMs, token);
            if (token.IsCancellationRequested) return;

            IsSearching = true;

            // Perform the search
            var results = await _documentService.SearchAsync(query);
            if (token.IsCancellationRequested) return;

            // Update results (reset index first so re-selecting 0 triggers a change)
            SelectedIndex = -1;
            Results.Clear();
            var count = 0;
            foreach (var doc in results)
            {
                if (count >= MaxResults) break;
                Results.Add(new SearchResultItemViewModel(doc));
                count++;
            }

            HasResults = Results.Count > 0;
            ShowNoResults = Results.Count == 0;

            // Select first result by default
            SelectedIndex = HasResults ? 0 : -1;
        }
        catch (OperationCanceledException)
        {
            // Expected when search is cancelled
        }
        finally
        {
            IsSearching = false;
        }
    }

    /// <summary>
    /// Updates the IsSelected state of all result items based on SelectedIndex.
    /// </summary>
    private void UpdateSelectionState()
    {
        for (int i = 0; i < Results.Count; i++)
        {
            Results[i].IsSelected = (i == SelectedIndex);
        }
    }

    /// <summary>
    /// Moves the selection to the next result.
    /// </summary>
    public void SelectNext()
    {
        if (Results.Count == 0) return;
        SelectedIndex = (SelectedIndex + 1) % Results.Count;
    }

    /// <summary>
    /// Moves the selection to the previous result.
    /// </summary>
    public void SelectPrevious()
    {
        if (Results.Count == 0) return;
        SelectedIndex = SelectedIndex <= 0 ? Results.Count - 1 : SelectedIndex - 1;
    }

    /// <summary>
    /// Opens the currently selected result.
    /// </summary>
    public void OpenSelectedResult()
    {
        if (SelectedIndex >= 0 && SelectedIndex < Results.Count)
        {
            var selected = Results[SelectedIndex];
            ResultSelected?.Invoke(this, selected);
        }
    }

    /// <summary>
    /// Opens a specific result.
    /// </summary>
    /// <param name="result">The result to open.</param>
    public void OpenResult(SearchResultItemViewModel result)
    {
        ResultSelected?.Invoke(this, result);
    }

    /// <summary>
    /// Requests the search overlay to close.
    /// </summary>
    public void RequestClose()
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Resets the search state.
    /// Call this when opening the search overlay.
    /// </summary>
    public void Reset()
    {
        SearchQuery = string.Empty;
        Results.Clear();
        HasResults = false;
        ShowNoResults = false;
        SelectedIndex = -1;
        IsSearching = false;
    }
}
