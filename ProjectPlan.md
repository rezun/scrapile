# Scrapile - Project Specification

## 1. Project Overview

### 1.1 Purpose
Scrapile is a cross-platform scratchpad application designed for quick, disposable note-taking. Unlike traditional note-taking applications that focus on document management and organization, Scrapile treats tabs as the primary interface with documents persisting invisibly in the background for later retrieval.

### 1.2 Core Concept
The application follows a "frictionless capture with optional promotion" workflow:

**Default Mode - Zero Friction:**
- Users open tabs, paste or type content, and close tabs without any dialogs or decisions
- All content is automatically saved in the background
- No required metadata, no titles, no organization needed
- Perfect for temporary notes, clipboard history, quick thoughts, pasted information

**Optional Promotion - Add Context When Needed:**
- When users realize content is valuable long-term, they can add a title (and later: tags)
- Titled documents become easily findable through search
- Untitled documents remain searchable but blend into the background

**Philosophy:**
Similar to Gmail's approach: most content is ephemeral andearchive-worthy, but adding minimal context (title/tags) makes important items instantly findable. The goal is "easy to write, easy to find what matters, without mandatory organization."

**Statistics:**
- 95% of notes: Quick, untitled, rarely accessed again
- 5% of notes: Given titles when recognized as valuable, frequently retrieved

### 1.3 Target Platforms
- Windows (primary)
- macOS (primary)
- Linux (secondary)

**Future Consideration:** The application is designed with clear separation of concerns to enable potential web version in the future.

### 1.4 Key Differentiators
- **Zero friction by default**: No required fields, no save dialogs, no organization needed
- **Optional context**: Add titles only when content proves valuable
- **Gmail-style findability**: Search everything, but titled items are easier to find
- **Tabs are ephemeral; history is eternal**: Write and forget, find when needed
- **No mandatory hierarchy**: No forced folders, categories, or tags (tags optional in v2)
- **Keyboard-driven workflow**: Fast, efficient, minimal mouse usage

---

## 2. Technical Stack

### 2.1 Recommended Technologies
- **Framework**: Avalonia UI (cross-platform XAML-based UI framework)
- **Language**: C# / .NET 8.0+
- **Text Editor**: Standard TextBox for MVP, AvaloniaEdit for v2 (syntax highlighting)
- **Storage**: File system + JSON metadata file
- **Search**: File system-based full-text search

### 2.2 Architecture Overview

**Critical Design Principle:** Clear separation of concerns to enable future platform flexibility (desktop, web, mobile).

**Layer Architecture:**

```
┌─────────────────────────────────────────────┐
│         Presentation Layer (UI)             │
│  - Avalonia UI (Desktop)                    │
│  - [Future: Web UI, Mobile UI]              │
└─────────────────┬───────────────────────────┘
                  │
┌─────────────────▼───────────────────────────┐
│         Application Layer                   │
│  - TabManager (tab lifecycle)               │
│  - DocumentService (CRUD operations)        │
│  - SearchService (search logic)             │
│  - AutoSaveService (debounced saving)       │
└─────────────────┬───────────────────────────┘
                  │
┌─────────────────▼───────────────────────────┐
│         Domain Layer                        │
│  - Document (entity)                        │
│  - Tab (entity)                             │
│  - Business logic & validation              │
└─────────────────┬───────────────────────────┘
                  │
┌─────────────────▼───────────────────────────┐
│         Infrastructure Layer                │
│  - IDocumentRepository (interface)          │
│  - FileSystemDocumentRepository (impl)      │
│  - IMetadataStore (interface)               │
│  - JsonMetadataStore (impl)                 │
│  - [Future: CloudRepository, etc.]          │
└─────────────────────────────────────────────┘
```

