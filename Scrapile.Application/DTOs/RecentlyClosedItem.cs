namespace Scrapile.Application.DTOs;

/// <summary>
/// Represents a recently closed document with display information.
/// Used for the "reopen closed tab" feature UI.
/// </summary>
public class RecentlyClosedItem
{
    /// <summary>
    /// The ID of the closed document.
    /// </summary>
    public Guid DocumentId { get; init; }

    /// <summary>
    /// When the tab was closed.
    /// </summary>
    public DateTime ClosedAt { get; init; }

    /// <summary>
    /// The document title, or null if untitled.
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// Content preview (first ~40 characters) for untitled documents.
    /// </summary>
    public string ContentPreview { get; init; } = string.Empty;

    /// <summary>
    /// The display name (title if set, otherwise content preview).
    /// </summary>
    public string DisplayName => !string.IsNullOrWhiteSpace(Title) ? Title : ContentPreview;

    /// <summary>
    /// Whether the document has been deleted from disk.
    /// If true, the document cannot be reopened.
    /// </summary>
    public bool IsDeleted { get; init; }

    /// <summary>
    /// Formatted time since closed for display (e.g., "2 minutes ago", "1 hour ago").
    /// </summary>
    public string FormattedClosedTime { get; init; } = string.Empty;
}
