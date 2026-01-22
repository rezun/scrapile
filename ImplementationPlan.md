# Scrapile - Implementation Plan

## Overview

This document provides an actionable implementation plan for the Scrapile MVP. Each task is designed to be completed in one day or less by a single developer. Tasks are organized into phases with milestones for testing and evaluation.

**Reference Document:** `ProjectPlan.md` contains full specifications, architecture diagrams, and sample code.

---

## How to Use This Plan

### For Developers

1. **Find the next available task** - Look for tasks marked `[ ]` (not started)
2. **Mark task as in-progress** - Change `[ ]` to `[~]`
3. **Read the task description** and reference the relevant section in `ProjectPlan.md`
4. **Complete the task** following the acceptance criteria
5. **Mark task as done** - Change `[~]` to `[x]` and add completion date
6. **Add notes** in the "Developer Notes" section if needed for future tasks
7. **Commit changes** to git with a descriptive message

### Task Status Legend

- `[ ]` - Not started
- `[~]` - In progress (add assignee and start date)
- `[x]` - Completed (add completion date)
- `[!]` - Blocked (add blocker description in notes)

### Developer Notes Format

```
#### Task X.Y Notes
- **Notes:** [Any information relevant for subsequent tasks]
```

---

## Phase 1: Project Setup & Core Domain

**Goal:** Establish project structure and implement core domain entities with no UI dependencies.

**Duration:** 3-4 days

### Tasks

#### Task 1.1: Solution and Project Structure
**Estimated effort:** 2-4 hours

Create the .NET solution with layered architecture.

**Actions:**
1. Create solution file `Scrapile.sln`
2. Create class library projects:
   - `Scrapile.Domain` (net9.0)
   - `Scrapile.Application` (net9.0)
   - `Scrapile.Infrastructure` (net9.0)
3. Create Avalonia application project:
   - `Scrapile.Desktop` (net9.0)
4. Set up project references:
   - Application → Domain
   - Infrastructure → Domain
   - Desktop → Application, Infrastructure

**Acceptance Criteria:**
- [x] Solution builds successfully
- [x] All projects target .NET 9.0
- [x] Project references follow dependency rules (no circular refs)
- [x] Basic folder structure matches `ProjectPlan.md` Section 2.2

**Status:** [x] Completed 2025-01-22

---

#### Task 1.2: Domain Entities
**Estimated effort:** 3-4 hours

Implement core domain entities in `Scrapile.Domain`.

**Actions:**
1. Create `Entities/` folder
2. Implement `Document.cs` - see `ProjectPlan.md` Appendix A.1
3. Implement `Tab.cs` - see `ProjectPlan.md` Appendix A.1
4. Implement metadata classes (`Metadata.cs`, `OpenTabInfo.cs`, `RecentlyClosedInfo.cs`, `DocumentMetadata.cs`)

**Key Design Decisions:**
- `Document.Title` is nullable (optional titles per spec)
- Stats (word count, char count) are calculated on-demand, not stored
- `ContentPreview` is a calculated property

**Acceptance Criteria:**
- [x] All entity classes compile
- [x] Properties match specification in `ProjectPlan.md` Section 2.3 and Appendix A.1
- [x] No UI or infrastructure dependencies in Domain project

**Status:** [x] Completed 2025-01-22

---

#### Task 1.3: Domain Interfaces
**Estimated effort:** 2-3 hours

Define repository and storage interfaces in `Scrapile.Domain`.

**Actions:**
1. Create `Interfaces/` folder
2. Implement `IDocumentRepository.cs` - see `ProjectPlan.md` Appendix A.2
3. Implement `IMetadataStore.cs` - see `ProjectPlan.md` Appendix A.2

**Acceptance Criteria:**
- [x] Interfaces define all CRUD operations needed for documents
- [x] Interfaces support async operations
- [x] No implementation details leak into interfaces

**Status:** [x] Completed 2025-01-22

---

#### Task 1.4: Content Helper Utilities
**Estimated effort:** 2-3 hours

Implement content processing utilities in `Scrapile.Application`.

**Actions:**
1. Create `Helpers/` or `Utilities/` folder
2. Implement `ContentHelper.cs` - see `ProjectPlan.md` Appendix A.4
   - `GetContentPreview(string content)` - First 40 chars
   - `CountWords(string content)` - Split on whitespace
   - `CountCharacters(string content)` - Total length
   - `FormatCount(int count)` - Abbreviate large numbers (1.5k, 23k)

**Acceptance Criteria:**
- [x] Preview extraction handles empty/whitespace content gracefully
- [x] Word count splits on all whitespace types
- [x] Count formatting works for values 0 to 1,000,000+
- [x] Unit tests pass for edge cases

**Status:** [x] Completed 2025-01-22

---

#### Task 1.5: Domain Layer Review
**Estimated effort:** 1-2 hours

Perform code review and verification of Phase 1 implementation.

**Actions:**
1. Verify all 4 projects exist: Domain, Application, Infrastructure, Desktop
2. Review project references to confirm clean architecture dependency rules
3. Review all entity files against `ProjectPlan.md` Appendix A.1:
   - `Scrapile.Domain/Entities/Document.cs`
   - `Scrapile.Domain/Entities/Tab.cs`
   - `Scrapile.Domain/Entities/Metadata.cs`
   - `Scrapile.Domain/Entities/OpenTabInfo.cs`
   - `Scrapile.Domain/Entities/RecentlyClosedInfo.cs`
   - `Scrapile.Domain/Entities/DocumentMetadata.cs`
4. Review interfaces against `ProjectPlan.md` Appendix A.2:
   - `Scrapile.Domain/Interfaces/IDocumentRepository.cs`
   - `Scrapile.Domain/Interfaces/IMetadataStore.cs`
5. Review `Scrapile.Application/Helpers/ContentHelper.cs` implementation
6. Run all unit tests and verify they pass
7. Verify Domain project has zero external NuGet dependencies

