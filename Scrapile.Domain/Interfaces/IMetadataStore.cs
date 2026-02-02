namespace Scrapile.Domain.Interfaces;

using Scrapile.Domain.Entities;

/// <summary>
/// Interface for metadata storage operations.
/// Metadata includes open tabs, recently closed documents, and document titles.
/// Implementations handle persistence (JSON file, database, etc.).
/// </summary>
public interface IMetadataStore
{
    /// <summary>
    /// Loads the metadata from storage.
    /// Creates default metadata if none exists.
    /// </summary>
    /// <returns>The loaded or default metadata.</returns>
    Task<Metadata> LoadAsync();

    /// <summary>
    /// Saves the entire metadata object to storage.
    /// </summary>
    /// <param name="metadata">The metadata to save.</param>
    Task SaveAsync(Metadata metadata);

    /// <summary>
    /// Adds a new document to the metadata.
    /// </summary>
    /// <param name="documentId">The document ID.</param>
    /// <param name="title">Optional title for the document.</param>
    Task AddDocumentAsync(Guid documentId, string? title);

    /// <summary>
    /// Updates the title of a document in metadata.
    /// </summary>
    /// <param name="documentId">The document ID.</param>
    /// <param name="title">The new title, or null to remove the title.</param>
    Task UpdateDocumentTitleAsync(Guid documentId, string? title);

    /// <summary>
    /// Removes a document from metadata.
    /// </summary>
    /// <param name="documentId">The document ID to remove.</param>
    Task RemoveDocumentAsync(Guid documentId);

    /// <summary>
    /// Adds a tab to the open tabs list.
    /// </summary>
    /// <param name="documentId">The document ID for the tab.</param>
    /// <param name="order">The position in the tab list.</param>
    Task AddOpenTabAsync(Guid documentId, int order);

    /// <summary>
    /// Removes a tab from the open tabs list.
    /// </summary>
    /// <param name="documentId">The document ID to remove from open tabs.</param>
    Task RemoveOpenTabAsync(Guid documentId);

    /// <summary>
    /// Updates the order of open tabs.
    /// </summary>
    /// <param name="orderedDocumentIds">Document IDs in their new order.</param>
    Task UpdateOpenTabsOrderAsync(IEnumerable<Guid> orderedDocumentIds);

    /// <summary>
    /// Adds a document to the recently closed list.
    /// Maintains the limit of 50 items (removes oldest if exceeded).
    /// </summary>
    /// <param name="documentId">The document ID.</param>
    /// <param name="closedAt">When the tab was closed.</param>
    Task AddRecentlyClosedAsync(Guid documentId, DateTime closedAt);

    /// <summary>
    /// Removes a document from the recently closed list.
    /// Called when a document is reopened.
    /// </summary>
    /// <param name="documentId">The document ID to remove.</param>
    Task RemoveRecentlyClosedAsync(Guid documentId);

    /// <summary>
    /// Gets the list of open tab document IDs in order.
    /// </summary>
    /// <returns>Ordered list of document IDs.</returns>
    Task<List<Guid>> GetOpenTabsAsync();

    /// <summary>
    /// Gets the recently closed documents list.
    /// </summary>
    /// <returns>List of recently closed info, most recent first.</returns>
    Task<List<RecentlyClosedInfo>> GetRecentlyClosedAsync();

    /// <summary>
    /// Gets the title for a specific document.
    /// </summary>
    /// <param name="documentId">The document ID.</param>
    /// <returns>The title, or null if no title is set.</returns>
    Task<string?> GetDocumentTitleAsync(Guid documentId);

    /// <summary>
    /// Gets the word wrap setting for a specific document.
    /// </summary>
    /// <param name="documentId">The document ID.</param>
    /// <returns>The word wrap setting, or null if using global default.</returns>
    Task<string?> GetDocumentWordWrapAsync(Guid documentId);

    /// <summary>
    /// Updates the word wrap setting for a document.
    /// </summary>
    /// <param name="documentId">The document ID.</param>
    /// <param name="wordWrap">The word wrap setting, or null to use global default.</param>
    Task UpdateDocumentWordWrapAsync(Guid documentId, string? wordWrap);

    /// <summary>
    /// Gets the document ID of the last active (selected) tab.
    /// </summary>
    /// <returns>The active tab's document ID, or null if none was set.</returns>
    Task<Guid?> GetActiveTabDocumentIdAsync();

    /// <summary>
    /// Sets the document ID of the active (selected) tab.
    /// Called when the user switches tabs to persist the selection.
    /// </summary>
    /// <param name="documentId">The active tab's document ID, or null to clear.</param>
    Task SetActiveTabDocumentIdAsync(Guid? documentId);

    /// <summary>
    /// Gets the syntax language for a specific document.
    /// </summary>
    /// <param name="documentId">The document ID.</param>
    /// <returns>The syntax language ID, or null if using plain text.</returns>
    Task<string?> GetDocumentSyntaxLanguageAsync(Guid documentId);

    /// <summary>
    /// Updates the syntax language for a document.
    /// </summary>
    /// <param name="documentId">The document ID.</param>
    /// <param name="syntaxLanguage">The syntax language ID, or null for plain text.</param>
    Task UpdateDocumentSyntaxLanguageAsync(Guid documentId, string? syntaxLanguage);
}