**Project Structure:**
```
EphemeralNotes.Domain/          # Core business logic, entities
├── Entities/
│   ├── Document.cs
│   ├── Tab.cs
│   └── RecentlyClosedItem.cs
└── Interfaces/
    ├── IDocumentRepository.cs
    └── IMetadataStore.cs

EphemeralNotes.Application/     # Application services
├── Services/
│   ├── DocumentService.cs
│   ├── TabManager.cs
│   ├── SearchService.cs
│   └── AutoSaveService.cs
└── DTOs/
    └── SearchResult.cs

EphemeralNotes.Infrastructure/  # Platform-agnostic storage
├── Repositories/
│   └── FileSystemDocumentRepository.cs
└── Storage/
    └── JsonMetadataStore.cs

EphemeralNotes.Desktop/         # Avalonia UI
├── Views/
│   ├── MainWindow.axaml
│   ├── SearchWindow.axaml
│   └── TabView.axaml
└── ViewModels/
    ├── MainViewModel.cs
    └── SearchViewModel.cs
```

**Key Principles:**
1. **Domain layer** has zero UI dependencies
2. **Application layer** contains no UI code, only business logic
3. **Infrastructure layer** uses interfaces defined in Domain
4. **Presentation layer** (Avalonia) only handles UI rendering and user input
5. All platform-specific code isolated to Infrastructure and Presentation layers

### 2.3 Data Storage Structure

**Document Files:**
- Location: User-specified directory (configurable, default: `~/Documents/EphemeralNotes/`)
- File naming: `{timestamp}_{guid}.txt` (e.g., `20250122143022_a3f5b2e1.txt`)
- Content: Plain text (UTF-8)
- File system metadata provides: created date, last modified date

**Metadata File:**
- Location: Same directory as documents
- Filename: `.ephemeral_metadata.json`
- **Philosophy**: Store ONLY data that cannot be derived from file system
- Structure:
```json
{
  "version": "1.0",
  "openTabs": [
    {
      "documentId": "a3f5b2e1-c4d3-4e5f-8a9b-1c2d3e4f5a6b",
      "order": 0
    },
    {
      "documentId": "b4e6c3f2-d5e4-5f6g-9b0c-2d3e4f5g6h7i",
      "order": 1
    }
  ],
  "recentlyClosed": [
    {
      "documentId": "c5f7d4g3-e6f5-6g7h-0c1d-3e4f5g6h7i8j",
      "closedAt": "2025-01-22T15:18:30Z"
    }
  ],
  "documents": {
    "a3f5b2e1-c4d3-4e5f-8a9b-1c2d3e4f5a6b": {
      "title": "Meeting notes with John"
    },
    "b4e6c3f2-d5e4-5f6g-9b0c-2d3e4f5g6h7i": {
      "title": null
    }
  }
}
```

**Design Rationale:**
- **NOT stored**: Character count, word count, first line, last accessed, created date, modified date
  - These can be calculated from files or file system metadata on demand
  - Avoids sync issues between metadata and actual files
  - Reduces complexity of keeping metadata current
- **Stored**: Open tabs, recently closed, user-provided titles
  - These cannot be derived from file system
  - User intent (open tabs) is application state, not file state
  - Titles are user metadata, not file content

**Calculated On-Demand:**
- For open tabs: Calculate word/character count, read first line for display
- For search results: Read file content as needed
- This is computationally cheap and avoids metadata staleness issues

---

## 3. MVP Feature Specifications

### 3.1 Core Functionality

#### 3.1.1 Multi-Tab Interface with Auto-Save
**Description:** Users can open multiple text editing tabs simultaneously. All content is automatically saved without user intervention.

**Requirements:**
- Vertical tab layout (tabs displayed on left or right side)
- Each tab contains a plain text editor (TextBox)
- Tabs show title (first line of content, max 50 chars) or "Untitled"
- Auto-save triggers 500ms after last keystroke (debounced)
- No save dialogs or manual save buttons
- New tab created via keyboard shortcut or button
- Tab close button on each tab
- All tabs persist across application sessions (session restore)

**Technical Notes:**
- Implement debounced auto-save to prevent excessive I/O
- Use async file operations to prevent UI blocking
- Handle race conditions when user types quickly and closes tab
- Update metadata JSON file after each save

#### 3.1.2 Document Search
**Description:** Users can search through all historical documents by title (first line) and content.

**Requirements:**
- Global search accessible via keyboard shortcut (Ctrl/Cmd+P or Ctrl/Cmd+K)
- Search modal/overlay showing:
  - Search input field
  - Real-time filtered results as user types
  - Results display: title, snippet of content, last modified date
  - Result selection opens document in new tab
