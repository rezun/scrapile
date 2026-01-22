namespace Scrapile.Domain.Entities;

/// <summary>
/// Root metadata object stored in .ephemeral_metadata.json.
/// Contains only data that cannot be derived from the file system.
/// </summary>
public class Metadata
{
    /// <summary>
    /// Metadata file format version.
    /// </summary>
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// List of currently open tabs with their order.
    /// </summary>
    public List<OpenTabInfo> OpenTabs { get; set; } = new();

    /// <summary>
    /// Stack of recently closed documents (most recent first).
    /// Limited to 50 items.
    /// </summary>
    public List<RecentlyClosedInfo> RecentlyClosed { get; set; } = new();

    /// <summary>
    /// Document-specific metadata indexed by document ID.
    /// Only contains metadata that cannot be derived from files.
    /// </summary>
    public Dictionary<Guid, DocumentMetadata> Documents { get; set; } = new();

    /// <summary>
    /// User's preferred theme setting.
    /// Valid values: "Light", "Dark", "System".
    /// </summary>
    public string Theme { get; set; } = "System";
}
