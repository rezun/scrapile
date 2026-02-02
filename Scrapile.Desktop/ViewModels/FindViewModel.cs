namespace Scrapile.Desktop.ViewModels;

using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

/// <summary>
/// ViewModel for the find bar, handling in-document search logic.
/// </summary>
public partial class FindViewModel : ViewModelBase
{
    private List<int> _matchPositions = new();
    private string _lastSearchedContent = string.Empty;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private int _currentMatchIndex;

    [ObservableProperty]
    private int _totalMatches;

    [ObservableProperty]
    private string _matchStatusText = string.Empty;

    /// <summary>
    /// Event raised when navigation to a match is requested.
    /// The event args contain the start position and length of the match.
    /// </summary>
    public event EventHandler<NavigateToMatchEventArgs>? NavigateToMatch;

    /// <summary>
    /// Event raised when the find bar should be closed.
    /// </summary>
    public event EventHandler? CloseRequested;

    /// <summary>
    /// Searches the given content for all occurrences of the search query.
    /// </summary>
    /// <param name="content">The document content to search.</param>
    public void Search(string content)
    {
        _lastSearchedContent = content;
        _matchPositions.Clear();

        if (string.IsNullOrEmpty(SearchQuery) || string.IsNullOrEmpty(content))
        {
            TotalMatches = 0;
            CurrentMatchIndex = 0;
            UpdateMatchStatusText();
            return;
        }

        // Case-insensitive search
        var searchLower = SearchQuery.ToLowerInvariant();
        var contentLower = content.ToLowerInvariant();
        var index = 0;

        while ((index = contentLower.IndexOf(searchLower, index, StringComparison.Ordinal)) != -1)
        {
            _matchPositions.Add(index);
            index += searchLower.Length;
        }

        TotalMatches = _matchPositions.Count;

        if (TotalMatches > 0)
        {
            CurrentMatchIndex = 1;
            NavigateToCurrentMatch();
        }
        else
        {
            CurrentMatchIndex = 0;
        }

        UpdateMatchStatusText();
    }

    /// <summary>
    /// Navigates to the next match, wrapping around if at the end.
    /// </summary>
    public void FindNext()
    {
        if (TotalMatches == 0)
        {
            return;
        }

        CurrentMatchIndex++;
        if (CurrentMatchIndex > TotalMatches)
        {
            CurrentMatchIndex = 1;
        }

        NavigateToCurrentMatch();
        UpdateMatchStatusText();
    }

    /// <summary>
    /// Navigates to the previous match, wrapping around if at the beginning.
    /// </summary>
    public void FindPrevious()
    {
        if (TotalMatches == 0)
        {
            return;
        }

        CurrentMatchIndex--;
        if (CurrentMatchIndex < 1)
        {
            CurrentMatchIndex = TotalMatches;
        }

        NavigateToCurrentMatch();
        UpdateMatchStatusText();
    }

    /// <summary>
    /// Requests to close the find bar.
    /// </summary>
    public void RequestClose()
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Clears the search state.
    /// </summary>
    public void Clear()
    {
        SearchQuery = string.Empty;
        _matchPositions.Clear();
        TotalMatches = 0;
        CurrentMatchIndex = 0;
        _lastSearchedContent = string.Empty;
        UpdateMatchStatusText();
    }

    /// <summary>
    /// Updates the match status text based on current state.
    /// </summary>
    private void UpdateMatchStatusText()
    {
        if (string.IsNullOrEmpty(SearchQuery))
        {
            MatchStatusText = string.Empty;
        }
        else if (TotalMatches == 0)
        {
            MatchStatusText = "No results";
        }
        else
        {
            MatchStatusText = $"{CurrentMatchIndex} of {TotalMatches}";
        }
    }

    /// <summary>
    /// Raises the NavigateToMatch event for the current match.
    /// </summary>
    private void NavigateToCurrentMatch()
    {
        if (CurrentMatchIndex < 1 || CurrentMatchIndex > _matchPositions.Count)
        {
            return;
        }

        var position = _matchPositions[CurrentMatchIndex - 1];
        NavigateToMatch?.Invoke(this, new NavigateToMatchEventArgs(position, SearchQuery.Length));
    }

    partial void OnSearchQueryChanged(string value)
    {
        // Re-search when query changes
        Search(_lastSearchedContent);
    }
}

/// <summary>
/// Event args for navigating to a match.
/// </summary>
public class NavigateToMatchEventArgs : EventArgs
{
    /// <summary>
    /// The start position of the match in the content.
    /// </summary>
    public int StartPosition { get; }

    /// <summary>
    /// The length of the match.
    /// </summary>
    public int Length { get; }

    public NavigateToMatchEventArgs(int startPosition, int length)
    {
        StartPosition = startPosition;
        Length = length;
    }
}