**Acceptance Criteria:**
- [x] All Phase 1 tasks completed
- [x] Domain layer has zero external dependencies (except .NET BCL)
- [x] All entities and interfaces match specification
- [x] Unit tests written for ContentHelper and all pass
- [x] Dependency flow is unidirectional (inward toward Domain)

**Status:** [x] Completed 2025-01-22

**Review Notes:**
```
## Milestone 1 Review Summary

### Project Structure (PASS)
- All 4 projects present: Domain, Application, Infrastructure, Desktop
- Test project exists: Scrapile.Application.Tests
- Proper project references follow clean architecture dependency rules

### Domain Entities (PASS)
All entities match specification in ProjectPlan.md Appendix A.1:
- Document has Id, Filename, Title (nullable), Content, Created, LastModified
- Tab has TabId, Document, Content, Order, IsDirty
- Metadata classes properly structured for JSON serialization
- Proper use of nullable reference types (string?)

### Domain Interfaces (PASS)
Interfaces match specification in ProjectPlan.md Appendix A.2:
- All CRUD operations defined
- All methods are async (Task-based)
- No implementation details leaked
- Proper abstraction for storage layer

### Content Helper (PASS)
Implemented methods with GeneratedRegex for performance optimization.
Test execution: 39 tests passed (0 failed)

### Dependencies (PASS)
Dependency flow is unidirectional (inward toward Domain).

### Overall Assessment
Phase 1 implementation complete. Ready to proceed to Phase 2.
```

---

## Phase 2: Infrastructure - File Storage

**Goal:** Implement file-based storage for documents and metadata.

**Duration:** 3-4 days

### Tasks

#### Task 2.1: File System Document Repository
**Estimated effort:** 4-6 hours

Implement `IDocumentRepository` using file system storage.

**Actions:**
1. Create `Repositories/` folder in Infrastructure project
2. Implement `FileSystemDocumentRepository.cs`
3. File naming: `{timestamp}_{guid}.txt` (e.g., `20250122143022_a3f5b2e1.txt`)
4. Implement atomic writes (write to temp file, then rename)
5. Read file system metadata for created/modified dates

**Key Methods:**
- `CreateAsync` - Generate filename, write content, return Document
- `GetByIdAsync` - Find file by GUID portion of filename
- `GetAllAsync` - Enumerate all .txt files in storage directory
- `UpdateContentAsync` - Atomic write to existing file
- `DeleteAsync` - Remove file from disk

**Acceptance Criteria:**
- [x] Documents persist to disk as .txt files
- [x] File naming convention matches spec
- [x] Atomic writes prevent data corruption
- [x] Handles missing files gracefully
- [x] Works with configurable storage directory

**Status:** [x] Completed 2025-01-22

---

#### Task 2.2: JSON Metadata Store
**Estimated effort:** 4-6 hours

Implement `IMetadataStore` using JSON file storage.

**Actions:**
1. Create `Storage/` folder in Infrastructure project
2. Implement `JsonMetadataStore.cs`
3. Metadata file: `.ephemeral_metadata.json` in storage directory
4. Implement atomic JSON writes with backup

**Key Methods:**
- `LoadAsync` - Read and deserialize metadata (create default if missing)
- `SaveAsync` - Atomic write of entire metadata object
- Tab management methods (add/remove open tabs)
- Recently closed management methods

**Acceptance Criteria:**
- [x] Metadata persists as JSON file
- [x] Handles missing/corrupted metadata file gracefully
- [x] JSON structure matches spec in `ProjectPlan.md` Section 2.3
- [x] Atomic writes prevent corruption

**Status:** [x] Completed 2025-01-22

---

#### Task 2.3: Search Implementation
**Estimated effort:** 3-4 hours

Implement document search in repository.

**Actions:**
1. Add search method to `FileSystemDocumentRepository`
2. Search algorithm:
   - Case-insensitive substring matching
   - Search in titles (from metadata) and file content
   - Return results sorted: title matches first, then by last modified

