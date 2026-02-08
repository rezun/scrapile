# Scrapile - Photino.NET Migration Plan

## Overview

This plan migrates the Desktop UI layer from Avalonia to Photino.NET with a vanilla HTML/CSS/JS frontend, as described in [ADR-001](./ADR-001-migrate-to-photino.md).

**Key principle:** The existing Avalonia project stays intact and working until the Photino version reaches feature parity. Both projects coexist during migration. The Avalonia project is only deleted in the final task.

**Reference documents:**
- `docs/ADR-001-migrate-to-photino.md` - Architecture decision and rationale
- `docs/ProjectPlan.md` - Full feature specification
- `docs/ImplementationPlan.md` - Original Avalonia implementation (for reference)

---

## How to Use This Plan

### For Developers

1. **Find the next available task** - Look for tasks marked `[ ]` (not started)
2. **Mark task as in-progress** - Change `[ ]` to `[~]` and add your name and date
3. **Read the full task description** including the "What to do" section
4. **Build and verify** - Run `dotnet build Scrapile.slnx` after your changes
5. **Test manually** - Run the Photino app and verify your work (see acceptance criteria)
6. **Mark task as done** - Change `[~]` to `[x]` and add completion date
7. **Write comments** in the task's "Comments" section if anything is noteworthy
8. **Commit** with a descriptive message referencing the task (e.g., "Migration Task 2.1: Build tab list HTML/CSS")

### Task Status Legend

- `[ ]` - Not started
- `[~]` - In progress (add your name and start date)
- `[x]` - Completed (add completion date)
- `[!]` - Blocked (describe blocker in Comments section)

### Running the Projects

```bash
# Run the EXISTING Avalonia app (for reference/comparison)
dotnet run --project Scrapile.Desktop/Scrapile.Desktop.csproj

# Run the NEW Photino app
dotnet run --project Scrapile.Photino/Scrapile.Photino.csproj

# Build everything
dotnet build Scrapile.slnx
```

---

## Phase 1: Foundation

**Goal:** Get a Photino window running, serving a basic HTML page, with the C#-JS message bridge working and DI wired up. At the end of this phase you can send a message from JS to C# and get a response back.

---

### Task 1.1: Create Photino.NET Project

**Status:** [ ]
**Assignee:**
**Estimated effort:** 4-6 hours

**What to do:**

Create a new .NET project `Scrapile.Photino` that opens a Photino window showing a simple "Hello from Photino" HTML page.

1. Create the project directory `Scrapile.Photino/`
2. Create `Scrapile.Photino.csproj` targeting `net9.0` as an `Exe`
   - Add NuGet package: `Photino.NET` (latest stable)
   - Add project references to `Scrapile.Application` and `Scrapile.Infrastructure` (same as the Avalonia project)
   - Copy over the icon-related properties from `Scrapile.Desktop/Scrapile.Desktop.csproj` (`ApplicationIcon`, etc.)
3. Create `Program.cs` with a minimal Photino window:
   ```csharp
   var window = new PhotinoWindow()
       .SetTitle("Scrapile")
       .SetUseOsDefaultSize(false)
       .SetSize(new System.Drawing.Size(1200, 800))
       .Center()
       .Load("wwwroot/index.html");
   window.WaitForClose();
   ```
4. Create `wwwroot/index.html` with basic HTML:
   ```html
   <!DOCTYPE html>
   <html><head><title>Scrapile</title></head>
   <body><h1>Hello from Photino</h1></body></html>
   ```
5. Ensure `wwwroot/` files are copied to output (`<Content Include="wwwroot\**" CopyToOutputDirectory="PreserveNewest" />` in csproj)
6. Add the new project to `Scrapile.slnx`
7. Verify: `dotnet run --project Scrapile.Photino/Scrapile.Photino.csproj` opens a window showing "Hello from Photino"

**Acceptance Criteria:**
- [ ] Project builds without errors
- [ ] Window opens and shows the HTML page
- [ ] Solution still builds (`dotnet build Scrapile.slnx`)
- [ ] Existing Avalonia app still works unchanged

**Comments:**
```
```

---

### Task 1.2: Set Up HTML/CSS/JS Scaffold

**Status:** [ ]
**Assignee:**
**Estimated effort:** 4-6 hours

**What to do:**

Create the frontend file structure with basic layout CSS and a JS entry point. This is the foundation all UI will be built on top of.

1. Create the following file structure inside `Scrapile.Photino/wwwroot/`:
   ```
   wwwroot/
   ├── index.html
   ├── css/
   │   ├── reset.css        (minimal CSS reset)
   │   └── main.css         (app layout styles)
   └── js/
       └── app.js           (JS entry point)
   ```
2. `index.html` should:
   - Link the CSS files
   - Define the two-column layout skeleton (tab sidebar + editor area) matching the current app layout
   - Include `<script src="js/app.js"></script>` at end of body
   - Have placeholder content in both panels (e.g., "Tab list here" and "Editor here")
3. `reset.css` - a small CSS reset (box-sizing, margin/padding zero, etc.)
4. `main.css` - basic layout:
   - Full-height app using flexbox or CSS grid
   - Left sidebar (tab list, ~220px default width, resizable via CSS `resize` or a drag handle)
   - Right area (editor, fills remaining space)
   - Use CSS custom properties (variables) for colors so theming is easy later:
     ```css
     :root {
       --bg-primary: #ffffff;
       --bg-sidebar: #f5f5f5;
       --text-primary: #1a1a1a;
       --text-secondary: #666666;
       --accent: #0078d4;
       --border: #e0e0e0;
     }
     ```
5. `app.js` - basic JS entry point with a `DOMContentLoaded` listener. Log "Scrapile JS loaded" to console for verification.

**Acceptance Criteria:**
- [ ] Page shows two-column layout with sidebar and editor area
- [ ] CSS variables defined for later theming
- [ ] JS loads and logs to console (visible in browser DevTools if supported)
- [ ] Layout is responsive to window resizing

**Comments:**
```
```

---

### Task 1.3: Implement C# to JS Message Bridge

**Status:** [ ]
**Assignee:**
**Estimated effort:** 6-8 hours

**What to do:**

Build the communication layer between the JS frontend and C# backend. This is the critical infrastructure that replaces Avalonia's data binding. All subsequent tasks depend on this working correctly.

**Photino's messaging API:**
- JS to C#: `window.external.sendMessage(jsonString)`
- C# to JS: `window.SendMessage(jsonString)`

1. **Define the message format.** All messages are JSON with this shape:
   ```json
   { "type": "actionName", "payload": { ... } }
   ```
   For example:
   - JS to C#: `{ "type": "createTab", "payload": {} }`
   - C# to JS: `{ "type": "tabCreated", "payload": { "tabId": "...", "content": "" } }`

2. **C# side - Create `Bridge/` directory in `Scrapile.Photino/`:**
   - `Bridge/BridgeMessage.cs` - Message model class with `Type` (string) and `Payload` (JsonElement or object)
   - `Bridge/MessageRouter.cs` - Receives messages from JS, deserializes, dispatches to handler methods. Pattern:
     ```csharp
     public class MessageRouter
     {
         private readonly Dictionary<string, Func<JsonElement, Task<object?>>> _handlers = new();

         public void RegisterHandler(string messageType, Func<JsonElement, Task<object?>> handler) { ... }

         // Called by Photino's WebMessageReceived event
         public async void OnMessageReceived(object? sender, string message) { ... }
     }
     ```
   - When a handler returns a value, send the response back to JS via `window.SendMessage()`
   - Handle errors gracefully: if a handler throws, send an error message back to JS

