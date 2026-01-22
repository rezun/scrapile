namespace Scrapile.Domain.Entities;

/// <summary>
/// Metadata for a document that cannot be derived from the file system.
/// Currently only stores user-provided title.
/// </summary>
public class DocumentMetadata
{
    /// <summary>
    /// User-provided title. Null means no title (content preview mode).
    /// </summary>
    public string? Title { get; set; }
}
