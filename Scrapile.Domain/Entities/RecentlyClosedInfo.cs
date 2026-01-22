namespace Scrapile.Domain.Entities;

/// <summary>
/// Represents information about a recently closed document.
/// Used for the "reopen closed tab" feature.
/// </summary>
public class RecentlyClosedInfo
{
    /// <summary>
    /// The ID of the closed document.
    /// </summary>
    public Guid DocumentId { get; set; }

    /// <summary>
    /// When the tab was closed.
    /// </summary>
    public DateTime ClosedAt { get; set; }
}