3. **JS side - Create `js/bridge.js`:**
   - `function sendMessage(type, payload)` - serializes and calls `window.external.sendMessage()`
   - `function onMessage(callback)` - registers a callback for messages from C#
   - Photino delivers C# messages via `window.external.receiveMessage` — set up the listener
   - Export or attach to a global `Bridge` object

4. **Wire it up in `Program.cs`:**
   - Register `window.RegisterWebMessageReceivedHandler(router.OnMessageReceived)`
   - Register a test handler: `"ping"` that returns `{ "type": "pong", "payload": { "time": "..." } }`

5. **Test it in `index.html`/`app.js`:**
   - Add a test button that sends a `ping` message
   - Display the `pong` response on screen
   - Verify round-trip message passing works

**Acceptance Criteria:**
- [ ] Clicking the test button sends a message from JS to C#
- [ ] C# receives the message, processes it, and sends a response
- [ ] JS receives the response and displays it on screen
- [ ] Error handling works (malformed messages don't crash the app)
- [ ] Messages are logged to console/debug output for development visibility

**Comments:**
```
```

---

### Task 1.4: Wire Up DI and Existing Services

**Status:** [ ]
**Assignee:**
**Estimated effort:** 4-6 hours

**What to do:**

Set up dependency injection in the Photino host, reusing the same service registrations as the Avalonia app. After this task, the message bridge can access `TabManager`, `DocumentService`, `AutoSaveService`, and `SettingsService`.

1. **Create `DependencyInjection/ServiceCollectionExtensions.cs` in `Scrapile.Photino/`:**
   - Copy the service registrations from `Scrapile.Desktop/DependencyInjection/ServiceCollectionExtensions.cs`
   - Register the same infrastructure services: `ISettingsStore`, `IMetadataStore`, `IDocumentRepository`
   - Register the same application services: `SettingsService`, `DocumentService`, `AutoSaveService`, `TabManager`
   - Register `MessageRouter` as a singleton
   - Do NOT register Avalonia-specific services (ThemeService, TrayIconService, etc.) — those will be handled in later tasks
   - Keep the `GetDefaultStorageDirectory()` and `GetStorageDirectory()` helper methods

2. **Update `Program.cs`:**
   - Build the DI container before creating the Photino window
   - Resolve `MessageRouter` from DI
   - Pass the router to the Photino window's message handler
   - Initialize `TabManager` (call `InitializeAsync()` to restore session)

3. **Register initial message handlers on the router to verify DI works:**
   - `"getOpenTabs"` → Returns list of open tabs from `TabManager`
   - `"getSettings"` → Returns current settings from `SettingsService`

4. **Test in JS:**
   - On page load, send `getOpenTabs` and log the result
   - If you have existing documents from the Avalonia app, they should appear

**Acceptance Criteria:**
- [ ] DI container builds without errors
- [ ] `TabManager.InitializeAsync()` runs on startup (loads existing session)
- [ ] `getOpenTabs` message returns the open tabs
- [ ] `getSettings` message returns settings
- [ ] Existing documents/tabs from the Avalonia app are accessible

**Comments:**
```
```

---

### Task 1.5: Phase 1 Verification

**Status:** [ ]
**Assignee:**
**Estimated effort:** 2-3 hours

**What to do:**

Verify the entire foundation works end-to-end before proceeding to UI work.

1. Start the Photino app
2. Verify the window opens with the two-column layout
3. Open browser DevTools (if Photino supports it) or add on-screen debug output
4. Test the message bridge:
   - Send `ping`, verify `pong` received
   - Send `getOpenTabs`, verify tabs returned
   - Send `getSettings`, verify settings returned
   - Send a malformed message, verify error handled gracefully
5. Verify the Avalonia app still works independently
6. Review the code for any issues or patterns that will cause problems in Phase 2
7. Write any findings in the Comments section

**Acceptance Criteria:**
- [ ] All message types work correctly
- [ ] No errors in console
- [ ] Avalonia app unaffected
- [ ] Code is clean and ready for Phase 2

**Comments:**
```
```

---

## Phase 2: Core UI

**Goal:** Build the tab list, integrate CodeMirror as the editor, and wire up auto-save. At the end of this phase, you can create tabs, type content, switch between tabs, and content auto-saves. This is the key validation milestone — if this works, the migration approach is proven.

---

### Task 2.1: Build Tab List UI

**Status:** [ ]
**Assignee:**
**Estimated effort:** 6-8 hours

**What to do:**

Build the vertical tab list in the sidebar using HTML/CSS/JS, connected to the C# `TabManager` via the message bridge.

1. **Register C# message handlers in `MessageRouter`:**
   - `"createTab"` → Calls `TabManager.CreateTabAsync()`, returns new tab info
   - `"closeTab"` → Calls `TabManager.CloseTabAsync(tabId)`, saves content first via `AutoSaveService`
   - `"selectTab"` → Returns the tab's content (from `TabManager`)
   - `"getOpenTabs"` → Returns all open tabs with display info (title/preview, word count, dirty state)

2. **Create `js/tabList.js`:**
   - Renders the tab list in the sidebar from data received via bridge
   - Each tab item shows:
     - Title (bold) or content preview (normal weight) — same display as current Avalonia app
     - Word count subtitle (e.g., "245 words")
     - Close button (X)
     - Dirty indicator (dot) for unsaved changes
   - Selected tab has a visual highlight
   - "New Tab" button at the bottom
   - Clicking a tab sends `selectTab` to C#
   - Clicking close sends `closeTab` to C#
   - Clicking "New Tab" sends `createTab` to C#

3. **Push updates from C# to JS:**
   - After a tab is created/closed, send a `tabsUpdated` message to JS with the updated tab list
   - After auto-save completes, send `tabSaved` to JS so the dirty indicator clears

4. **CSS styling** in `main.css`:
   - Style tab items with hover, selected, and dirty states
   - Scrollable tab list when many tabs
   - Match the general visual feel of the current Avalonia app

**Look at these files for reference** (what the tab list currently does):
- `Scrapile.Desktop/Views/TabListView.axaml` - Current tab list layout
- `Scrapile.Desktop/ViewModels/TabListViewModel.cs` - Current tab list logic
- `Scrapile.Desktop/ViewModels/TabItemViewModel.cs` - Current tab item data

**Acceptance Criteria:**
- [ ] Tab list renders with correct display (title/preview, word count)
- [ ] Clicking "New Tab" creates a tab and it appears in the list
- [ ] Clicking a tab selects it (visual highlight)
- [ ] Clicking close button removes the tab
- [ ] Dirty indicator shows for unsaved tabs
- [ ] List is scrollable with many tabs

**Comments:**
```
```

---

### Task 2.2: Integrate CodeMirror 6 as the Editor

**Status:** [ ]
**Assignee:**
**Estimated effort:** 6-8 hours

**What to do:**

Replace the Avalonia TextBox/AvaloniaEdit with CodeMirror 6 in the editor area. CodeMirror will handle all text editing, syntax highlighting, and keyboard input within the editor.

1. **Add CodeMirror 6 to the project:**
   - Option A (recommended for simplicity): Download a pre-built CodeMirror bundle (from npm or CDN) and place it in `wwwroot/lib/codemirror/`
   - Option B: Set up a minimal build step (e.g., using esbuild or rollup) to bundle CodeMirror modules. If you choose this, document the build step clearly for future developers.
   - Required CodeMirror extensions: basic setup, language support (at minimum: plaintext, markdown, javascript, python, csharp, json, html, css, sql, xml, yaml, bash, typescript, go, rust, java, cpp)

2. **Create `js/editor.js`:**
   - Initialize CodeMirror in the editor area div
   - Expose functions:
     - `setContent(text)` - Set the editor content (used when switching tabs)
     - `getContent()` - Get the current editor content
     - `setLanguage(langId)` - Set the syntax highlighting language
     - `setReadOnly(bool)` - Toggle read-only mode
   - On content change, send a `contentChanged` message to C# (debounced at ~100ms on the JS side to avoid flooding the bridge — the real auto-save debounce is 500ms on the C# side)
   - On focus, notify C# (optional, for tracking active tab)

3. **Register C# message handlers:**
   - `"contentChanged"` → Calls `AutoSaveService.ScheduleSaveAsync(documentId, content)` and marks tab dirty
   - `"getTabContent"` → Returns content for a specific tab

4. **Wire tab selection to editor:**
   - When a tab is selected in JS, request its content via `getTabContent`
   - Load the content into CodeMirror via `setContent()`
   - Detect language from file content or settings and set syntax highlighting

5. **Add CodeMirror CSS to `index.html`**

**Look at these files for reference:**
- `Scrapile.Desktop/Views/EditorView.axaml` - Current editor layout
- `Scrapile.Desktop/ViewModels/EditorViewModel.cs` - Current editor logic
- `Scrapile.Desktop/Behaviors/DocumentTextBindingBehavior.cs` - Current text binding

**Acceptance Criteria:**
- [ ] CodeMirror renders in the editor area
- [ ] Selecting a tab loads its content into the editor
- [ ] Typing in the editor sends content changes to C#
- [ ] Auto-save triggers after content changes (verify file on disk updates)
- [ ] Syntax highlighting works for at least plain text and one programming language
- [ ] Editor fills the available space and resizes with the window

**Comments:**
```
```

---

### Task 2.3: Title Bar and Save Status

**Status:** [ ]
**Assignee:**
**Estimated effort:** 4-6 hours

**What to do:**

Add the title editing field above the editor and the save status indicator. This matches the current Avalonia app's title editing UX.

1. **Title field (above the editor):**
   - HTML input field with placeholder "Add a title (optional)..."
   - On change, send `updateTitle` message to C# with `{ tabId, title }`
   - Empty/whitespace title = no title (C# normalizes this to `null`)
   - When switching tabs, update the title field from tab data
   - F2 keyboard shortcut focuses the title field
   - Enter key in title field moves focus to the editor
   - Escape key in title field reverts to the previous value

2. **Register C# message handlers:**
   - `"updateTitle"` → Calls `DocumentService.UpdateTitleAsync(documentId, title)`, updates tab display name, sends `tabsUpdated` to JS

3. **Save status indicator:**
   - Small text near the title area showing: "Saving...", "Saved", or nothing
   - C# sends `saveStatusChanged` message with status text
   - Status clears after ~2 seconds

4. **Empty state:**
   - When no tab is selected, show a centered message "No tabs open — press Ctrl+T to create one" (Cmd+T on macOS)
   - Hide the title field and editor when no tab is selected

**Look at these files for reference:**
- `Scrapile.Desktop/Views/EditorView.axaml` - Title field and save status layout
- `Scrapile.Desktop/ViewModels/EditorViewModel.cs` - Title change handling

**Acceptance Criteria:**
- [ ] Title field shows and edits the current tab's title
- [ ] Title changes persist (visible in tab list and on app restart)
- [ ] F2 focuses the title field, Enter moves to editor, Escape reverts
- [ ] Save status shows "Saving..." and "Saved" at appropriate times
- [ ] Empty state shows when no tabs are open

**Comments:**
```
```

---

### Task 2.4: Keyboard Shortcuts (Core Set)

**Status:** [ ]
**Assignee:**
**Estimated effort:** 4-6 hours

**What to do:**

Implement the core keyboard shortcuts in JavaScript. The shortcuts must be platform-aware (Cmd on macOS, Ctrl on Windows/Linux).

1. **Create `js/shortcuts.js`:**
   - Detect platform: `navigator.platform` or `navigator.userAgent` to check for macOS
   - Set `modKey` to `Meta` (Cmd) on macOS, `Control` (Ctrl) elsewhere
   - Register `keydown` event listener on `document`
   - Implement shortcuts:
     | Shortcut | Action |
     |----------|--------|
     | Mod+T | Send `createTab` to C# |
     | Mod+W | Send `closeTab` for current tab |
     | Mod+Tab | Select next tab (send `selectTab` with next tab ID) |
     | Mod+Shift+Tab | Select previous tab |
     | Mod+Shift+T | Send `reopenLastClosed` to C# |
     | Mod+P or Mod+K | Open search overlay (Task 3.1) — for now, just log it |
     | Mod+Shift+D | Send `duplicateTab` for current tab |
     | Mod+Shift+E or F2 | Focus title field |
     | Mod+Shift+C | Send `copyToClipboard` for current tab |
     | Mod+Shift+S | Send `saveAs` for current tab |
     | Escape | Close search overlay / cancel title edit |

   - Prevent default browser behavior for these shortcuts (`e.preventDefault()`)

2. **Register C# message handlers for new actions:**
   - `"reopenLastClosed"` → Calls `TabManager.ReopenLastClosedAsync()`, sends `tabsUpdated`
   - `"duplicateTab"` → Calls `TabManager.DuplicateTabAsync(tabId)`, sends `tabsUpdated`
   - `"copyToClipboard"` → Returns content to JS; JS uses `navigator.clipboard.writeText()` to copy

3. **Tab cycling logic in JS:**
   - Maintain the current tab index in JS state
   - Mod+Tab cycles forward (wraps at end), Mod+Shift+Tab cycles backward

**Look at these files for reference:**
- `Scrapile.Desktop/Views/MainWindow.axaml.cs` - Current keyboard shortcut handling (lines with `OnKeyDown`)

**Acceptance Criteria:**
- [ ] All shortcuts listed above work
- [ ] Platform-appropriate modifier key used (Cmd on macOS, Ctrl on Windows/Linux)
- [ ] Shortcuts don't conflict with CodeMirror's built-in shortcuts
- [ ] Browser default behavior prevented (e.g., Ctrl+W doesn't close the Photino window)
- [ ] Mod+Tab cycles through tabs correctly

**Comments:**
```
```

---

### Task 2.5: Phase 2 Verification

**Status:** [ ]
**Assignee:**
**Estimated effort:** 3-4 hours

**What to do:**

This is the **key validation milestone**. If this works well, the migration approach is proven. Test thoroughly.

1. **Full workflow test:**
   - Launch the Photino app
   - Create 3-4 tabs
   - Type content in each tab
   - Switch between tabs — verify content is correct for each tab
   - Add a title to one tab — verify it shows in the tab list
   - Close a tab — verify it disappears
   - Reopen last closed tab (Mod+Shift+T) — verify it returns
   - Close the app and reopen — verify all tabs and content restored

2. **Auto-save verification:**
   - Type content, wait 1 second
   - Check the document file on disk (in `~/Documents/Scrapile/`) — verify content saved
   - Type more content, immediately close the tab — verify content saved (immediate save on close)

3. **Editor verification:**
   - Paste a large block of text — verify it renders correctly
   - Test word wrap behavior
   - Test undo/redo (Mod+Z, Mod+Shift+Z) — handled by CodeMirror
   - Test syntax highlighting (if configured for a language)

4. **Keyboard shortcuts:**
   - Test every shortcut from Task 2.4
   - Verify on macOS (Cmd) and Windows/Linux (Ctrl) if possible

5. **Compare with the Avalonia app:**
   - Run both apps side by side
   - Note any significant UX differences
   - Document anything missing that should be addressed in Phase 3

6. **Write findings in Comments**

**Acceptance Criteria:**
- [ ] Create, edit, switch, and close tabs works
- [ ] Auto-save works (content persists to disk)
- [ ] Session restore works (tabs survive app restart)
- [ ] Keyboard shortcuts work
- [ ] No data loss in any scenario
- [ ] CodeMirror editor is functional and responsive

**Comments:**
```
```

---

## Phase 3: Features

**Goal:** Implement the remaining UI features: search overlay, find bar, settings, dialogs, and recently closed panel.

---

### Task 3.1: Search Overlay

**Status:** [ ]
**Assignee:**
**Estimated effort:** 6-8 hours

**What to do:**

Build the global document search overlay in HTML/CSS/JS. This is the Cmd+P / Ctrl+P overlay that searches across all documents.

1. **Create `js/search.js` and add HTML structure in `index.html`:**
   - Overlay with semi-transparent backdrop (clicking backdrop closes it)
   - Centered modal with:
     - Search input field (auto-focused when opened)
     - Results list below
   - Hidden by default, shown when Mod+P/Mod+K is pressed

2. **Search result display:**
   - Each result shows:
     - Title (bold) or content preview (normal) as primary line
     - Content snippet as secondary line
     - Last modified date as tertiary line (relative: "2 hours ago", "Yesterday", etc.)
   - Selected result highlighted with accent color
   - "No documents found" message for empty results
   - "Type to search all documents" hint when input is empty

3. **Keyboard navigation:**
   - Up/Down arrows navigate results (with wrap-around)
   - Enter opens the selected result
   - Escape closes the overlay

4. **Register C# message handlers:**
   - `"search"` → Calls `DocumentService.SearchAsync(query)`, returns results with display info
   - `"openDocument"` → Calls `TabManager.OpenDocumentInTabAsync(documentId)` or focuses existing tab, sends `tabsUpdated`

5. **Wire it up:**
   - JS sends `search` message on every keystroke (debounced ~100ms in JS)
   - On Enter, send `openDocument` with the selected result's document ID
   - Close the overlay after opening a document
   - Check if document is already open in a tab — if so, just select that tab

**Look at these files for reference:**
- `Scrapile.Desktop/Views/SearchOverlay.axaml` - Current search layout
- `Scrapile.Desktop/ViewModels/SearchViewModel.cs` - Current search logic
- `Scrapile.Desktop/ViewModels/SearchResultItemViewModel.cs` - Search result data

**Acceptance Criteria:**
- [ ] Mod+P opens search overlay, Escape closes it
- [ ] Typing filters results in real-time
- [ ] Results display correctly (title/preview, snippet, date)
- [ ] Keyboard navigation works (Up/Down/Enter/Escape)
- [ ] Opening a result opens or focuses the document
- [ ] Clicking the backdrop closes the overlay

**Comments:**
```
```

---

### Task 3.2: In-Document Find Bar

**Status:** [ ]
**Assignee:**
**Estimated effort:** 3-4 hours

**What to do:**

Implement find (and optionally find-and-replace) within the current document using CodeMirror's built-in search extension.

1. **Enable CodeMirror search extension:**
   - If not already included in the CodeMirror bundle, add `@codemirror/search`
   - CodeMirror's built-in Mod+F opens the find panel
   - CodeMirror's built-in Mod+H (or Mod+Shift+F) opens find-and-replace

2. **Verify it works:**
   - Mod+F should open CodeMirror's find bar within the editor
   - It should support case-sensitive, regex, and whole-word options
   - Escape should close the find bar

3. **If CodeMirror's built-in find bar is sufficient, this task is mostly verification.** If customization is needed (different styling, different shortcuts), document what was changed.

4. **Ensure no conflicts:**
   - Mod+F should be handled by CodeMirror, not by the global shortcuts handler
   - The global `shortcuts.js` should not intercept Mod+F

**Look at this file for reference:**
- `Scrapile.Desktop/Views/FindBarView.axaml` - Current find bar (for feature comparison only — CodeMirror handles this natively)

**Acceptance Criteria:**
- [ ] Mod+F opens the find bar in the editor
- [ ] Find works (highlights matches, navigates between them)
- [ ] Find-and-replace works (if included)
- [ ] Escape closes the find bar
- [ ] No shortcut conflicts

**Comments:**
```
```

---

### Task 3.3: Settings Window

**Status:** [ ]
**Assignee:**
**Estimated effort:** 6-8 hours

**What to do:**

Build a settings UI. This can either be an HTML modal within the main page, or a second Photino window. An HTML modal is simpler and recommended.

1. **Create `js/settings.js` and settings HTML structure:**
   - Settings sections (matching the current Avalonia settings window):
     - **Appearance:** Theme (Light/Dark/System), Tab Position (Left/Right)
     - **Editor:** Font Family (dropdown), Font Size (number input or slider)
     - **Saving:** Auto-Save Delay (slider, 100ms-5000ms)
     - **Storage:** Document Storage Directory (text input + Browse button)
     - **Reset to Defaults** button
   - Changes apply immediately (no Save/Cancel buttons)
   - Show the settings file path at the bottom for reference

2. **Register C# message handlers:**
   - `"getSettings"` → Returns current settings object (already done in Task 1.4)
   - `"updateSetting"` → Takes `{ key, value }`, calls appropriate `SettingsService` setter
   - `"browseDirectory"` → Opens native folder picker dialog (Photino may have a dialog API, or use a platform-specific approach), returns selected path
   - `"resetSettings"` → Calls `SettingsService.ResetToDefaultsAsync()`, returns new settings

3. **Apply settings in JS:**
   - When theme changes, update CSS variables or toggle a dark theme class
   - When font changes, update CodeMirror's font family and size
   - When tab position changes, swap sidebar to left or right via CSS

4. **Open settings via:**
   - Mod+, (comma) keyboard shortcut
   - Or a settings gear icon in the UI

**Look at these files for reference:**
- `Scrapile.Desktop/Views/SettingsWindow.axaml` - Current settings layout
- `Scrapile.Desktop/ViewModels/SettingsViewModel.cs` - Current settings logic

**Acceptance Criteria:**
- [ ] Settings modal/window opens and shows current settings
- [ ] Each setting can be changed and persists across app restarts
- [ ] Theme switching works visually
- [ ] Font changes apply to the editor
- [ ] Browse button opens a native folder picker (or shows a text input as fallback)
- [ ] Reset to Defaults works

**Comments:**
```
```

---

### Task 3.4: Message and Confirmation Dialogs

**Status:** [ ]
**Assignee:**
**Estimated effort:** 3-4 hours

**What to do:**

Build a reusable HTML dialog component for messages and confirmations. The current Avalonia app uses `MessageDialog.axaml` for these.

1. **Create `js/dialog.js`:**
   - `showDialog({ title, message, buttons })` → Returns a Promise that resolves with the clicked button
   - Buttons can be: OK, Cancel, Yes, No (configurable)
   - Dialog appears as a centered modal with backdrop
   - Keyboard: Enter confirms, Escape cancels
   - Example usage:
     ```js
     const result = await showDialog({
       title: "Close All Tabs",
       message: "Are you sure you want to close all open tabs?",
       buttons: ["Cancel", "Close All"]
     });
     ```

2. **Use the dialog where needed:**
   - "Close All Tabs" confirmation (from context menu)
   - Any error messages from C# (e.g., storage directory not found)
   - C# can send a `showDialog` message to JS to trigger a dialog

3. **Style the dialog** in `main.css`:
   - Clean, minimal design matching the rest of the app
   - Backdrop, centered card, title, message, buttons row

**Look at this file for reference:**
- `Scrapile.Desktop/Views/MessageDialog.axaml` - Current dialog layout

**Acceptance Criteria:**
- [ ] Dialog renders correctly with title, message, and buttons
- [ ] Returns the user's choice (which button was clicked)
- [ ] Keyboard works (Enter/Escape)
- [ ] Multiple dialogs don't stack incorrectly
- [ ] Dialog is reusable from any JS module

**Comments:**
```
```

---

### Task 3.5: Tab Context Menu and Recently Closed Panel

**Status:** [ ]
**Assignee:**
**Estimated effort:** 6-8 hours

**What to do:**

Add the right-click context menu on tabs and the "Recently Closed" section at the bottom of the tab list.

1. **Tab context menu (right-click):**
   - Create a custom HTML context menu (or use a small library)
   - Menu items:
     - Close Tab
     - Close All Tabs (with confirmation dialog from Task 3.4)
     - Close Tabs Above
     - Close Tabs Below
     - Duplicate Tab
     - Edit Title
     - Copy to Clipboard
     - Save As...
   - Register C# message handlers for bulk operations:
     - `"closeAllTabs"` → Closes all tabs (saves dirty ones first)
     - `"closeTabsAbove"` → Closes tabs above the specified tab
     - `"closeTabsBelow"` → Closes tabs below the specified tab
   - `"saveAs"` → Returns content and suggested filename; JS uses a download approach or C# provides a native save dialog

2. **Recently Closed panel:**
   - Collapsible section at the bottom of the tab sidebar
   - Header: "Recently Closed" with expand/collapse chevron and count badge
   - Items show: title/preview and relative close time
   - Clicking an item reopens it
   - Register C# message handler:
     - `"getRecentlyClosed"` → Returns recently closed items list
     - `"reopenDocument"` → Opens specific document from recently closed

3. **Refresh the recently closed list** whenever a tab is closed or reopened

**Look at these files for reference:**
- `Scrapile.Desktop/Views/TabListView.axaml` - Current context menu and recently closed section
- `Scrapile.Desktop/ViewModels/TabListViewModel.cs` - Bulk close logic and recently closed management
- `Scrapile.Desktop/ViewModels/RecentlyClosedMenuItemViewModel.cs` - Recently closed item display

**Acceptance Criteria:**
- [ ] Right-click on tab shows context menu
- [ ] All context menu actions work correctly
- [ ] Close All Tabs shows confirmation dialog
- [ ] Recently Closed section shows at bottom of tab list
- [ ] Clicking a recently closed item reopens it
- [ ] Recently closed list updates when tabs are closed/reopened

**Comments:**
```
```

---

### Task 3.6: Phase 3 Verification

**Status:** [ ]
**Assignee:**
**Estimated effort:** 3-4 hours

**What to do:**

Test all Phase 3 features and compare with the Avalonia app for feature parity.

1. **Search overlay:**
   - Create 5+ documents with known titles/content
   - Test partial matches, case-insensitive search, content-only search
   - Test keyboard-only workflow (Mod+P → type → arrow keys → Enter)
   - Test opening already-open documents (should focus existing tab)

2. **Find bar:**
   - Open Mod+F, search for text within a document
   - Navigate between matches
   - Test find-and-replace if available

3. **Settings:**
   - Change each setting, restart app, verify persistence
   - Test theme switching
   - Test font size change (should apply to editor)

4. **Dialogs:**
   - Trigger "Close All Tabs" → verify confirmation dialog appears
   - Test keyboard (Enter/Escape) in dialog

5. **Context menu and recently closed:**
   - Test every context menu action
   - Verify recently closed panel updates correctly
   - Close 5 tabs, verify they appear in recently closed, reopen them

6. **Feature parity check against Avalonia app:**
   - Run both apps side by side
   - Make a checklist of any missing features
   - Document any intentional differences

**Acceptance Criteria:**
- [ ] All Phase 3 features work
- [ ] Search finds documents correctly
- [ ] Settings persist
- [ ] No regressions in Phase 2 functionality
- [ ] Feature parity checklist documented

**Comments:**
```
```

---

## Phase 4: Platform Integration

**Goal:** Get platform-specific features working: system tray, global hotkeys, autorun, and native dialogs.

---

### Task 4.1: System Tray Icon

**Status:** [ ]
**Assignee:**
**Estimated effort:** 6-8 hours

**What to do:**

The current Avalonia app has a system tray icon with a context menu (Show/Hide, Settings, Quit). Evaluate how to implement this with Photino.

1. **Research Photino's tray support:**
   - Check if Photino.NET has a tray icon API
   - If not, evaluate alternatives:
     - [H.NotifyIcon](https://github.com/HavenDV/H.NotifyIcon) (cross-platform tray icon library for .NET)
     - Platform-specific implementations (as last resort)

2. **Implement tray icon:**
   - Show the Scrapile icon in the system tray
   - Context menu items:
     - Show/Hide Scrapile (toggles main window visibility)
     - Settings... (sends message to JS to open settings)
     - Quit Scrapile (graceful shutdown)
   - Single-click on tray icon toggles window visibility

3. **Window hide behavior:**
   - Closing the window (X button) should hide it, not quit the app
   - The app keeps running in the system tray
   - "Quit" from tray menu actually exits the process

4. **If tray is not feasible** on one or more platforms, document it clearly and fall back to normal window close behavior (closing = quitting).

**Look at this file for reference:**
- `Scrapile.Desktop/Services/TrayIconService.cs` - Current tray implementation

**Acceptance Criteria:**
- [ ] Tray icon appears on at least one platform
- [ ] Context menu works (Show/Hide, Settings, Quit)
- [ ] Closing window hides it (app stays running in tray)
- [ ] Tray icon click toggles window visibility
- [ ] Document which platforms are supported

**Comments:**
```
```

---

### Task 4.2: Global Hotkeys

**Status:** [ ]
**Assignee:**
**Estimated effort:** 4-6 hours

**What to do:**

The current app uses SharpHook for global hotkeys (e.g., show/hide the app with a system-wide shortcut). SharpHook is not Avalonia-dependent, so it should work with Photino.

1. **Copy `GlobalHotkeyService.cs` from `Scrapile.Desktop/Services/`** to `Scrapile.Photino/Services/` (or reference it directly if the code is clean enough)
2. **Wire up the global hotkey:**
   - On startup, read the configured hotkey from settings
   - Register it via `GlobalHotkeyService`
   - When triggered, toggle window visibility (show/hide)
3. **Test on available platforms:**
   - Windows: Should work directly
   - macOS: Needs Accessibility permission (same as current app)
   - Linux: X11 only (same limitation as current app)
4. **Wire up settings changes** — if the user changes the hotkey in settings, re-register it

**Look at these files for reference:**
- `Scrapile.Desktop/Services/GlobalHotkeyService.cs` - Full implementation
- `Scrapile.Desktop/App.axaml.cs` - How the hotkey is initialized and connected

**Acceptance Criteria:**
- [ ] Global hotkey toggles window visibility
- [ ] Hotkey configurable via settings
- [ ] Works on at least one platform
- [ ] Proper cleanup on app exit (hook disposed)

**Comments:**
```
```

---

### Task 4.3: Autorun and macOS Dock Service

**Status:** [ ]
**Assignee:**
**Estimated effort:** 3-4 hours

**What to do:**

Verify that the existing platform-specific autorun and macOS dock services work with the Photino host. These services don't depend on Avalonia.

1. **Copy from `Scrapile.Desktop/Services/`:**
   - `AutorunService.cs` - Creates startup entries (plist on macOS, registry on Windows, desktop file on Linux)
   - `MacOSDockService.cs` - Shows/hides macOS dock icon via ObjC runtime
2. **Register in DI** and wire up:
   - Autorun: Controlled by settings. When user enables/disables autorun in settings, call `AutorunService` accordingly
   - Dock: On macOS, when the app has a tray icon, optionally hide the dock icon
3. **Test:**
   - Enable autorun in settings, restart machine (or log out/in), verify app starts
   - On macOS, verify dock icon can be hidden/shown
4. **If something doesn't work** because of path differences (the Photino app binary is in a different location than the Avalonia app), fix the path detection in `AutorunService`

**Look at these files for reference:**
- `Scrapile.Desktop/Services/AutorunService.cs`
- `Scrapile.Desktop/Services/MacOSDockService.cs`

**Acceptance Criteria:**
- [ ] Autorun toggle in settings works
- [ ] App starts on login when autorun is enabled
- [ ] macOS dock icon hide/show works (macOS only)
- [ ] Paths are correct for the Photino app binary

**Comments:**
```
```

---

### Task 4.4: Native File Dialogs

**Status:** [ ]
**Assignee:**
**Estimated effort:** 3-4 hours

**What to do:**

Wire up native file picker dialogs for "Save As" and "Browse for storage directory" features.

1. **Research Photino's dialog API:**
   - Photino.NET may have built-in dialog methods (e.g., `PhotinoWindow.ShowOpenDialog()`, `PhotinoWindow.ShowSaveDialog()`)
   - If not, evaluate alternatives (P/Invoke or helper libraries)

2. **Implement dialogs:**
   - **Save As:** When user triggers "Save As" (Mod+Shift+S or context menu), show a native save file dialog. Default filename based on document title. Save content to chosen location.
   - **Browse Directory:** When user clicks "Browse" in settings for storage directory, show a native folder picker dialog. Return selected path to settings.

3. **Wire up C# message handlers:**
   - `"showSaveDialog"` → Opens native save dialog, writes content to chosen file, returns success/failure
   - `"showFolderDialog"` → Opens native folder picker, returns selected path

**Acceptance Criteria:**
- [ ] Save As opens a native file dialog and saves the file
- [ ] Browse for directory opens a native folder picker
- [ ] Dialogs work on the current platform
- [ ] Canceling a dialog is handled gracefully

**Comments:**
```
```

---

### Task 4.5: Welcome Window

**Status:** [ ]
**Assignee:**
**Estimated effort:** 4-6 hours

**What to do:**

Implement the first-run welcome experience. Currently, the Avalonia app shows a `WelcomeWindow` on first launch to set up the storage directory, global hotkey, and autorun preference.

1. **Decide on approach:**
   - Option A: Show the welcome as a page within the main Photino window (before the main UI loads)
   - Option B: Show a separate, smaller Photino window for the welcome flow
   - Option A is simpler and recommended

2. **Create `js/welcome.js` and welcome HTML:**
   - Welcome heading: "Welcome to Scrapile"
   - Step 1: Storage directory selection (default shown, Browse button to change)
   - Step 2: Global hotkey configuration (text input showing default shortcut, or a key capture UI)
   - Step 3: Autorun preference (checkbox "Start Scrapile on login")
   - "Get Started" button to save settings and proceed to the main app

3. **First-run detection:**
   - C# checks if settings file exists on startup (same logic as current `App.axaml.cs`)
   - If first run, send a `showWelcome` message to JS, or load the welcome page first
   - After welcome is completed, JS sends `welcomeComplete` to C# with the chosen settings
   - C# saves settings and transitions to the main app

4. **Register C# message handler:**
   - `"welcomeComplete"` → Saves settings (storage dir, hotkey, autorun), initializes services, transitions UI to main app

**Look at these files for reference:**
- `Scrapile.Desktop/Views/WelcomeWindow.axaml` - Current welcome layout
- `Scrapile.Desktop/ViewModels/WelcomeViewModel.cs` - Current welcome logic
- `Scrapile.Desktop/App.axaml.cs` - First-run detection flow

**Acceptance Criteria:**
- [ ] First-run shows the welcome flow
- [ ] User can configure storage directory, hotkey, and autorun
- [ ] Settings are saved and the main app loads
- [ ] Subsequent launches skip the welcome and go straight to the main app
- [ ] Deleting the settings file triggers the welcome flow again

**Comments:**
```
```

---

### Task 4.6: Phase 4 Verification

**Status:** [ ]
**Assignee:**
**Estimated effort:** 3-4 hours

**What to do:**

Test all platform integration features.

1. **System tray:**
   - Verify tray icon appears
   - Test all context menu items
   - Test window show/hide via tray

2. **Global hotkey:**
   - Set a hotkey in settings
   - Press it from another application — verify Scrapile appears/hides
   - Change the hotkey — verify the new one works

3. **Autorun:**
   - Enable autorun, restart, verify app starts
   - Disable autorun, restart, verify app doesn't start

4. **File dialogs:**
   - Test Save As dialog
   - Test Browse Directory dialog in settings

5. **Welcome flow:**
   - Delete the settings file
   - Start the app — verify welcome flow appears
   - Complete the welcome — verify main app loads correctly
   - Restart — verify welcome does not appear again

6. **Test on multiple platforms if possible** (at minimum, the developer's platform)

**Acceptance Criteria:**
- [ ] All platform features work on the developer's platform
- [ ] Any platform-specific limitations documented in Comments
- [ ] No regressions in previous phases

**Comments:**
```
```

---

## Phase 5: Polish and Cutover

**Goal:** Add theming, thorough cross-platform testing, update publish scripts, and remove the Avalonia project.

---

### Task 5.1: Theme Support (Light/Dark) via CSS

**Status:** [ ]
**Assignee:**
**Estimated effort:** 4-6 hours

**What to do:**

Implement proper light and dark themes using CSS custom properties.

1. **Define theme variables in `main.css`:**
   ```css
   :root, [data-theme="light"] {
     --bg-primary: #ffffff;
     --bg-sidebar: #f5f5f5;
     --bg-editor: #ffffff;
     --text-primary: #1a1a1a;
     --text-secondary: #666666;
     --accent: #0078d4;
     --border: #e0e0e0;
     /* ... more as needed */
   }

   [data-theme="dark"] {
     --bg-primary: #1e1e1e;
     --bg-sidebar: #252526;
     --bg-editor: #1e1e1e;
     --text-primary: #d4d4d4;
     --text-secondary: #808080;
     --accent: #0078d4;
     --border: #3c3c3c;
     /* ... more as needed */
   }
   ```

2. **Apply theme:**
   - All CSS should use `var(--variable-name)` instead of hardcoded colors
   - Theme applied by setting `data-theme` attribute on `<html>` element
   - When settings change, JS updates `document.documentElement.dataset.theme`

3. **CodeMirror theming:**
   - Create or use a dark theme for CodeMirror that matches the app's dark theme
   - Switch CodeMirror theme when app theme changes

4. **System theme detection:**
   - "System" theme option uses `window.matchMedia('(prefers-color-scheme: dark)')`
   - Listen for changes (user switches OS theme while app is running)

5. **Ensure all UI elements are themed:**
   - Tab list (background, text, hover, selected states)
   - Editor area
   - Search overlay
   - Settings modal
   - Dialogs
   - Context menus
   - Scrollbars (if custom-styled)

**Acceptance Criteria:**
- [ ] Light theme looks good
- [ ] Dark theme looks good
- [ ] Theme switching works without restart
- [ ] System theme detection works
- [ ] CodeMirror theme matches the app theme
- [ ] All UI components are properly themed

**Comments:**
```
```

---

### Task 5.2: UI Polish and Accessibility

**Status:** [ ]
**Assignee:**
**Estimated effort:** 4-6 hours

**What to do:**

Polish the UI to match or exceed the Avalonia app's visual quality. Fix any rough edges.

1. **Visual polish:**
   - Consistent spacing and padding
   - Proper font sizes and weights
   - Smooth transitions/animations where appropriate (e.g., tab list changes, overlay fade-in)
   - Resizable sidebar (CSS `resize: horizontal` or a draggable handle)
   - Proper focus indicators for keyboard navigation

2. **Accessibility:**
   - Proper `aria-` attributes on interactive elements
   - Focus management (tabbing between elements works logically)
   - Screen reader-friendly labels

3. **Responsive behavior:**
   - Window can be resized to small sizes without breaking layout
   - Sidebar collapses gracefully at small widths

4. **Cross-browser consistency:**
   - WebKit (macOS) and WebView2 (Windows) may render slightly differently
   - Test on available platforms and fix any visual discrepancies

5. **Compare with Avalonia app** and address any noticeable quality gaps

**Acceptance Criteria:**
- [ ] UI looks polished and professional
- [ ] Keyboard navigation works throughout the app
- [ ] Window resizing works at all sizes
- [ ] No visual glitches or misalignments

**Comments:**
```
```

---

### Task 5.3: Graceful Startup and Shutdown

**Status:** [ ]
**Assignee:**
**Estimated effort:** 4-6 hours

**What to do:**

Ensure the app starts and shuts down cleanly, saving all state. This covers the lifecycle features from the current Avalonia app's `App.axaml.cs`.

1. **Startup sequence:**
   - Check for settings file (first run → welcome flow)
   - Acquire storage lock (prevent multiple instances running simultaneously)
   - Initialize DI and services
   - Restore session (open tabs, active tab)
   - Initialize tray icon and global hotkey
   - Send initial state to JS (tabs, settings, theme)

2. **Shutdown sequence:**
   - Save all pending changes (`AutoSaveService.SaveImmediatelyAsync()` for all dirty tabs)
   - Save active tab ID
   - Dispose global hotkey service
   - Dispose tray icon service
   - Release storage lock
   - Dispose service provider

3. **Window close behavior:**
   - If tray icon is active: closing window hides it (don't quit)
   - If tray icon is not available: closing window quits the app
   - "Quit" from tray menu runs the full shutdown sequence

4. **Handle edge cases:**
   - App crash recovery: storage lock should have a timeout or be file-based
   - Force-quit: accept minimal data loss (only unsaved debounce window)

**Look at this file for reference:**
- `Scrapile.Desktop/App.axaml.cs` - Current startup and shutdown sequences

**Acceptance Criteria:**
- [ ] App starts cleanly, restores session
- [ ] App shuts down cleanly, saves all state
- [ ] Storage lock prevents multiple instances
- [ ] Window close hides to tray when tray is available
- [ ] No data loss on normal shutdown

**Comments:**
```
```

---

### Task 5.4: Cross-Platform Testing (macOS)

**Status:** [ ]
**Assignee:**
**Estimated effort:** 4-6 hours

**What to do:**

Test the complete Photino app on macOS. This platform uses WebKit as the webview.

1. **Build and run:**
   - `dotnet run --project Scrapile.Photino/Scrapile.Photino.csproj`
   - Verify it launches without errors

2. **Test all features:**
   - Tab management (create, switch, close, duplicate)
   - Editor (type, auto-save, syntax highlighting)
   - Search overlay (Cmd+P)
   - Find bar (Cmd+F)
   - Settings (Cmd+,)
   - Recently closed (Cmd+Shift+T)
   - Context menus (right-click)
   - Save As (Cmd+Shift+S)
   - Copy to clipboard (Cmd+Shift+C)
   - All keyboard shortcuts use Cmd (not Ctrl)

3. **Platform-specific features:**
   - System tray icon
   - Global hotkey (may need Accessibility permission)
   - Autorun (LaunchAgent plist)
   - macOS dock icon hide/show
   - OS spell checking (should work automatically in WebKit!)

4. **Visual verification:**
   - Dark and light themes
   - Font rendering
   - Window resize behavior
   - Native-feeling window chrome

5. **Document any issues** in Comments section

**Acceptance Criteria:**
- [ ] App launches and all features work on macOS
- [ ] Cmd-based shortcuts work correctly
- [ ] OS spell checking works in the editor
- [ ] Platform-specific features work or have documented limitations
- [ ] No visual glitches

**Comments:**
```
```

---

### Task 5.5: Cross-Platform Testing (Windows)

**Status:** [ ]
**Assignee:**
**Estimated effort:** 4-6 hours

**What to do:**

Test the complete Photino app on Windows. This platform uses WebView2 (Chromium-based) as the webview.

1. **Build and run:**
   - `dotnet run --project Scrapile.Photino/Scrapile.Photino.csproj`
   - Verify WebView2 runtime is available (ships with Windows 10/11)

2. **Test all features:** (same checklist as Task 5.4, using Ctrl instead of Cmd)

3. **Platform-specific features:**
   - System tray icon
   - Global hotkey
   - Autorun (registry key)
   - High DPI display support

4. **Visual verification:**
   - Dark and light themes
   - Font rendering (may differ from macOS)
   - Window resize behavior

5. **Document any issues** in Comments section

**Acceptance Criteria:**
- [ ] App launches and all features work on Windows
- [ ] Ctrl-based shortcuts work correctly
- [ ] OS spell checking works in the editor
- [ ] WebView2 runtime detected correctly
- [ ] No visual glitches

**Comments:**
```
```

---

### Task 5.6: Cross-Platform Testing (Linux)

**Status:** [ ]
**Assignee:**
**Estimated effort:** 4-6 hours

**What to do:**

Test the complete Photino app on Linux. This platform uses WebKitGTK as the webview.

1. **Prerequisites:**
   - Ensure WebKitGTK is installed (`sudo apt install libwebkit2gtk-4.1-dev` on Ubuntu/Debian)

2. **Build and run:**
   - `dotnet run --project Scrapile.Photino/Scrapile.Photino.csproj`
   - Verify it launches without errors

3. **Test all features:** (same checklist as Task 5.4, using Ctrl)

4. **Platform-specific features:**
   - System tray icon (may vary by desktop environment)
   - Global hotkey (X11 only — Wayland not supported)
   - Autorun (desktop file in autostart)

5. **Document any issues** in Comments section

**Acceptance Criteria:**
- [ ] App launches and all features work on Linux
- [ ] Ctrl-based shortcuts work correctly
- [ ] Platform-specific features work or have documented limitations
- [ ] WebKitGTK dependency documented

**Comments:**
```
```

---

### Task 5.7: Update Publish Scripts

**Status:** [ ]
**Assignee:**
**Estimated effort:** 4-6 hours

**What to do:**

Update `publish.sh` and the csproj to produce distributable builds for the Photino app on all platforms.

1. **Update `Scrapile.Photino.csproj`:**
   - Configure publish settings (single-file, self-contained options)
   - Ensure `wwwroot/` files are included in published output
   - macOS bundling: Evaluate if `Dotnet.Bundle` works with Photino, or use manual `.app` bundle creation
   - Windows: Publish as single-file exe
   - Linux: Publish as single-file executable

2. **Create or update `publish.sh`:**
   - Build macOS arm64 (and optionally x64) — output to `pub/macos/`
   - Build Windows x64 — output to `pub/windows/`
   - Build Linux x64 — output to `pub/linux/`
   - Both self-contained and framework-dependent variants
   - Verify `wwwroot/` resources are embedded or copied correctly

3. **Test the published builds:**
   - Run the published macOS build
   - Run the published Windows build
   - Run the published Linux build
   - Verify all features work in the published version (not just `dotnet run`)

4. **Icon handling:**
   - Windows exe icon
   - macOS app bundle icon
   - Application icon in window title bar

**Look at these files for reference:**
- `publish.sh` - Current publish script
- `Scrapile.Desktop/Scrapile.Desktop.csproj` - Current publish configuration

**Acceptance Criteria:**
- [ ] Published builds work on all target platforms
- [ ] `wwwroot/` files included in published output
- [ ] macOS produces a `.app` bundle (or documented alternative)
- [ ] Windows produces a single-file `.exe`
- [ ] Icons work in published builds

**Comments:**
```
```

---

### Task 5.8: Remove Avalonia Project and Clean Up

**Status:** [ ]
**Assignee:**
**Estimated effort:** 3-4 hours

**What to do:**

Remove the old Avalonia project and clean up the repository. **Only do this after the Photino app has full feature parity and has been tested on all platforms.**

1. **Pre-removal checklist** (all must be true):
   - [ ] All Phase 1-5 tasks completed
   - [ ] Photino app tested on macOS, Windows, and Linux
   - [ ] No critical bugs remaining
   - [ ] All keyboard shortcuts work
   - [ ] All features from the Avalonia app are implemented

2. **Remove Avalonia project:**
   - Delete `Scrapile.Desktop/` directory
   - Remove `Scrapile.Desktop` from `Scrapile.slnx`
   - Remove any Avalonia-specific references from other projects (there should be none since Desktop depends on Application/Infrastructure, not the other way)

3. **Rename project (optional, discuss with team):**
   - Consider renaming `Scrapile.Photino` to `Scrapile.Desktop` for continuity
   - Update solution file references
   - Update `publish.sh`
   - Update `CLAUDE.md` build commands

4. **Update documentation:**
   - Update `CLAUDE.md`:
     - Change build and run commands to reference the new project
     - Update architecture diagram
     - Update key files table
     - Update publish commands
   - Update `docs/ProjectPlan.md` Technical Stack section
   - Update `ADR-001` status from "Proposed" to "Accepted" (or "Implemented")
   - Update version number (minor bump — this is a significant feature change)

5. **Final build verification:**
   - `dotnet build Scrapile.slnx` succeeds
   - `dotnet test Scrapile.slnx` — all existing tests still pass
   - `dotnet run --project <new-desktop-project>` works

**Acceptance Criteria:**
- [ ] Avalonia project fully removed
- [ ] Solution builds and tests pass
- [ ] Documentation updated
- [ ] Publish scripts work
- [ ] Version number bumped

**Comments:**
```
```

---

## Developer Notes

Use this section to record information that spans multiple tasks or is useful for anyone working on this migration.

### General Notes
```
```

### Architecture Decisions Made During Migration
```
```

### Known Issues and Workarounds
```
```

### Platform-Specific Notes

#### macOS
```
```

#### Windows
```
```

#### Linux
```
```

### Message Bridge API Reference

Document the final message types here as they are implemented:

| Message Type (JS→C#) | Payload | Response | Task |
|----------------------|---------|----------|------|
| `ping` | `{}` | `pong` with timestamp | 1.3 |
| `getOpenTabs` | `{}` | Tab list array | 1.4 |
| `getSettings` | `{}` | Settings object | 1.4 |
| `createTab` | `{}` | New tab info | 2.1 |
| `closeTab` | `{ tabId }` | Updated tab list | 2.1 |
| `selectTab` | `{ tabId }` | Tab content | 2.1 |
| `contentChanged` | `{ tabId, content }` | — | 2.2 |
| `updateTitle` | `{ tabId, title }` | Updated tab list | 2.3 |
| `reopenLastClosed` | `{}` | Updated tab list | 2.4 |
| `duplicateTab` | `{ tabId }` | Updated tab list | 2.4 |
| `copyToClipboard` | `{ tabId }` | Content string | 2.4 |
| `search` | `{ query }` | Search results | 3.1 |
| `openDocument` | `{ documentId }` | Updated tab list | 3.1 |
| `updateSetting` | `{ key, value }` | Updated settings | 3.3 |
| `browseDirectory` | `{}` | Selected path | 3.3 |
| `resetSettings` | `{}` | Default settings | 3.3 |
| `closeAllTabs` | `{}` | Updated tab list | 3.5 |
| `closeTabsAbove` | `{ tabId }` | Updated tab list | 3.5 |
| `closeTabsBelow` | `{ tabId }` | Updated tab list | 3.5 |
| `getRecentlyClosed` | `{}` | Recently closed list | 3.5 |
| `reopenDocument` | `{ documentId }` | Updated tab list | 3.5 |
| `showSaveDialog` | `{ content, filename }` | Success/failure | 4.4 |
| `showFolderDialog` | `{}` | Selected path | 4.4 |
| `welcomeComplete` | `{ storageDir, hotkey, autorun }` | — | 4.5 |

| Message Type (C#→JS) | Payload | Purpose | Task |
|----------------------|---------|---------|------|
| `pong` | `{ time }` | Ping response | 1.3 |
| `tabsUpdated` | Tab list array | Refresh tab list | 2.1 |
| `tabSaved` | `{ tabId }` | Clear dirty indicator | 2.1 |
| `saveStatusChanged` | `{ status }` | Update save indicator | 2.3 |
| `showWelcome` | `{}` | Show welcome flow | 4.5 |

---

**Document Version:** 1.0
**Created:** 2026-02-08
**Based on:** ADR-001-migrate-to-photino.md
