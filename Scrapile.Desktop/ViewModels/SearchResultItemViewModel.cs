namespace Scrapile.Desktop.ViewModels;

using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Scrapile.Application.DTOs;

/// <summary>
/// ViewModel for an individual search result item.
/// </summary>
public partial class SearchResultItemViewModel : ViewModelBase
{
    private readonly DocumentWithStats _documentWithStats;

    [ObservableProperty]
    private bool _isSelected;

    /// <summary>
    /// Creates a new SearchResultItemViewModel.
    /// </summary>
    /// <param name="documentWithStats">The document data with stats.</param>
    public SearchResultItemViewModel(DocumentWithStats documentWithStats)
    {
        _documentWithStats = documentWithStats ?? throw new ArgumentNullException(nameof(documentWithStats));
    }

    /// <summary>
    /// The document ID.
    /// </summary>
    public Guid DocumentId => _documentWithStats.Document.Id;

    /// <summary>
    /// The display name (title if set, otherwise content preview).
    /// </summary>
    public string DisplayName => _documentWithStats.DisplayName;

    /// <summary>
    /// Whether this document has a user-provided title.
    /// </summary>
    public bool HasTitle => _documentWithStats.Document.HasTitle;

    /// <summary>
    /// A snippet of the document content (used as secondary line).
    /// </summary>
    public string ContentSnippet => _documentWithStats.ContentPreview;

    /// <summary>
    /// The formatted last modified date.
    /// </summary>
    public string LastModified => FormatLastModified(_documentWithStats.Document.LastModified);

    /// <summary>
    /// The full document content (for preview pane).
    /// </summary>
    public string FullContent => _documentWithStats.Document.Content;

    /// <summary>
    /// The underlying DocumentWithStats.
    /// </summary>
    public DocumentWithStats DocumentWithStats => _documentWithStats;

    /// <summary>
    /// Formats the last modified date as a human-readable string.
    /// </summary>
    private static string FormatLastModified(DateTime lastModified)
    {
        var now = DateTime.Now;
        var diff = now - lastModified;

        if (diff.TotalMinutes < 1)
        {
            return "Just now";
        }
        if (diff.TotalMinutes < 60)
        {
            var minutes = (int)diff.TotalMinutes;
            return minutes == 1 ? "1 minute ago" : $"{minutes} minutes ago";
        }
        if (diff.TotalHours < 24)
        {
            var hours = (int)diff.TotalHours;
            return hours == 1 ? "1 hour ago" : $"{hours} hours ago";
        }
        if (diff.TotalDays < 2)
        {
            return "Yesterday";
        }
        if (diff.TotalDays < 7)
        {
            var days = (int)diff.TotalDays;
            return $"{days} days ago";
        }

        // For older dates, show the actual date
        return lastModified.ToString("MMM d, yyyy");
    }
}