- Search scope: document titles (first line) and full content
- Search algorithm: Case-insensitive substring match (simple for MVP)
- Results sorted by relevance (exact title match first, then by last accessed date)

**Technical Notes:**
- For MVP, linear search through all files is acceptable (<10,000 documents)
- Read first line from metadata (fast), full content from file only when needed
- Consider async search to keep UI responsive
- Limit displayed results to top 50-100

#### 3.1.3 Recently Closed Tabs
**Description:** Users can quickly reopen recently closed tabs.

**Requirements:**
- Maintain stack of recently closed documents (last 50-100)
- Keyboard shortcut to reopen last closed tab (Ctrl/Cmd+Shift+T)
- Optional: UI panel showing recently closed list with titles and close times
- Reopening a tab removes it from recently closed list
- Recently closed list persists across sessions

**Technical Notes:**
- Store in metadata JSON as ordered array
- Implement LRU eviction when limit exceeded
- Include timestamp when tab was closed

### 3.2 Tab Management Features

#### 3.2.1 Vertical Tab Layout
**Description:** Tabs are displayed vertically (left or right side of window) rather than horizontally.

**Requirements:**
- Tabs stacked vertically with full title visibility
- Each tab shows:
  - Title (first line, truncated to fit)
  - Stats subtitle (see 3.2.2)
  - Close button
- Scrollable tab list when many tabs are open
- Visual indicator for active tab
- Configurable position (left or right side)

**Technical Notes:**
- Avalonia TabControl supports vertical orientation
- Custom tab header template for styling
- Consider virtualization if >100 tabs expected

#### 3.2.2 Quick Stats in Tabs
**Description:** Each tab displays metadata to help identify content at a glance.

**Requirements:**
- Display in tab header as subtitle below title/preview
- Show: Word count and character count (e.g., "245 words, 1.5k chars")
- **Calculate on-demand** (not stored in metadata):
  - For open tabs: Calculate from in-memory content, update on typing (debounced)
  - For recently closed: Calculate when displaying the list
- Stats visible without opening tab
- Use abbreviated formats for large numbers (1.5k, 23k)

**Technical Notes:**
- Simple word count: split on whitespace
- Character count: total characters including whitespace
- Debounce calculation (same as auto-save) to avoid performance issues
- Cache calculated stats for currently visible tabs
- No need to persist stats since they're cheap to calculate

#### 3.2.3 Bulk Tab Operations
**Description:** Users can close multiple tabs at once.

**Requirements:**
- Right-click context menu on tab with options:
  - "Close Tab" (Ctrl/Cmd+W)
  - "Close All Tabs"
  - "Close Tabs Above"
  - "Close Tabs Below"
  - "Duplicate Tab" (see 3.3.1)
- Confirmation dialog for "Close All Tabs" (optional, configurable)
- All closed tabs added to recently closed list

**Technical Notes:**
- Bulk operations should be efficient (batch metadata updates)
- Ensure all documents are saved before closing

#### 3.2.4 Optional Titles with Content Preview
**Description:** Documents have optional user-provided titles. Untitled documents display a content preview instead.

**Requirements:**
- Title field is optional and separate from document content
- **Default state**: No title (this is the normal/expected case)
- Title can be added via:
  - Title input field in tab header (inline editing)
  - Keyboard shortcut (Ctrl/Cmd+Shift+T for "Title")
  - Context menu option
- **Display in tabs:**
  - **With title**: Show title (50 char limit) in bold or prominent style
  - **Without title**: Show first ~40 characters of content in regular style
- **Display in search results:**
  - **With title**: Show title as primary line
  - **Without title**: Show content preview as primary line
- Title is stored in metadata JSON, not in document content
- Title editing updates immediately (no save action needed)
- Empty titles are treated as no title

**Design Philosophy:**
- Untitled is the norm, not an exception to label
- Adding a title is a conscious decision: "This matters, make it findable"
- Content preview gives enough context to recognize untitled documents
- Titled documents stand out visually as "promoted" content

**Technical Notes:**
- Extract first N characters on-demand when displaying untitled documents
- Trim whitespace and handle empty documents gracefully
- Title stored in metadata, synced to file system on change
- Consider caching content previews for open tabs only

