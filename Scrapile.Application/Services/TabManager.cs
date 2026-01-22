namespace Scrapile.Application.Services;

using Scrapile.Application.DTOs;
using Scrapile.Application.Helpers;
using Scrapile.Domain.Entities;
using Scrapile.Domain.Interfaces;

/// <summary>
/// Service for managing tab lifecycle including creation, closing, duplication,
/// and reordering. Maintains in-memory tab collection and persists state via metadata store.
/// </summary>
public class TabManager
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IMetadataStore _metadataStore;
    private readonly List<Tab> _tabs = new();
    private readonly object _lock = new();
    private bool _initialized;

    /// <summary>
    /// Creates a new TabManager.
    /// </summary>
    /// <param name="documentRepository">The document repository for storage operations.</param>
    /// <param name="metadataStore">The metadata store for tab persistence.</param>
    public TabManager(IDocumentRepository documentRepository, IMetadataStore metadataStore)
    {
        _documentRepository = documentRepository ?? throw new ArgumentNullException(nameof(documentRepository));
        _metadataStore = metadataStore ?? throw new ArgumentNullException(nameof(metadataStore));
    }

    /// <summary>
    /// Initializes the tab manager by loading previously open tabs from metadata.
    /// Call this on application startup to restore the session.
    /// </summary>
    /// <returns>The restored tabs with stats.</returns>
    public async Task<IReadOnlyList<TabWithStats>> InitializeAsync()
    {
        var openTabIds = await _metadataStore.GetOpenTabsAsync();
        var restoredTabs = new List<Tab>();

        foreach (var documentId in openTabIds)
        {
            var document = await _documentRepository.GetByIdAsync(documentId);
            if (document != null)
            {
                var tab = new Tab
                {
                    TabId = Guid.NewGuid(),
                    Document = document,
                    Content = document.Content,
                    Order = restoredTabs.Count,
                    IsDirty = false
                };
                restoredTabs.Add(tab);
            }
            else
            {
                // Document was deleted externally - remove from metadata
                await _metadataStore.RemoveOpenTabAsync(documentId);
            }
        }

        lock (_lock)
        {
            _tabs.Clear();
            _tabs.AddRange(restoredTabs);
            _initialized = true;
        }

        return GetOpenTabs();
    }

    /// <summary>
    /// Creates a new tab with a new empty document.
    /// </summary>
    /// <returns>The created tab with stats.</returns>
    public async Task<TabWithStats> CreateTabAsync()
    {
        EnsureInitialized();

        var document = await _documentRepository.CreateAsync(string.Empty, null);

        int order;
        lock (_lock)
        {
            order = _tabs.Count;
        }

        await _metadataStore.AddOpenTabAsync(document.Id, order);

        var tab = new Tab
        {
            TabId = Guid.NewGuid(),
            Document = document,
            Content = document.Content,
            Order = order,
            IsDirty = false
        };

        lock (_lock)
        {
            _tabs.Add(tab);
        }

        return EnrichWithStats(tab);
    }

    /// <summary>
    /// Opens an existing document in a new tab.
    /// If the document is already open in a tab, returns that tab.
    /// </summary>
    /// <param name="documentId">The ID of the document to open.</param>
    /// <returns>The tab with stats, or null if document not found.</returns>
    public async Task<TabWithStats?> OpenDocumentInTabAsync(Guid documentId)
    {
        EnsureInitialized();

        // Check if already open
        Tab? existingTab;
        lock (_lock)
        {
            existingTab = _tabs.FirstOrDefault(t => t.Document.Id == documentId);
        }

        if (existingTab != null)
        {
            return EnrichWithStats(existingTab);
        }

        var document = await _documentRepository.GetByIdAsync(documentId);
        if (document == null)
        {
            return null;
        }

        int order;
        lock (_lock)
        {
            order = _tabs.Count;
        }

        await _metadataStore.AddOpenTabAsync(document.Id, order);

        // Remove from recently closed if present
        await _metadataStore.RemoveRecentlyClosedAsync(documentId);

        var tab = new Tab
        {
            TabId = Guid.NewGuid(),
            Document = document,
            Content = document.Content,
            Order = order,
            IsDirty = false
        };

        lock (_lock)
        {
            _tabs.Add(tab);
        }

        return EnrichWithStats(tab);
    }

    /// <summary>
    /// Opens an existing document in a new tab at a specific position.
    /// </summary>
    /// <param name="documentId">The ID of the document to open.</param>
    /// <param name="insertAfterTabId">The tab ID to insert after, or null to add at end.</param>
    /// <returns>The tab with stats, or null if document not found.</returns>
    public async Task<TabWithStats?> OpenDocumentInTabAtPositionAsync(Guid documentId, Guid? insertAfterTabId)
    {
        EnsureInitialized();

        // Check if already open
        Tab? existingTab;
        lock (_lock)
        {
            existingTab = _tabs.FirstOrDefault(t => t.Document.Id == documentId);
        }

        if (existingTab != null)
        {
            return EnrichWithStats(existingTab);
        }

        var document = await _documentRepository.GetByIdAsync(documentId);
        if (document == null)
        {
            return null;
        }

        int insertIndex;
        lock (_lock)
        {
            if (insertAfterTabId.HasValue)
            {
                var afterTab = _tabs.FirstOrDefault(t => t.TabId == insertAfterTabId.Value);
                insertIndex = afterTab != null ? _tabs.IndexOf(afterTab) + 1 : _tabs.Count;
            }
            else
            {
                insertIndex = _tabs.Count;
            }
        }

        var tab = new Tab
        {
            TabId = Guid.NewGuid(),
            Document = document,
            Content = document.Content,
            Order = insertIndex,
            IsDirty = false
        };

        lock (_lock)
        {
            _tabs.Insert(insertIndex, tab);
            ReorderTabsInternal();
        }

        await PersistTabOrderAsync();

        // Remove from recently closed if present
        await _metadataStore.RemoveRecentlyClosedAsync(documentId);

        return EnrichWithStats(tab);
    }

    /// <summary>
    /// Closes a tab and adds the document to recently closed list.
    /// </summary>
    /// <param name="tabId">The ID of the tab to close.</param>
    /// <returns>True if the tab was closed, false if not found.</returns>
    public async Task<bool> CloseTabAsync(Guid tabId)
    {
        EnsureInitialized();

        Tab? tabToClose;
        lock (_lock)
        {
            tabToClose = _tabs.FirstOrDefault(t => t.TabId == tabId);
            if (tabToClose == null)
            {
                return false;
            }

            _tabs.Remove(tabToClose);
            ReorderTabsInternal();
        }

        // Remove from open tabs in metadata
        await _metadataStore.RemoveOpenTabAsync(tabToClose.Document.Id);

        // Add to recently closed
        await _metadataStore.AddRecentlyClosedAsync(tabToClose.Document.Id, DateTime.UtcNow);

        // Persist the new tab order
        await PersistTabOrderAsync();

        return true;
    }

    /// <summary>
    /// Duplicates a tab by creating a new document with copied content.
    /// The new tab is inserted immediately after the source tab.
    /// </summary>
    /// <param name="tabId">The ID of the tab to duplicate.</param>
    /// <returns>The duplicated tab with stats, or null if source tab not found.</returns>
    public async Task<TabWithStats?> DuplicateTabAsync(Guid tabId)
    {
        EnsureInitialized();

        Tab? sourceTab;
        int insertIndex;
        lock (_lock)
        {
            sourceTab = _tabs.FirstOrDefault(t => t.TabId == tabId);
            if (sourceTab == null)
            {
                return null;
            }

            insertIndex = _tabs.IndexOf(sourceTab) + 1;
        }

        // Create new document with copied content
        // If source has title, new title = "{title} - Copy"
        string? newTitle = sourceTab.Document.HasTitle
            ? $"{sourceTab.Document.Title} - Copy"
            : null;

        var newDocument = await _documentRepository.CreateAsync(sourceTab.Content, newTitle);

        var newTab = new Tab
        {
            TabId = Guid.NewGuid(),
            Document = newDocument,
            Content = newDocument.Content,
            Order = insertIndex,
            IsDirty = false
        };

        lock (_lock)
        {
            _tabs.Insert(insertIndex, newTab);
            ReorderTabsInternal();
        }

        await PersistTabOrderAsync();

        return EnrichWithStats(newTab);
    }

    /// <summary>
    /// Reorders tabs by moving a tab to a new position.
    /// </summary>
    /// <param name="tabId">The ID of the tab to move.</param>
    /// <param name="newOrder">The new position (0-based index).</param>
    /// <returns>True if reorder succeeded, false if tab not found or invalid position.</returns>
    public async Task<bool> ReorderTabAsync(Guid tabId, int newOrder)
    {
        EnsureInitialized();

        lock (_lock)
        {
            var tab = _tabs.FirstOrDefault(t => t.TabId == tabId);
            if (tab == null)
            {
                return false;
            }

            if (newOrder < 0 || newOrder >= _tabs.Count)
            {
                return false;
            }

            var currentIndex = _tabs.IndexOf(tab);
            if (currentIndex == newOrder)
            {
                return true; // Already in position
            }

            _tabs.RemoveAt(currentIndex);
            _tabs.Insert(newOrder, tab);
            ReorderTabsInternal();
        }

        await PersistTabOrderAsync();
        return true;
    }

    /// <summary>
    /// Reorders all tabs according to the provided order.
    /// </summary>
    /// <param name="orderedTabIds">Tab IDs in their new order.</param>
    /// <returns>True if reorder succeeded, false if any tab ID is invalid.</returns>
    public async Task<bool> ReorderTabsAsync(IEnumerable<Guid> orderedTabIds)
    {
        EnsureInitialized();

        var orderedList = orderedTabIds.ToList();

        lock (_lock)
        {
            if (orderedList.Count != _tabs.Count)
            {
                return false;
            }

            var reorderedTabs = new List<Tab>();
            foreach (var tabId in orderedList)
            {
                var tab = _tabs.FirstOrDefault(t => t.TabId == tabId);
                if (tab == null)
                {
                    return false;
                }
                reorderedTabs.Add(tab);
            }

            _tabs.Clear();
            _tabs.AddRange(reorderedTabs);
            ReorderTabsInternal();
        }

        await PersistTabOrderAsync();
        return true;
    }

    /// <summary>
    /// Gets all open tabs in order with their stats.
    /// </summary>
    /// <returns>List of tabs with stats, ordered by position.</returns>
    public IReadOnlyList<TabWithStats> GetOpenTabs()
    {
        lock (_lock)
        {
            return _tabs
                .OrderBy(t => t.Order)
                .Select(EnrichWithStats)
                .ToList()
                .AsReadOnly();
        }
    }

    /// <summary>
    /// Gets a specific tab by ID with stats.
    /// </summary>
    /// <param name="tabId">The tab ID.</param>
    /// <returns>The tab with stats, or null if not found.</returns>
    public TabWithStats? GetTab(Guid tabId)
    {
        lock (_lock)
        {
            var tab = _tabs.FirstOrDefault(t => t.TabId == tabId);
            return tab != null ? EnrichWithStats(tab) : null;
        }
    }

    /// <summary>
    /// Gets a tab by document ID with stats.
    /// </summary>
    /// <param name="documentId">The document ID.</param>
    /// <returns>The tab with stats, or null if not found.</returns>
    public TabWithStats? GetTabByDocumentId(Guid documentId)
    {
        lock (_lock)
        {
            var tab = _tabs.FirstOrDefault(t => t.Document.Id == documentId);
            return tab != null ? EnrichWithStats(tab) : null;
        }
    }

    /// <summary>
    /// Updates the in-memory content for a tab without saving to disk.
    /// </summary>
    /// <param name="tabId">The tab ID.</param>
    /// <param name="content">The new content.</param>
    /// <returns>True if update succeeded, false if tab not found.</returns>
    public bool UpdateTabContent(Guid tabId, string content)
    {
        lock (_lock)
        {
            var tab = _tabs.FirstOrDefault(t => t.TabId == tabId);
            if (tab == null)
            {
                return false;
            }

            tab.Content = content;
            tab.IsDirty = tab.Content != tab.Document.Content;
            return true;
        }
    }

    /// <summary>
    /// Marks a tab as saved (not dirty) after the document has been persisted.
    /// </summary>
    /// <param name="tabId">The tab ID.</param>
    /// <returns>True if update succeeded, false if tab not found.</returns>
    public bool MarkTabAsSaved(Guid tabId)
    {
        lock (_lock)
        {
            var tab = _tabs.FirstOrDefault(t => t.TabId == tabId);
            if (tab == null)
            {
                return false;
            }

            // Update the document content to match tab content
            tab.Document.Content = tab.Content;
            tab.IsDirty = false;
            return true;
        }
    }

    /// <summary>
    /// Gets the count of open tabs.
    /// </summary>
    public int TabCount
    {
        get
        {
            lock (_lock)
            {
                return _tabs.Count;
            }
        }
    }

    /// <summary>
    /// Checks if any tabs have unsaved changes.
    /// </summary>
    public bool HasDirtyTabs
    {
        get
        {
            lock (_lock)
            {
                return _tabs.Any(t => t.IsDirty);
            }
        }
    }

    /// <summary>
    /// Gets all tabs that have unsaved changes.
    /// </summary>
    /// <returns>List of dirty tabs.</returns>
    public IReadOnlyList<TabWithStats> GetDirtyTabs()
    {
        lock (_lock)
        {
            return _tabs
                .Where(t => t.IsDirty)
                .Select(EnrichWithStats)
                .ToList()
                .AsReadOnly();
        }
    }

    /// <summary>
    /// Gets the list of recently closed documents with display information.
    /// </summary>
    /// <returns>List of recently closed items, most recent first.</returns>
    public async Task<IReadOnlyList<RecentlyClosedItem>> GetRecentlyClosedAsync()
    {
        EnsureInitialized();

        var recentlyClosed = await _metadataStore.GetRecentlyClosedAsync();
        var result = new List<RecentlyClosedItem>();

        foreach (var info in recentlyClosed)
        {
            var document = await _documentRepository.GetByIdAsync(info.DocumentId);
            var isDeleted = document == null;

            string? title = null;
            string contentPreview = string.Empty;

            if (!isDeleted)
            {
                title = document!.Title;
                contentPreview = ContentHelper.GetContentPreview(document.Content);
            }
            else
            {
                // Try to get title from metadata even if file is deleted
                title = await _metadataStore.GetDocumentTitleAsync(info.DocumentId);
            }

            result.Add(new RecentlyClosedItem
            {
                DocumentId = info.DocumentId,
                ClosedAt = info.ClosedAt,
                Title = title,
                ContentPreview = contentPreview,
                IsDeleted = isDeleted,
                FormattedClosedTime = FormatClosedTime(info.ClosedAt)
            });
        }

        return result.AsReadOnly();
    }

    /// <summary>
    /// Reopens the most recently closed tab.
    /// </summary>
    /// <returns>The reopened tab with stats, or null if no recently closed items or document was deleted.</returns>
    public async Task<TabWithStats?> ReopenLastClosedAsync()
    {
        EnsureInitialized();

        var recentlyClosed = await _metadataStore.GetRecentlyClosedAsync();

        foreach (var info in recentlyClosed)
        {
            // Try to open the document - skip if deleted
            var tab = await OpenDocumentInTabAsync(info.DocumentId);
            if (tab != null)
            {
                return tab;
            }

            // Document was deleted - remove from recently closed and try next
            await _metadataStore.RemoveRecentlyClosedAsync(info.DocumentId);
        }

        return null;
    }

    /// <summary>
    /// Reopens a specific document from the recently closed list.
    /// </summary>
    /// <param name="documentId">The ID of the document to reopen.</param>
    /// <returns>The reopened tab with stats, or null if document not found or was deleted.</returns>
    public async Task<TabWithStats?> ReopenDocumentFromRecentlyClosedAsync(Guid documentId)
    {
        EnsureInitialized();

        // Try to open the document
        var tab = await OpenDocumentInTabAsync(documentId);

        if (tab == null)
        {
            // Document was deleted - remove from recently closed
            await _metadataStore.RemoveRecentlyClosedAsync(documentId);
        }

        return tab;
    }

    /// <summary>
    /// Formats the closed time as a human-readable relative time string.
    /// </summary>
    private static string FormatClosedTime(DateTime closedAt)
    {
        var elapsed = DateTime.UtcNow - closedAt;

        if (elapsed.TotalSeconds < 60)
        {
            return "just now";
        }
        if (elapsed.TotalMinutes < 60)
        {
            var minutes = (int)elapsed.TotalMinutes;
            return minutes == 1 ? "1 minute ago" : $"{minutes} minutes ago";
        }
        if (elapsed.TotalHours < 24)
        {
            var hours = (int)elapsed.TotalHours;
            return hours == 1 ? "1 hour ago" : $"{hours} hours ago";
        }
        if (elapsed.TotalDays < 7)
        {
            var days = (int)elapsed.TotalDays;
            return days == 1 ? "1 day ago" : $"{days} days ago";
        }
        if (elapsed.TotalDays < 30)
        {
            var weeks = (int)(elapsed.TotalDays / 7);
            return weeks == 1 ? "1 week ago" : $"{weeks} weeks ago";
        }

        return closedAt.ToString("MMM d, yyyy");
    }

    /// <summary>
    /// Enriches a tab with calculated statistics.
    /// </summary>
    private static TabWithStats EnrichWithStats(Tab tab)
    {
        var wordCount = ContentHelper.CountWords(tab.Content);
        var charCount = ContentHelper.CountCharacters(tab.Content);
        var preview = ContentHelper.GetContentPreview(tab.Content);

        return new TabWithStats
        {
            Tab = tab,
            WordCount = wordCount,
            CharacterCount = charCount,
            ContentPreview = preview,
            FormattedWordCount = $"{ContentHelper.FormatCount(wordCount)} words",
            FormattedCharacterCount = $"{ContentHelper.FormatCount(charCount)} chars"
        };
    }

    /// <summary>
    /// Updates the Order property of all tabs based on their list position.
    /// Must be called within a lock.
    /// </summary>
    private void ReorderTabsInternal()
    {
        for (int i = 0; i < _tabs.Count; i++)
        {
            _tabs[i].Order = i;
        }
    }

    /// <summary>
    /// Persists the current tab order to metadata store.
    /// </summary>
    private async Task PersistTabOrderAsync()
    {
        List<Guid> documentIds;
        lock (_lock)
        {
            documentIds = _tabs.OrderBy(t => t.Order).Select(t => t.Document.Id).ToList();
        }

        await _metadataStore.UpdateOpenTabsOrderAsync(documentIds);
    }

    /// <summary>
    /// Ensures the tab manager has been initialized.
    /// </summary>
    private void EnsureInitialized()
    {
        if (!_initialized)
        {
            throw new InvalidOperationException("TabManager must be initialized before use. Call InitializeAsync() first.");
        }
    }
}