**Performance Notes:**
- Linear search is acceptable for MVP (<10,000 documents)
- Read files only when needed (title-only matches don't need file read)

**Acceptance Criteria:**
- [x] Search finds documents by title substring
- [x] Search finds documents by content substring
- [x] Results are sorted appropriately
- [x] Search is case-insensitive
- [x] Performance acceptable with 100+ documents

**Status:** [x] Completed 2025-01-22

---

#### Task 2.4: Infrastructure Integration Tests
**Estimated effort:** 3-4 hours

Write integration tests for storage layer.

**Actions:**
1. Create test project `Scrapile.Infrastructure.Tests`
2. Test document CRUD operations
3. Test metadata persistence
4. Test search functionality
5. Test edge cases (missing files, concurrent access)

**Acceptance Criteria:**
- [ ] All repository methods have test coverage
- [ ] Tests use temporary directories (cleanup after)
- [ ] Edge cases documented and tested
- [ ] All tests pass

**Status:** [ ]

---

#### Task 2.5: Storage Layer Review
**Estimated effort:** 2-3 hours

Perform integration testing and verification of Phase 2 implementation.

**Actions:**
1. Run all integration tests in `Scrapile.Infrastructure.Tests`
2. Create a test harness or console app to manually test:
   - Create 10+ documents with various content
   - Read documents by ID
   - Update document content
   - Delete documents
   - Verify metadata persists correctly
3. Verify file naming convention matches spec: `{timestamp}_{guid}.txt`
4. Verify metadata JSON structure matches `ProjectPlan.md` Section 2.3
5. Performance test: create 100 documents and measure search time
6. Test error handling: missing files, corrupted metadata, permission errors
7. Document any issues found and verify fixes

**Acceptance Criteria:**
- [ ] All Phase 2 tasks completed
- [ ] Integration tests pass
- [ ] Manual CRUD operations work correctly
- [ ] File format matches specification
- [ ] Search completes in <500ms with 100 documents
- [ ] Error scenarios handled gracefully

**Status:** [ ]

**Review Notes:**
```
[Add review notes here]
```

---

## Phase 3: Application Services

**Goal:** Implement business logic services that orchestrate domain operations.

**Duration:** 3-4 days

### Tasks

#### Task 3.1: Document Service
**Estimated effort:** 3-4 hours

Implement document operations orchestration.

**Actions:**
1. Create `Services/` folder in Application project
2. Implement `DocumentService.cs`
3. Coordinate between repository and metadata store
4. Handle title updates (stored in metadata, not file)

**Key Methods:**
- Create document (generates file + metadata entry)
- Get document with calculated stats
- Update document content
- Update document title
- Delete document (removes file + metadata)
- Search documents

**Acceptance Criteria:**
- [ ] Service properly coordinates repository and metadata store
- [ ] Title changes update metadata only
- [ ] Content changes update file only
- [ ] Stats calculated on retrieval

**Status:** [ ]

---

#### Task 3.2: Auto-Save Service
**Estimated effort:** 3-4 hours

Implement debounced auto-save functionality.

**Actions:**
1. Implement `AutoSaveService.cs` - see `ProjectPlan.md` Appendix A.3
2. Debounce delay: 500ms (configurable)
3. Track pending saves per document
4. Support immediate save (for tab close)

**Key Behaviors:**
- Each keystroke resets the debounce timer
- Only one pending save per document at a time
- `SaveImmediately` cancels any pending debounced save

**Acceptance Criteria:**
- [ ] Debouncing works correctly (rapid typing = single save)
- [ ] Immediate save works for tab close scenarios
- [ ] No race conditions with concurrent saves
- [ ] Cancellation handled gracefully

**Status:** [ ]

---

#### Task 3.3: Tab Manager Service
**Estimated effort:** 4-5 hours

Implement tab lifecycle management.

**Actions:**
1. Implement `TabManager.cs`
2. Manage in-memory tab collection
3. Track tab order
4. Handle tab CRUD operations
5. Integrate with metadata store for persistence

**Key Methods:**
- `CreateTabAsync()` - New tab with new document
- `OpenDocumentInTabAsync(Guid documentId)` - Open existing document
- `CloseTabAsync(Guid tabId)` - Close tab, add to recently closed
- `DuplicateTabAsync(Guid tabId)` - Copy tab/document
- `ReorderTabs(...)` - Change tab positions
- `GetOpenTabs()` - Return current tabs in order

**Acceptance Criteria:**
- [ ] Tabs maintain correct order
- [ ] Closing tab adds to recently closed list
- [ ] Duplicate creates new document with copied content
- [ ] Tab state survives service restart (via metadata)

**Status:** [ ]

---

#### Task 3.4: Recently Closed Service
**Estimated effort:** 2-3 hours

Implement recently closed tab tracking.

**Actions:**
1. Add recently closed logic to `TabManager` or create separate service
2. Maintain stack of last 50 closed documents
3. Support reopen operation
4. Persist to metadata

**Key Methods:**
- `AddToRecentlyClosed(Guid documentId)`
- `GetRecentlyClosed()` - Returns list with timestamps
- `ReopenLastClosed()` - Removes from list, opens in new tab
- `ReopenDocument(Guid documentId)` - Reopen specific document

**Acceptance Criteria:**
- [ ] Stack limited to 50 items (LRU eviction)
- [ ] Reopening removes from recently closed
- [ ] List persists across sessions
- [ ] Handles missing documents (deleted files)

**Status:** [ ]

---

#### Task 3.5: Application Service Tests
**Estimated effort:** 3-4 hours

Write unit tests for application services.

**Actions:**
1. Create test project `Scrapile.Application.Tests`
2. Mock repository and metadata store
3. Test DocumentService operations
4. Test AutoSaveService debouncing
5. Test TabManager lifecycle

**Acceptance Criteria:**
- [ ] All service methods have test coverage
- [ ] Debouncing behavior verified with timing tests
- [ ] Edge cases documented and tested
- [ ] All tests pass

**Status:** [ ]

---

#### Task 3.6: Services Layer Review
**Estimated effort:** 2-3 hours

Perform unit testing and integration verification of Phase 3 implementation.

**Actions:**
1. Run all unit tests in `Scrapile.Application.Tests`
2. Create integration test for full document lifecycle:
   - Create document via DocumentService
   - Update content and title
   - Verify auto-save triggers
   - Close and reopen via TabManager
   - Delete document
3. Test auto-save debouncing:
   - Rapid content changes (simulate typing)
   - Verify only one save after debounce delay
   - Test immediate save on tab close
4. Test TabManager state persistence:
   - Create multiple tabs with different orders
   - Simulate service restart
   - Verify tabs restored in correct order
5. Test RecentlyClosedService:
   - Close tabs and verify they appear in recently closed
   - Reopen tabs and verify removal from list
   - Test 50-item limit
6. Document any issues found and verify fixes

**Acceptance Criteria:**
- [ ] All Phase 3 tasks completed
- [ ] Unit tests pass
- [ ] Full document lifecycle works end-to-end
- [ ] Auto-save debouncing verified (rapid typing = single save)
- [ ] Tab manager state persists across restarts
- [ ] Recently closed list works correctly

**Status:** [ ]

**Review Notes:**
```
[Add review notes here]
```

---

## Phase 4: Basic UI - Main Window and Tabs

**Goal:** Implement the core Avalonia UI with basic tab functionality.

**Duration:** 4-5 days

### Tasks

#### Task 4.1: Avalonia Project Setup
**Estimated effort:** 2-3 hours

Configure Avalonia project with MVVM structure.

**Actions:**
1. Configure `Scrapile.Desktop` as Avalonia application
2. Add required NuGet packages (Avalonia.Desktop, etc.)
3. Set up MVVM folders (Views, ViewModels)
4. Configure dependency injection
5. Create App.axaml with basic styling

**Acceptance Criteria:**
- [ ] Application launches with empty window
- [ ] DI container configured with services
- [ ] MVVM structure in place
- [ ] Basic light theme applied

**Status:** [ ]

---

#### Task 4.2: Main Window Layout
**Estimated effort:** 3-4 hours

Implement main window with split layout.

**Actions:**
1. Create `MainWindow.axaml` with two-column layout
2. Left panel: Tab list area (placeholder)
3. Right panel: Editor area (placeholder)
4. Implement `MainViewModel.cs`
5. Set up basic window chrome (title, resize, close)

**Reference:** `ProjectPlan.md` Section 4.1 for layout diagram

**Acceptance Criteria:**
- [ ] Window displays with correct layout
- [ ] Panels resize appropriately
- [ ] Window state (size, position) feels natural
- [ ] Proper window title

**Status:** [ ]

---

#### Task 4.3: Vertical Tab List UI
**Estimated effort:** 4-5 hours

Implement vertical tab list component.

**Actions:**
1. Create `TabListView.axaml` user control
2. Display tabs in vertical list with:
   - Title or content preview
   - Stats subtitle (word count)
   - Close button
3. Visual indicator for selected tab
4. "New tab" button at bottom
5. Create `TabListViewModel.cs`

**Tab Item Display:**
- With title: Bold text
- Without title: Regular text showing content preview
- Subtitle: "245 words" format

**Acceptance Criteria:**
- [ ] Tabs display vertically
- [ ] Selected tab visually distinct
- [ ] Close button visible on hover or always
- [ ] New tab button functional
- [ ] Scrollable when many tabs

**Status:** [ ]

---

#### Task 4.4: Text Editor Area
**Estimated effort:** 3-4 hours

Implement the main text editor component.

**Actions:**
1. Create `EditorView.axaml` user control
2. Use standard TextBox for MVP (multiline, accepting returns)
3. Title edit field above editor (optional, for titled documents)
4. Create `EditorViewModel.cs`
5. Bind to current tab's content

**Acceptance Criteria:**
- [ ] Editor displays current tab content
- [ ] Text changes reflected in ViewModel
- [ ] Proper text wrapping behavior
- [ ] Reasonable default font/size
- [ ] Title field shows/edits document title

**Status:** [ ]

---

#### Task 4.5: Tab Selection and Switching
**Estimated effort:** 3-4 hours

Wire up tab selection to editor display.

**Actions:**
1. Connect TabListViewModel selection to MainViewModel
2. Update editor when tab selection changes
3. Implement tab switching with keyboard (Ctrl+Tab, Ctrl+Shift+Tab)
4. Handle no-tabs state gracefully

**Acceptance Criteria:**
- [ ] Clicking tab shows its content in editor
- [ ] Keyboard shortcuts switch tabs
- [ ] Editor updates immediately on switch
- [ ] Empty state handled (no tabs open)

**Status:** [ ]

---

#### Task 4.6: Tab Create and Close Operations
**Estimated effort:** 3-4 hours

Implement basic tab lifecycle in UI.

**Actions:**
1. "New Tab" creates tab via TabManager
2. Close button closes tab via TabManager
3. Confirm save before close (handled by auto-save)
4. Update UI when tabs change

**Keyboard Shortcuts:**
- Ctrl+T: New tab
- Ctrl+W: Close current tab

**Acceptance Criteria:**
- [ ] New tab appears and becomes selected
- [ ] Close removes tab from list
- [ ] Keyboard shortcuts work
- [ ] Content saved before close

**Status:** [ ]

---

#### Task 4.7: Basic UI Review
**Estimated effort:** 2-3 hours

Perform UI testing and verification of Phase 4 implementation.

**Actions:**
1. Launch application and verify window displays correctly
2. Verify UI layout matches `ProjectPlan.md` Section 4.1:
   - Two-column layout (tab list left, editor right)
   - Panels resize appropriately
   - Window title correct
3. Test tab operations manually:
   - Create new tab (button and Ctrl+T)
   - Type content in editor
   - Switch between tabs (click and Ctrl+Tab/Ctrl+Shift+Tab)
   - Close tab (button and Ctrl+W)
4. Test persistence:
   - Create tab, type content, close app
   - Reopen app, verify tab and content restored
5. Test edge cases:
   - No tabs open state
   - Many tabs (10+) with scrolling
   - Long content in editor
6. Verify visual design:
   - Selected tab visually distinct
   - Close button visible
   - Font and spacing appropriate
7. Document any issues found and verify fixes

**Acceptance Criteria:**
- [ ] All Phase 4 tasks completed
- [ ] Application launches and displays correctly
- [ ] Can create, edit, switch, and close tabs
- [ ] Tab content persists across app restarts
- [ ] Keyboard shortcuts work (Ctrl+T, Ctrl+W, Ctrl+Tab)
- [ ] UI matches layout in specification

**Status:** [ ]

**Review Notes:**
```
[Add review notes here]
```

---

## Phase 5: Auto-Save and Session Restore

**Goal:** Implement automatic saving and session persistence.

**Duration:** 2-3 days

### Tasks

#### Task 5.1: Auto-Save Integration
**Estimated effort:** 3-4 hours

Connect UI to AutoSaveService.

**Actions:**
1. Subscribe to text changes in EditorViewModel
2. Trigger AutoSaveService on each change
3. Show subtle save indicator (optional: dirty state indicator)
4. Ensure save completes on tab close

**Acceptance Criteria:**
- [ ] Typing triggers debounced save
- [ ] No save on every keystroke (debouncing works)
- [ ] Save indicator shows state (if implemented)
- [ ] Closing tab waits for save to complete

**Status:** [ ]

---

#### Task 5.2: Session Restore on Startup
**Estimated effort:** 3-4 hours

Implement session restore functionality.

**Actions:**
1. On app startup, load metadata for open tabs
2. Restore tabs in saved order
3. Restore last active tab as selected
4. Handle missing documents gracefully

**Acceptance Criteria:**
- [ ] Tabs restored on app launch
- [ ] Tab order preserved
- [ ] Last active tab selected
- [ ] Missing files handled (show error or skip)

**Status:** [ ]

---

#### Task 5.3: Session Save on Exit
**Estimated effort:** 2-3 hours

Implement session save on application close.

**Actions:**
1. Hook application exit event
2. Save all pending changes immediately
3. Save open tab list to metadata
4. Save active tab ID

**Acceptance Criteria:**
- [ ] Closing app saves all pending changes
- [ ] Open tabs saved to metadata
- [ ] Active tab recorded
- [ ] Graceful handling of save errors

**Status:** [ ]

---

#### Task 5.4: Auto-Save and Session Restore Review
**Estimated effort:** 2-3 hours

Perform thorough testing of auto-save and session persistence.

**Actions:**
1. Test auto-save debouncing:
   - Type rapidly for 10 seconds, verify single save after delay
   - Type slowly (pause between words), verify saves occur
   - Monitor file system to confirm write behavior
2. Test session restore scenarios:
   - Create 5 tabs with content
   - Close app normally, reopen - verify all tabs present
   - Verify tab order preserved
   - Verify last active tab selected
3. Test data loss prevention:
   - Type rapidly, close app immediately
   - Reopen and verify no data loss
   - Type, force-quit app (kill process)
   - Reopen and verify minimal data loss (only unsaved debounce window)
4. Test error handling:
   - Delete a document file externally while app running
   - Reopen app - verify graceful handling
   - Corrupt metadata file, reopen app - verify recovery
5. Test save indicator (if implemented):
   - Verify dirty state shown during typing
   - Verify saved state shown after auto-save
6. Document any issues found and verify fixes

**Acceptance Criteria:**
- [ ] All Phase 5 tasks completed
- [ ] Auto-save debouncing works correctly
- [ ] Session restore works for all tab states
- [ ] No data loss in normal operation
- [ ] Graceful handling of deleted/missing files
- [ ] Force-quit results in minimal data loss

**Status:** [ ]

**Review Notes:**
```
[Add review notes here]
```

---

## Phase 6: Search Functionality

**Goal:** Implement document search UI and functionality.

**Duration:** 3-4 days

### Tasks

#### Task 6.1: Search Window/Overlay UI
**Estimated effort:** 4-5 hours

Create the search interface.

**Actions:**
1. Create `SearchWindow.axaml` (modal or overlay)
2. Search input field at top
3. Results list below
4. Result items show: title/preview, snippet, last modified
5. Create `SearchViewModel.cs`

**Reference:** `ProjectPlan.md` Section 4.2 for mockup

**Acceptance Criteria:**
- [ ] Search window appears as overlay/modal
- [ ] Clean, minimal design
- [ ] Results display correctly formatted
- [ ] Keyboard navigable

**Status:** [ ]

---

#### Task 6.2: Search Trigger and Keyboard Shortcut
**Estimated effort:** 2-3 hours

Implement search activation.

**Actions:**
1. Add global keyboard shortcut (Ctrl+P or Ctrl+K)
2. Show search window on trigger
3. Focus search input immediately
4. Escape key closes search

**Acceptance Criteria:**
- [ ] Ctrl+P opens search
- [ ] Search input focused automatically
- [ ] Escape closes search
- [ ] Clicking outside closes search (optional)

**Status:** [ ]

---

#### Task 6.3: Real-Time Search Results
**Estimated effort:** 3-4 hours

Implement live search as user types.

**Actions:**
1. Connect SearchViewModel to DocumentService search
2. Trigger search on input change (with small debounce ~100ms)
3. Display results immediately
4. Limit to top 50-100 results
5. Show loading indicator for large result sets

**Acceptance Criteria:**
- [ ] Results update as user types
- [ ] Search is responsive (no UI freeze)
- [ ] Results limited appropriately
- [ ] Empty query shows nothing or recent documents

**Status:** [ ]

---

#### Task 6.4: Open Document from Search
**Estimated effort:** 2-3 hours

Implement result selection and document opening.

**Actions:**
1. Click on result opens document in new tab
2. Enter key opens selected result
3. Arrow keys navigate results
4. Search window closes after selection

**Acceptance Criteria:**
- [ ] Click opens document
- [ ] Keyboard navigation works
- [ ] Document opens in new tab (or focuses if already open)
- [ ] Search closes after opening document

**Status:** [ ]

---

#### Task 6.5: Search Functionality Review
**Estimated effort:** 2-3 hours

Perform thorough testing of search functionality.

**Actions:**
1. Create test documents with known titles and content:
   - Document with title "Meeting Notes"
   - Document with title "Project Ideas"
   - Untitled document with content "budget report"
   - Untitled document with content "quarterly review"
2. Test search scenarios:
   - Empty query (should show nothing or recent documents)
   - Partial title match ("Meet") - should find "Meeting Notes"
   - Full title match ("Project Ideas")
   - Content-only match ("budget") - should find untitled document
   - No results ("xyznonexistent")
   - Case-insensitive search ("MEETING" should match "Meeting")
3. Test keyboard-only workflow:
   - Ctrl+P to open search
   - Type query
   - Arrow keys to navigate results
   - Enter to open selected document
   - Escape to close without selecting
4. Test result display:
   - Title/preview shown correctly
   - Snippet shows matching content
   - Last modified date displayed
5. Test document opening:
   - Click opens document in new tab
   - Already-open document focuses existing tab
   - Search closes after selection
6. Test performance with 50+ documents
7. Document any issues found and verify fixes

**Acceptance Criteria:**
- [ ] All Phase 6 tasks completed
- [ ] Search finds documents by title (case-insensitive)
- [ ] Search finds documents by content (case-insensitive)
- [ ] Results display correctly formatted
- [ ] Keyboard navigation works completely
- [ ] Document opens successfully from search
- [ ] Performance acceptable with many documents

**Status:** [ ]

**Review Notes:**
```
[Add review notes here]
```

---

## Phase 7: Recently Closed and Additional Features

**Goal:** Implement recently closed tabs and remaining MVP features.

**Duration:** 3-4 days

### Tasks

#### Task 7.1: Reopen Last Closed Tab
**Estimated effort:** 2-3 hours

Implement quick reopen functionality.

**Actions:**
1. Add keyboard shortcut Ctrl+Shift+T
2. Connect to RecentlyClosed service
3. Reopen most recently closed tab
4. Handle empty recently closed list

**Acceptance Criteria:**
- [ ] Ctrl+Shift+T reopens last closed tab
- [ ] Tab appears in correct position (or at end)
- [ ] Document removed from recently closed list
- [ ] No-op if list is empty (show message optional)

**Status:** [ ]

---

#### Task 7.2: Recently Closed Panel (Optional)
**Estimated effort:** 3-4 hours

Implement UI to view/manage recently closed tabs.

**Actions:**
1. Create panel or dropdown showing recently closed
2. Display document title/preview and close time
3. Click to reopen specific document
4. Consider placement (bottom of tab list, menu, etc.)

**Acceptance Criteria:**
- [ ] Recently closed list visible
- [ ] Shows title and relative time
- [ ] Click reopens document
- [ ] List scrollable if long

**Status:** [ ]

---

#### Task 7.3: Tab Context Menu
**Estimated effort:** 3-4 hours

Implement right-click context menu on tabs.

**Actions:**
1. Add context menu to tab items
2. Menu options:
   - Close Tab
   - Close All Tabs
   - Close Tabs Above
   - Close Tabs Below
   - Duplicate Tab
   - Edit Title (if applicable)
3. Implement each action

**Acceptance Criteria:**
- [ ] Right-click shows context menu
- [ ] All menu options work correctly
- [ ] Bulk close operations update recently closed

**Status:** [ ]

---

#### Task 7.4: Duplicate Tab Feature
**Estimated effort:** 2-3 hours

Implement tab duplication.

**Actions:**
1. Add Ctrl+Shift+D keyboard shortcut
2. Create new document with copied content
3. If source has title, new title = "{title} - Copy"
4. Insert new tab next to source

**Acceptance Criteria:**
- [ ] Keyboard shortcut works
- [ ] Content fully copied
- [ ] Title handled correctly
- [ ] New tab positioned correctly

**Status:** [ ]

---

#### Task 7.5: Export/Copy to Clipboard
**Estimated effort:** 2-3 hours

Implement export functionality.

**Actions:**
1. Add Ctrl+Shift+C to copy entire document to clipboard
2. Add "Save As..." option in context menu or menu bar
3. Save As opens file dialog for location selection

**Acceptance Criteria:**
- [ ] Copy to clipboard works
- [ ] Save As creates file at chosen location
- [ ] Original document unchanged
- [ ] Feedback shown (toast/notification optional)

**Status:** [ ]

---

#### Task 7.6: Additional Features Review
**Estimated effort:** 2-3 hours

Perform thorough testing of Phase 7 features.

**Actions:**
1. Test recently closed functionality:
   - Close 5 tabs sequentially
   - Use Ctrl+Shift+T to reopen each (LIFO order)
   - Verify all 5 reopen correctly with content intact
   - Test recently closed panel (if implemented)
2. Test context menu:
   - Right-click on tab, verify menu appears
   - Test "Close Tab" - tab closes
   - Test "Close All Tabs" - all tabs close
   - Test "Close Tabs Above" - tabs above close
   - Test "Close Tabs Below" - tabs below close
   - Test "Duplicate Tab" - new tab with copied content
   - Test "Edit Title" (if in menu)
3. Test duplicate tab feature:
   - Create tab with title and content
   - Duplicate with Ctrl+Shift+D
   - Verify new tab has "{title} - Copy" title
   - Verify content fully copied
   - Verify new tab positioned next to original
4. Test export/copy features:
   - Create tab with content
   - Use Ctrl+Shift+C to copy entire document
   - Paste in external app, verify content matches
   - Test "Save As..." if implemented
   - Verify original document unchanged
5. Test edge cases:
   - Recently closed with 50+ items (LRU eviction)
   - Reopen document that was deleted from disk
   - Context menu on only tab
6. Document any issues found and verify fixes

**Acceptance Criteria:**
- [ ] All Phase 7 tasks completed
- [ ] Reopen last closed works (Ctrl+Shift+T)
- [ ] Recently closed list shows correctly
- [ ] All context menu options work
- [ ] Duplicate preserves content and title correctly
- [ ] Export to clipboard works
- [ ] Edge cases handled gracefully

**Status:** [ ]

**Review Notes:**
```
[Add review notes here]
```

---

## Phase 8: Polish and Platform Testing

**Goal:** Polish UI, implement settings, and test across platforms.

**Duration:** 4-5 days

### Tasks

#### Task 8.1: Quick Stats Display
**Estimated effort:** 2-3 hours

Finalize stats display in tabs.

**Actions:**
1. Ensure word/character count displays in tab subtitle
2. Update stats on content change (debounced)
3. Use abbreviated format for large numbers
4. Cache stats for performance

**Acceptance Criteria:**
- [ ] Stats show in tab subtitle
- [ ] Stats update when content changes
- [ ] Format: "245 words" or "1.5k words"
- [ ] No performance issues

**Status:** [ ]

---

#### Task 8.2: Title Editing UX
**Estimated effort:** 3-4 hours

Polish the title editing experience.

**Actions:**
1. Title field in editor header area
2. F2 or Ctrl+Shift+E to focus title field
3. Enter to save, Escape to cancel
4. Empty title = no title (content preview mode)
5. Visual distinction between titled and untitled tabs

**Acceptance Criteria:**
- [ ] Title editing is intuitive
- [ ] Keyboard shortcuts work
- [ ] Titled tabs visually distinct
- [ ] Empty title clears title (not sets to empty string)

**Status:** [ ]

---

#### Task 8.3: Settings Infrastructure
**Estimated effort:** 3-4 hours

Implement settings storage and basic UI.

**Actions:**
1. Create settings service
2. Settings file location per spec (platform-specific)
3. Implement settings model:
   - Storage directory
   - Tab position (left/right)
   - Font family and size
   - Theme (light/dark)
   - Auto-save delay
4. Create simple settings dialog

**Acceptance Criteria:**
- [ ] Settings persist across sessions
- [ ] Settings file in correct location per platform
- [ ] Settings dialog allows changes
- [ ] Changes apply immediately or on restart

**Status:** [ ]

---

#### Task 8.4: Dark Mode Support
**Estimated effort:** 3-4 hours

Implement theme switching.

**Actions:**
1. Define light and dark color schemes
2. Theme toggle in settings
3. Apply theme to all UI components
4. System theme detection (optional)

**Acceptance Criteria:**
- [ ] Light theme looks good
- [ ] Dark theme looks good
- [ ] Theme switch works without restart
- [ ] All components themed correctly

**Status:** [ ]

---

#### Task 8.5: Keyboard Shortcuts Audit
**Estimated effort:** 2-3 hours

Verify all keyboard shortcuts work.

**Actions:**
1. Test all shortcuts from `ProjectPlan.md` Section 3.4
2. Fix any non-working shortcuts
3. Ensure platform-specific keys (Cmd vs Ctrl)
4. Add keyboard shortcut help (menu or dialog)

**Acceptance Criteria:**
- [ ] All shortcuts in spec work
- [ ] Platform-appropriate modifier keys
- [ ] No shortcut conflicts
- [ ] Shortcuts discoverable in UI

**Status:** [ ]

---

#### Task 8.6: Windows Platform Testing
**Estimated effort:** 3-4 hours

Test and fix Windows-specific issues.

**Actions:**
1. Test on Windows 10/11
2. Verify file paths work correctly
3. Check window behavior (minimize, maximize, taskbar)
4. Test with high DPI displays
5. Fix any Windows-specific bugs

**Acceptance Criteria:**
- [ ] Application runs correctly on Windows
- [ ] File operations work
- [ ] Window behavior is native-feeling
- [ ] High DPI supported

**Status:** [ ]

---

#### Task 8.7: macOS Platform Testing
**Estimated effort:** 3-4 hours

Test and fix macOS-specific issues.

**Actions:**
1. Test on macOS (latest + previous version if possible)
2. Verify file paths work correctly
3. Check window behavior (traffic lights, full screen)
4. Test Cmd key shortcuts
5. Fix any macOS-specific bugs

**Acceptance Criteria:**
- [ ] Application runs correctly on macOS
- [ ] File operations work
- [ ] Window behavior is native-feeling
- [ ] Cmd shortcuts work

**Status:** [ ]

---

#### Task 8.8: Linux Platform Testing
**Estimated effort:** 2-3 hours

Test on Linux (secondary platform).

**Actions:**
1. Test on Ubuntu or similar distro
2. Verify file paths work correctly
3. Check window behavior
4. Fix any Linux-specific bugs

**Acceptance Criteria:**
- [ ] Application runs correctly on Linux
- [ ] File operations work
- [ ] Reasonable window behavior

**Status:** [ ]

---

#### Task 8.9: MVP Feature Complete Review
**Estimated effort:** 3-4 hours

Perform comprehensive MVP feature verification across all platforms.

**Actions:**
1. Run complete feature checklist on each platform (Windows, macOS, Linux):
   - Multi-tab interface with vertical tabs
   - Auto-save (debounced)
   - Optional titles with content preview
   - Document search
   - Recently closed tabs
   - Session restore
   - Duplicate tab
   - Export/copy to clipboard
   - Bulk tab operations
   - Quick stats in tabs
   - Settings (storage location, theme, font)
   - All keyboard shortcuts (see Appendix)
2. Verify cross-platform consistency:
   - UI looks consistent across platforms
   - Keyboard shortcuts use platform-appropriate modifiers
   - File paths work correctly on each OS
3. Test settings on each platform:
   - Change storage directory
   - Switch theme (light/dark)
   - Change font family and size
   - Verify settings persist
4. Run final keyboard shortcut audit (from Appendix):
   - New Tab, Close Tab, Reopen Closed
   - Search, Edit Title, Duplicate Tab
   - Next/Previous Tab navigation
5. Compile list of any platform-specific issues
6. Verify no critical bugs remain
7. Document known limitations

**Acceptance Criteria:**
- [ ] All Phase 8 tasks completed
- [ ] All MVP features work on Windows
- [ ] All MVP features work on macOS
- [ ] All MVP features work on Linux
- [ ] No critical bugs
- [ ] All keyboard shortcuts work with platform-appropriate modifiers
- [ ] Settings persist correctly on all platforms

**Status:** [ ]

**Review Notes:**
```
[Add review notes here]
```

---

## Phase 9: Final Testing and Release Preparation

**Goal:** Final QA, bug fixes, and release preparation.

**Duration:** 3-4 days

### Tasks

#### Task 9.1: Performance Testing
**Estimated effort:** 3-4 hours

Test performance with large data sets.

**Actions:**
1. Create 1000+ documents for testing
2. Measure search performance
3. Measure startup time with many documents
4. Measure memory usage with many tabs open
5. Identify and fix performance bottlenecks

**Acceptance Criteria:**
- [ ] Search responds in <500ms with 1000 documents
- [ ] Startup time acceptable with 100 open tabs
- [ ] Memory usage reasonable
- [ ] No performance regressions from fixes

**Status:** [ ]

---

#### Task 9.2: Edge Case Testing
**Estimated effort:** 3-4 hours

Test error scenarios and edge cases.

**Actions:**
1. Test with storage directory deleted/unavailable
2. Test with corrupted metadata file
3. Test with disk full scenario
4. Test with very large documents (10MB+)
5. Test rapid tab operations
6. Document and fix issues found

**Acceptance Criteria:**
- [ ] Application handles all error scenarios gracefully
- [ ] No data loss in error conditions
- [ ] User notified of problems appropriately
- [ ] Recovery procedures work

**Status:** [ ]

---

#### Task 9.3: Bug Fixes
**Estimated effort:** Variable

Fix bugs found during testing.

**Actions:**
1. Triage and prioritize bugs
2. Fix critical and high-priority bugs
3. Document known issues (if any remain)
4. Regression test fixes

**Acceptance Criteria:**
- [ ] No critical bugs
- [ ] No high-priority bugs
- [ ] Known issues documented
- [ ] All fixes tested

**Status:** [ ]

---

#### Task 9.4: Documentation
**Estimated effort:** 2-3 hours

Create user and developer documentation.

**Actions:**
1. README with project overview and build instructions
2. User guide for basic operations
3. Developer setup guide
4. Document known limitations

**Acceptance Criteria:**
- [ ] README complete
- [ ] User can understand basic operations
- [ ] Developer can set up project from docs
- [ ] Limitations clearly stated

**Status:** [ ]

---

#### Task 9.5: Build and Distribution Setup
**Estimated effort:** 3-4 hours

Set up build pipeline and distribution.

**Actions:**
1. Create release build configuration
2. Set up builds for Windows, macOS, Linux
3. Create installers or packages as appropriate
4. Test installation on clean machines

**Acceptance Criteria:**
- [ ] Release builds work on all platforms
- [ ] Installation process is straightforward
- [ ] Application runs from installed location
- [ ] No missing dependencies

**Status:** [ ]

---

#### Task 9.6: MVP Release Sign-off
**Estimated effort:** 1-2 hours

Perform final release verification and sign-off.

**Actions:**
1. Verify all Phase 9 tasks completed:
   - Performance testing passed
   - Edge case testing passed
   - Bug fixes verified
   - Documentation complete
   - Build and distribution setup done
2. Final verification checklist:
   - Run release builds on Windows, macOS, Linux
   - Install from distribution package on clean machine
   - Verify application launches and core features work
   - Verify no missing dependencies
3. Review known issues list:
   - Ensure no critical or high-priority bugs remain
   - Document any deferred issues with workarounds
4. Prepare release artifacts:
   - Verify version number set correctly (1.0.0)
   - Verify release notes complete
   - Verify download links work (if applicable)
5. Obtain stakeholder sign-off
6. Tag release in git repository
7. Publish release artifacts

**Acceptance Criteria:**
- [ ] All Phase 9 tasks completed
- [ ] All features work as specified
- [ ] No critical or high-priority bugs
- [ ] Release builds verified on all target platforms
- [ ] Documentation complete and accurate
- [ ] Release artifacts published
- [ ] Git repository tagged with version

**Status:** [ ]

**Sign-off:**
```
Release approved by: _______________
Date: _______________
Version: 1.0.0
```

---

## Developer Notes Section

Use this section to record notes for future developers or tasks.

### General Notes

```
[Add general project notes here]
```

### Phase 1 Notes

```
[Add Phase 1 specific notes here]
```

### Phase 2 Notes

```
[Add Phase 2 specific notes here]
```

### Phase 3 Notes

```
[Add Phase 3 specific notes here]
```

### Phase 4 Notes

```
[Add Phase 4 specific notes here]
```

### Phase 5 Notes

```
[Add Phase 5 specific notes here]
```

### Phase 6 Notes

```
[Add Phase 6 specific notes here]
```

### Phase 7 Notes

```
[Add Phase 7 specific notes here]
```

### Phase 8 Notes

```
[Add Phase 8 specific notes here]
```

### Phase 9 Notes

```
[Add Phase 9 specific notes here]
```

---

## Appendix: Quick Reference

### Project Structure
```
Scrapile.sln
├── Scrapile.Domain/           # Entities, interfaces (no dependencies)
├── Scrapile.Application/      # Services, business logic
├── Scrapile.Infrastructure/   # File system, JSON storage
├── Scrapile.Desktop/          # Avalonia UI
├── Scrapile.Domain.Tests/     # Domain unit tests
├── Scrapile.Application.Tests/ # Service unit tests
└── Scrapile.Infrastructure.Tests/ # Integration tests
```

### Key Files
- `ProjectPlan.md` - Full specification
- `ImplementationPlan.md` - This file
- `.ephemeral_metadata.json` - Runtime metadata (in storage dir)
- `settings.json` - User settings (platform-specific location)

### Keyboard Shortcuts (MVP)
| Action | Windows/Linux | macOS |
|--------|---------------|-------|
| New Tab | Ctrl+T | Cmd+T |
| Close Tab | Ctrl+W | Cmd+W |
| Reopen Closed | Ctrl+Shift+T | Cmd+Shift+T |
| Search | Ctrl+P | Cmd+P |
| Edit Title | F2 | F2 |
| Duplicate Tab | Ctrl+Shift+D | Cmd+Shift+D |
| Next Tab | Ctrl+Tab | Cmd+Tab |
| Previous Tab | Ctrl+Shift+Tab | Cmd+Shift+Tab |

---

**Document Version:** 1.0
**Created:** January 22, 2025
**Based on:** ProjectPlan.md v1.1
