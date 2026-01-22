namespace Scrapile.Domain.Entities;

/// <summary>
/// Represents an open tab in the application.
/// Each tab is associated with a document and maintains in-memory editing state.
/// </summary>
public class Tab
{
    public Guid TabId { get; set; }

    /// <summary>
    /// The document associated with this tab.
    /// </summary>
    public Document Document { get; set; } = null!;

    /// <summary>
    /// In-memory content being edited. May differ from saved Document.Content.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Position in the tab list (0-based).
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Indicates whether the tab has unsaved changes.
    /// </summary>
    public bool IsDirty { get; set; }
}