#### 3.2.5 Session Restore
**Description:** All open tabs are restored when the application is reopened.

**Requirements:**
- Save list of open tab document IDs in metadata on application close
- Restore tabs in same order on application launch
- Restore last active tab as selected tab
- Handle missing documents gracefully (if file was deleted externally)

**Technical Notes:**
- Store `openTabs` array in metadata JSON
- Load documents on startup
- Consider lazy loading content for many tabs (load on tab activation)

### 3.3 Additional MVP Features

#### 3.3.1 Duplicate Tab
**Description:** Users can duplicate the current tab to create a copy with identical content.

**Requirements:**
- Keyboard shortcut: Ctrl/Cmd+Shift+D
- Context menu option: "Duplicate Tab"
- Creates new document file with copied content
- New tab opens immediately next to source tab
- New tab has same content but different document ID
- **Title handling**:
  - If source has title: New document gets title "{original title} - Copy"
  - If source has no title: New document also has no title

**Technical Notes:**
- Create new file with new GUID
- Copy entire content from source document
- Copy title from metadata if present, append " - Copy"
- Insert new tab adjacent to source in tab list

#### 3.3.2 Export/Share
**Description:** Users can export document content for external use.

**Requirements:**
- Copy to clipboard button/shortcut (Ctrl/Cmd+Shift+C)
- "Save As..." option to save document to user-chosen location
- Export formats: Plain text (.txt) for MVP
- Keyboard shortcut or right-click context menu access

**Technical Notes:**
- Use system clipboard API
- "Save As" uses standard file save dialog
- Does not modify original document location

### 3.4 Keyboard Shortcuts

| Action | Windows/Linux | macOS |
|--------|---------------|-------|
| New Tab | Ctrl+T | Cmd+T |
| Close Tab | Ctrl+W | Cmd+W |
| Reopen Closed Tab | Ctrl+Shift+T | Cmd+Shift+T |
| Edit Title | Ctrl+Shift+E or F2 | Cmd+Shift+E or F2 |
| Search | Ctrl+P or Ctrl+K | Cmd+P or Cmd+K |
| Duplicate Tab | Ctrl+Shift+D | Cmd+Shift+D |
| Copy to Clipboard | Ctrl+Shift+C | Cmd+Shift+C |
| Next Tab | Ctrl+Tab | Cmd+Tab |
| Previous Tab | Ctrl+Shift+Tab | Cmd+Shift+Tab |
| Close All Tabs | Ctrl+Shift+W | Cmd+Shift+W |

---

## 4. User Interface Design

### 4.1 Main Window Layout

```
┌─────────────────────────────────────────────┐
│  File  Edit  View  Help                     │ Menu Bar
├──────────┬──────────────────────────────────┤
│          │  ┌─────────────────────────────┐ │
│ Meeting  │  │ Title: Meeting notes...  [✓]│ │ Title edit
│  notes   │  └─────────────────────────────┘ │
│ 145 wds  │                                  │
│    [x]   │                                  │
│          │  [Text Editor Content]           │
│ Lorem    │                                  │
│  ipsum   │                                  │
│  dolor   │                                  │
│ 23 wds   │                                  │
│    [x]   │                                  │
│          │                                  │
│ Code     │                                  │
│  snip... │                                  │
│ 2.3k ch  │                                  │
│    [x]   │                                  │
│          │                                  │
│   [+]    │                                  │
│          │                                  │
│ Vertical │     Main Text Editor Area        │
│   Tab    │                                  │
│   Bar    │                                  │
│          │                                  │
└──────────┴──────────────────────────────────┘
```

**Tab Display Examples:**
- **With title**: "Meeting notes" (bold/prominent) + "145 wds" (subtitle)
- **Without title**: "Lorem ipsum dolor..." (regular) + "23 wds" (subtitle)

**Title Editing:**
- Inline title input field appears at top of editor when:
  - User presses F2 or Ctrl/Cmd+Shift+E
  - User clicks on tab and selects "Edit Title"
- Checkmark or Enter to save, Escape to cancel
- Can also clear title (leave blank) to return to content preview mode

### 4.2 Search Interface

