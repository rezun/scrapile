namespace Scrapile.Domain.Entities;

/// <summary>
/// Represents information about an open tab stored in metadata.
/// Used for session persistence.
/// </summary>
public class OpenTabInfo
{
    /// <summary>
    /// The ID of the document open in this tab.
    /// </summary>
    public Guid DocumentId { get; set; }

    /// <summary>
    /// The position of this tab in the tab list (0-based).
    /// </summary>
    public int Order { get; set; }
}
