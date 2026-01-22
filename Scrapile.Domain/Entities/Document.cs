namespace Scrapile.Domain.Entities;

/// <summary>
/// Represents a document stored in the scratchpad.
/// Titles are optional - untitled documents display a content preview instead.
/// </summary>
public class Document
{
    public Guid Id { get; set; }

    /// <summary>
    /// The filename on disk (e.g., "20250122143022_a3f5b2e1.txt")
    /// </summary>
    public string Filename { get; set; } = string.Empty;

    /// <summary>
    /// User-provided title. Null means no title (content preview mode).
    /// Empty string is treated as no title.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// The document content. May be loaded on-demand for performance.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// File creation timestamp (from file system).
    /// </summary>
    public DateTime Created { get; set; }

    /// <summary>
    /// Last modification timestamp (from file system).
    /// </summary>
    public DateTime LastModified { get; set; }

    /// <summary>
    /// Whether this document has a user-provided title.
    /// </summary>
    public bool HasTitle => !string.IsNullOrWhiteSpace(Title);
}