```
┌─────────────────────────────────────────────┐
│  Search Documents                      [x]  │
├─────────────────────────────────────────────┤
│  [Search input field...................]    │
├─────────────────────────────────────────────┤
│  Meeting notes with John                    │ ← Titled document
│  Lorem ipsum dolor sit amet...              │
│  Last modified: 2 hours ago                 │
├─────────────────────────────────────────────┤
│  Build cross-platform note app...          │ ← Untitled (content preview)
│  Continuing: Need to implement search...    │
│  Last modified: Jan 22, 2025                │
├─────────────────────────────────────────────┤
│  Q3 Planning Notes                          │ ← Titled document
│  - Hire 2 engineers - Launch beta...       │
│  Last modified: Yesterday                   │
├─────────────────────────────────────────────┤
│  ... (more results)                         │
└─────────────────────────────────────────────┘
```

**Search Result Display:**
- Titled documents: Title shown prominently (bold)
- Untitled documents: First ~50 chars of content shown
- All results: Content snippet + metadata (last modified)

### 4.3 Visual Design Guidelines
- Clean, minimal interface
- Focus on content (text editor takes majority of screen space)
- Low visual clutter
- High contrast text for readability
- Monospace font option for code/technical content
- Responsive layout (handle window resizing gracefully)

---

## 5. Implementation Phases

### Phase 1: Core Infrastructure (Week 1)
- Set up Avalonia project structure
- Implement file storage system
- Create metadata JSON management
- Basic tab UI with single text editor

### Phase 2: Auto-Save & Tab Management (Week 2)
- Implement debounced auto-save
- Multi-tab support with vertical layout
- Tab creation and closing
- First-line title extraction
- Session restore

### Phase 3: Search & Recently Closed (Week 3)
- Search interface and functionality
- Recently closed tracking
- Reopen closed tabs feature
- Keyboard shortcuts implementation

### Phase 4: Polish & Additional Features (Week 4)
- Quick stats in tabs
- Duplicate tab functionality
- Export/copy to clipboard
- Bulk tab operations
- Context menus
- Settings/preferences

### Phase 5: Testing & Refinement (Week 5)
- Cross-platform testing (Windows, macOS, Linux)
- Performance testing with large document counts
- Edge case handling
- Bug fixes and polish

---

## 6. Configuration & Settings

### 6.1 Application Settings
Users should be able to configure:
- Document storage location (directory path)
- Tab position (left or right)
- Font family and size
- Theme (light/dark mode)
- Auto-save delay (default: 500ms)
- Recently closed list size (default: 50)
- Stats display format (word count vs. last modified)

### 6.2 Settings Storage
- Store in user-specific configuration file
- Location: `~/.config/EphemeralNotes/settings.json` (Linux/macOS) or `%APPDATA%/EphemeralNotes/settings.json` (Windows)

---

## 7. Error Handling & Edge Cases

### 7.1 Critical Scenarios to Handle
1. **Storage directory deleted/unavailable**
   - Prompt user to select new location
   - Do not lose open tab content (keep in memory)

2. **Metadata file corrupted**
   - Rebuild from file system scan
   - Warn user of potential data loss in recently closed

3. **File write errors (disk full, permissions)**
   - Show persistent error notification
   - Retry with exponential backoff
   - Allow user to save to alternate location

4. **Concurrent modifications (external editors)**
   - Detect file changes via file watcher
   - Prompt user to reload or keep current version

5. **Large document performance**
   - Set warning threshold (e.g., 10MB file size)
   - Consider optimization or chunking for v2

6. **Many tabs open (100+)**
   - Implement tab list virtualization
   - Consider warning user or tab limit

### 7.2 Data Integrity
- Atomic file writes (write to temp file, then rename)
- Metadata backup before write
- Periodic metadata validation

---

## 8. Future Enhancements (Post-MVP)

### 8.1 Version 2 Features (Planned)
- **Tags system**: Optional tags for additional categorization (similar to title promotion)
- **AvaloniaEdit integration** for syntax highlighting (Markdown, code)
- **Markdown preview** toggle/split view
- **Enhanced search** with fuzzy matching and tag filtering
- **Better findability for titled documents**: Prioritize titled documents in search results

### 8.2 Future Vision (v3+)
- **Document snapshots/versions**: 
  - Automatic snapshots at regular intervals
  - Manual snapshot creation
  - Compare versions side-by-side
  - Restore from previous versions
