namespace Scrapile.Application.DTOs;

using Scrapile.Domain.Entities;

/// <summary>
/// Represents a document with calculated statistics.
/// Stats are computed on-demand rather than stored.
/// </summary>
public class DocumentWithStats
{
    /// <summary>
    /// The underlying document entity.
    /// </summary>
    public Document Document { get; init; } = null!;

    /// <summary>
    /// Word count calculated from content.
    /// </summary>
    public int WordCount { get; init; }

    /// <summary>
    /// Character count calculated from content.
    /// </summary>
    public int CharacterCount { get; init; }

    /// <summary>
    /// Content preview (first ~40 characters).
    /// </summary>
    public string ContentPreview { get; init; } = string.Empty;

    /// <summary>
    /// Formatted word count for display (e.g., "245 words" or "1.5k words").
    /// </summary>
    public string FormattedWordCount { get; init; } = string.Empty;

    /// <summary>
    /// Formatted character count for display (e.g., "1.5k chars").
    /// </summary>
    public string FormattedCharacterCount { get; init; } = string.Empty;

    /// <summary>
    /// The display name for the document (title if set, otherwise content preview).
    /// </summary>
    public string DisplayName => Document.HasTitle ? Document.Title! : ContentPreview;
}