- **Document archiving** (auto-archive old, unaccessed documents)
- **Templates** for common note types
- **Drag & drop** text files to open as tabs
- **Web version**: Leverage layered architecture for browser-based client
- **Mobile apps**: iOS/Android with sync

### 8.3 Power User Features (Future)
- **Command palette** for all actions
- **Vim keybindings** mode (optional)
- **Custom themes** support
- **Plugins/extensions** architecture
- **Cloud sync** option (user-controlled, encrypted)

---

## 9. Testing Requirements

### 9.1 Unit Tests
- Document CRUD operations
- Auto-save logic (debouncing)
- Search functionality
- Metadata management
- First-line title extraction

### 9.2 Integration Tests
- Full tab lifecycle (create, edit, close, reopen)
- Session restore
- Search across multiple documents
- Bulk operations

### 9.3 Manual Testing Checklist
- Cross-platform UI consistency
- Keyboard shortcuts on all platforms
- Performance with 1000+ documents
- File system edge cases
- Long-running session stability

---

## 10. Success Criteria

The MVP will be considered complete when:
1. Users can create, edit, and close tabs without any manual saving or required fields
2. All content is reliably auto-saved to files
3. Users can optionally add titles to documents when content proves valuable
4. Untitled documents display content previews instead of "Untitled" labels
5. Search quickly finds documents by title (when present) and content
6. Recently closed tabs can be reopened
7. Sessions restore on app restart
8. Application runs smoothly on Windows, macOS, and Linux
9. No data loss under normal operation
10. Keyboard-driven workflow is smooth and intuitive
11. Clear separation of concerns enables future platform flexibility

---

## 11. Known Limitations (MVP)

- Plain text only (no rich text or markdown rendering)
- Simple substring search (no fuzzy matching, relevance ranking, or advanced queries)
- No tags system (planned for v2)
- No document snapshots/versions (planned for v3+)
- No collaboration features
- No mobile support
- No cloud sync
- Basic undo/redo (relies on text editor default)
- No automatic cleanup/archival of old documents

---

## 12. References & Resources

- **Avalonia UI Documentation**: https://docs.avaloniaui.net/
- **AvaloniaEdit**: https://github.com/AvaloniaUI/AvaloniaEdit
- **Similar Apps**: Notational Velocity, nvALT, Simplenote
- **Design Inspiration**: SQL History (RedGate), Browser tab model

---

## Appendix A: Sample Code Snippets

### A.1 Domain Entities

**Document.cs:**
```csharp
public class Document
{
    public Guid Id { get; set; }
    public string Filename { get; set; }
    public string? Title { get; set; }  // User-provided, optional
    
    // Derived from file system (not stored in metadata):
    public DateTime Created => File.GetCreationTime(FilePath);
    public DateTime LastModified => File.GetLastWriteTime(FilePath);
    
    // Calculated on-demand:
    public string ContentPreview { get; set; }  // First ~40 chars
    public int WordCount { get; set; }          // Calculated when needed
    public int CharacterCount { get; set; }     // Calculated when needed
    
    public string FilePath => Path.Combine(StorageDirectory, Filename);
    
    // Display name logic
    public string DisplayName => !string.IsNullOrWhiteSpace(Title) 
        ? Title 
        : ContentPreview;
}
```

**Tab.cs:**
```csharp
public class Tab
{
    public Guid TabId { get; set; }
    public Document Document { get; set; }
    public string Content { get; set; }  // In-memory content
    public int Order { get; set; }       // Position in tab list
    public bool IsDirty { get; set; }    // Has unsaved changes
}
```

**Metadata.cs:**
```csharp
public class Metadata
{
    public string Version { get; set; } = "1.0";
    public List<OpenTabInfo> OpenTabs { get; set; } = new();
    public List<RecentlyClosedInfo> RecentlyClosed { get; set; } = new();
    public Dictionary<Guid, DocumentMetadata> Documents { get; set; } = new();
}

public class OpenTabInfo
{
    public Guid DocumentId { get; set; }
    public int Order { get; set; }
}

public class RecentlyClosedInfo
{
    public Guid DocumentId { get; set; }
    public DateTime ClosedAt { get; set; }
}

public class DocumentMetadata
{
    public string? Title { get; set; }  // Only user-provided data
}
```

### A.2 Repository Interfaces (Domain Layer)

**IDocumentRepository.cs:**
```csharp
public interface IDocumentRepository
{
    Task<Document> CreateAsync(string content, string? title = null);
    Task<Document> GetByIdAsync(Guid id);
    Task<IEnumerable<Document>> GetAllAsync();
    Task UpdateContentAsync(Guid id, string content);
    Task UpdateTitleAsync(Guid id, string? title);
    Task DeleteAsync(Guid id);
    Task<IEnumerable<Document>> SearchAsync(string query);
}
```

**IMetadataStore.cs:**
```csharp
public interface IMetadataStore
{
    Task<Metadata> LoadAsync();
    Task SaveAsync(Metadata metadata);
    Task AddDocumentAsync(Guid documentId, string? title);
    Task UpdateDocumentTitleAsync(Guid documentId, string? title);
    Task AddOpenTabAsync(Guid documentId, int order);
    Task RemoveOpenTabAsync(Guid documentId);
    Task AddRecentlyClosedAsync(Guid documentId, DateTime closedAt);
    Task<List<Guid>> GetOpenTabsAsync();
    Task<List<RecentlyClosedInfo>> GetRecentlyClosedAsync();
}
```

**Design Benefits:**
- Infrastructure implementations (FileSystem, JSON) are completely swappable
- Future implementations: DatabaseRepository, CloudRepository, WebAPIRepository
- Domain and Application layers have zero dependency on storage mechanism
- Enables true separation for web version in future
### A.3 Auto-Save Service (Application Layer)

```csharp
public class AutoSaveService
{
    private readonly IDocumentRepository _repository;
    private readonly TimeSpan _debounceDelay = TimeSpan.FromMilliseconds(500);
    private readonly Dictionary<Guid, CancellationTokenSource> _saveTasks = new();
    
    public AutoSaveService(IDocumentRepository repository)
    {
        _repository = repository;
    }
    
    public async Task SaveAfterDelay(Guid documentId, string content)
    {
        // Cancel any existing save task for this document
        if (_saveTasks.TryGetValue(documentId, out var existingCts))
        {
            existingCts.Cancel();
        }
        
        var cts = new CancellationTokenSource();
        _saveTasks[documentId] = cts;
        
        try
        {
            await Task.Delay(_debounceDelay, cts.Token);
            await _repository.UpdateContentAsync(documentId, content);
            _saveTasks.Remove(documentId);
        }
        catch (TaskCanceledException)
        {
            // Expected when user continues typing
        }
    }
    
    public async Task SaveImmediately(Guid documentId, string content)
    {
        // Cancel debounced save if pending
        if (_saveTasks.TryGetValue(documentId, out var cts))
        {
            cts.Cancel();
            _saveTasks.Remove(documentId);
        }
        
        await _repository.UpdateContentAsync(documentId, content);
    }
}
```

### A.4 Content Preview Helper (Application Layer)

```csharp
public static class ContentHelper
{
    private const int PreviewLength = 40;
    
    public static string GetContentPreview(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return "(empty)";
        
        // Remove extra whitespace and newlines
        var cleaned = Regex.Replace(content.Trim(), @"\s+", " ");
        
        if (cleaned.Length <= PreviewLength)
            return cleaned;
        
        return cleaned.Substring(0, PreviewLength) + "...";
    }
    
    public static int CountWords(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return 0;
        
        return content.Split(new[] { ' ', '\t', '\n', '\r' }, 
            StringSplitOptions.RemoveEmptyEntries).Length;
    }
    
    public static int CountCharacters(string content)
    {
        return content?.Length ?? 0;
    }
    
    public static string FormatCount(int count)
    {
        if (count < 1000)
            return count.ToString();
        if (count < 10000)
            return $"{count / 1000.0:F1}k";
        return $"{count / 1000}k";
    }
}
```

---

**Document Version**: 1.1  
**Last Updated**: January 22, 2025  
**Author**: Software Specification for Ephemeral Notes MVP  
**Revision**: Updated core concept to "frictionless capture with optional promotion" model
