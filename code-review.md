# Code Review: Scrapile MVP

**Review Date:** 2026-01-29
**Reviewer:** Claude Code
**Version Reviewed:** 1.3.0

## Summary

The codebase follows good architectural patterns with clear layered separation (Domain → Application → Infrastructure → Desktop). The code is generally well-documented and uses appropriate patterns like MVVM, repository pattern, and dependency injection. However, there are several issues ranging from minor technical debt to potential bugs that should be addressed before considering this production-ready.

---

## Critical Issues

### 1. Potential Race Condition in TabManager

- [x] **Fix race condition in `CreateTabAsync`**

**File:** `Scrapile.Application/Services/TabManager.cs:77-106`

The `CreateTabAsync` method has a race condition between checking the count and adding to the list:

```csharp
int order;
lock (_lock)
{
    order = _tabs.Count;  // Gets count here
}

await _metadataStore.AddOpenTabAsync(document.Id, order);  // Async operation outside lock

var tab = new Tab { ... };

lock (_lock)
{
    _tabs.Add(tab);  // Another thread could have added a tab between locks
}
```

**Risk:** If two tabs are created concurrently, they could end up with the same order value.

**Recommendation:** Consider using `AsyncLock` (from Nito.AsyncEx or similar) or restructure to minimize the window between operations. One approach is to do all list mutations inside the lock and only perform async I/O outside.

---

### 2. Missing Disposal Pattern for JsonMetadataStore

- [x] **Implement `IDisposable` for `JsonMetadataStore` to dispose `SemaphoreSlim`**

**File:** `Scrapile.Infrastructure/Storage/JsonMetadataStore.cs`

The `SemaphoreSlim _lock` is never disposed:

```csharp
private readonly SemaphoreSlim _lock = new(1, 1);
// No IDisposable implementation
```

**Recommendation:** Implement `IDisposable` and dispose the semaphore in the disposal chain.

---

### 3. Fire-and-Forget Async in EditorViewModel

- [x] **Add error handling for fire-and-forget async call in `LoadTabContent`**

**File:** `Scrapile.Desktop/ViewModels/EditorViewModel.cs:279`

```csharp
// Load per-document word wrap setting asynchronously
_ = LoadDocumentWordWrapAsync(_currentTab.DocumentId);
```

**Risk:** Exceptions are silently swallowed, and there's no guarantee the load completes before the user interacts with the editor.

**Recommendation:** Add proper exception handling or await the task with error logging:
```csharp
_ = LoadDocumentWordWrapAsync(_currentTab.DocumentId).ContinueWith(t =>
{
    if (t.IsFaulted)
        Console.Error.WriteLine($"Failed to load word wrap setting: {t.Exception}");
}, TaskScheduler.Default);
```

---

## High Priority Issues

### 4. AutoSaveService Debounce Delay Not Configurable at Runtime

- [x] **Apply user-configured `AutoSaveDelayMs` setting to `AutoSaveService`**

**Files:** `Scrapile.Desktop/DependencyInjection/ServiceCollectionExtensions.cs`, `Scrapile.Application/Services/AutoSaveService.cs`

`AutoSaveService` is constructed with a hardcoded 500ms delay in DI, but `AppSettings.AutoSaveDelayMs` exists and can be changed by users:

```csharp
// ServiceCollectionExtensions.cs
services.AddSingleton<AutoSaveService>();  // Uses default 500ms

// AppSettings.cs
public int AutoSaveDelayMs { get; set; } = 500;  // User-configurable
```

**Issue:** The user-configured delay is never applied to the AutoSaveService.

**Recommendation:** Either:
1. Inject `ISettingsStore` into `AutoSaveService` constructor and read the delay
2. Make the debounce delay configurable via a property that can be updated when settings change
3. Use a factory pattern to create `AutoSaveService` with the correct delay

---

### 5. Async Void Event Handlers Without Exception Handling

- [x] **Add try-catch blocks to all `async void` event handlers in `MainWindowViewModel`**

**File:** `Scrapile.Desktop/ViewModels/MainWindowViewModel.cs`

Multiple `async void` event handlers without try-catch:

```csharp
private async void OnTabSelected(object? sender, TabItemViewModel? tabViewModel) { ... }
private async void OnRecentlyClosedChanged(object? sender, EventArgs e) { ... }
private async void OnEditorContentChanged(object? sender, ContentChangedEventArgs e) { ... }
private async void OnEditorTitleChanged(object? sender, TitleChangedEventArgs e) { ... }
private async void OnAutoSaveCompleted(object? sender, SaveCompletedEventArgs e) { ... }
```

**Risk:** Unhandled exceptions in these methods will crash the application.

**Recommendation:** Wrap all `async void` handlers in try-catch blocks:
```csharp
private async void OnTabSelected(object? sender, TabItemViewModel? tabViewModel)
{
    try
    {
        // existing code
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error in OnTabSelected: {ex}");
    }
}
```

---

### 6. Missing Validation/Warning for Storage Directory Change

- [ ] **Add migration logic or user warning when changing storage directory**

**File:** `Scrapile.Application/Services/SettingsService.cs:60-70`

When `StorageDirectory` is changed, there's no migration logic:

```csharp
public async Task SetStorageDirectoryAsync(string? directory)
{
    // Just saves the setting, doesn't migrate documents
    _currentSettings.StorageDirectory = normalized;
    await SaveAndNotifyAsync("StorageDirectory");
}
```

**Issue:** Changing storage directory requires app restart and leaves orphaned documents in the old location.

**Recommendation:** Either:
1. Disable runtime changes (require restart with clear messaging)
2. Implement document migration to new location
3. Show a confirmation dialog warning users about consequences

---

## Medium Priority Issues

### 7. AppSettings.Validate() Not Called After Loading

- [ ] **Call `AppSettings.Validate()` after deserializing settings in `JsonSettingsStore`**

**File:** `Scrapile.Infrastructure/Storage/JsonSettingsStore.cs`

The `AppSettings.Validate()` method exists but isn't called after deserializing settings. A user with a corrupted settings file could have invalid values.

**Recommendation:** Call `Validate()` after loading settings:
```csharp
var settings = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions);
settings?.Validate();
return settings ?? AppSettings.CreateDefault();
```

---

### 8. No Cancellation Token Support in Async Methods

- [ ] **Add `CancellationToken` parameters to async methods in services**

**Files:** `TabManager.cs`, `DocumentService.cs`, `AutoSaveService.cs`

Most async methods don't accept `CancellationToken`:

```csharp
public async Task<TabWithStats> CreateTabAsync()  // No CancellationToken
public async Task<DocumentWithStats?> GetByIdAsync(Guid id)  // No CancellationToken
```

**Issue:** Operations cannot be cancelled during app shutdown or when switching contexts quickly.

**Recommendation:** Add `CancellationToken` parameters to async methods:
```csharp
public async Task<TabWithStats> CreateTabAsync(CancellationToken cancellationToken = default)
```

---

### 9. Hardcoded String Constants Throughout Codebase

- [ ] **Extract setting names and valid values to a constants class or enums**

**Files:** Multiple

String constants are scattered throughout the code:

```csharp
// Different files have same strings
if (e.SettingName == "FontFamily" || e.SettingName == "FontSize" || e.SettingName == "All")
if (position != "Left" && position != "Right")
if (Theme != "Light" && Theme != "Dark" && Theme != "System")
```

**Recommendation:** Create a `SettingNames` class and enums:
```csharp
public static class SettingNames
{
    public const string FontFamily = "FontFamily";
    public const string FontSize = "FontSize";
    public const string All = "All";
    // etc.
}

public enum TabPosition { Left, Right }
public enum ThemeMode { Light, Dark, System }
```

---

### 10. Missing Test Coverage for Desktop Layer

- [ ] **Add unit tests for ViewModels (MainWindowViewModel, TabListViewModel, EditorViewModel)**

**Current Test Files:** Only Application and Infrastructure layers are tested

The Desktop layer (ViewModels, Services) has no unit tests. Given the complexity of `MainWindowViewModel` and `TabListViewModel`, this is a gap.

**Recommendation:** Add ViewModel tests, especially for:
- Tab selection and navigation logic
- Keyboard shortcut handling
- Event coordination between ViewModels
- Error handling in async operations

---

### 11. StorageLockService PID Reuse Vulnerability

- [ ] **Verify process name in `StorageLockService.IsProcessRunning()` to prevent PID reuse issues**

**File:** `Scrapile.Infrastructure/Services/StorageLockService.cs:176-198`

```csharp
private static bool IsProcessRunning(int pid)
{
    try
    {
        var process = Process.GetProcessById(pid);
        return !process.HasExited;
    }
    catch (ArgumentException) { return false; }
}
```

**Issue:** On Unix systems, PIDs can be reused quickly. A crashed Scrapile could leave a lock file, and if a different process gets the same PID, the lock check passes incorrectly.

**Recommendation:** Also verify the process name:
```csharp
private static bool IsProcessRunning(int pid)
{
    try
    {
        var process = Process.GetProcessById(pid);
        if (process.HasExited) return false;

        // Verify it's actually Scrapile
        var processName = process.ProcessName;
        return processName.Contains("Scrapile", StringComparison.OrdinalIgnoreCase) ||
               processName.Contains("dotnet", StringComparison.OrdinalIgnoreCase);
    }
    catch { return false; }
}
```

---

## Low Priority Issues

### 12. Duplicate Code in Tab Management

- [ ] **Extract common tab-finding logic in `TabListViewModel` into helper methods**

**File:** `Scrapile.Desktop/ViewModels/TabListViewModel.cs`

Similar patterns for finding and removing tabs appear multiple times:
```csharp
var tabInCollection = Tabs.FirstOrDefault(t => t.TabId == tabViewModel.TabId);
if (tabInCollection == null) return;
var index = Tabs.IndexOf(tabInCollection);
```

**Recommendation:** Extract to helper method:
```csharp
private (TabItemViewModel? tab, int index) FindTab(Guid tabId)
{
    var tab = Tabs.FirstOrDefault(t => t.TabId == tabId);
    return (tab, tab != null ? Tabs.IndexOf(tab) : -1);
}
```

---

### 13. Magic Numbers Without Context

- [ ] **Extract magic numbers to named constants with explanatory comments**

**Files:** Multiple

```csharp
const int MaxRecentlyClosedItems = 50;  // Good example, but others are inline:
await Task.Delay(1500);  // What does 1500ms represent?
if (fontSize < 8 || fontSize > 72)  // Why 8 and 72?
```

**Recommendation:**
```csharp
private const int SaveStatusDisplayDurationMs = 1500;  // How long "Saved" status shows
private const int MinFontSize = 8;   // Minimum readable font size
private const int MaxFontSize = 72;  // Maximum practical font size
```

---

### 14. Mutable Domain Entity

- [ ] **Consider making `Document` entity immutable or use private setters**

**File:** `Scrapile.Domain/Entities/Document.cs`

`Document` is a domain entity but has mutable public setters:

```csharp
public string? Title { get; set; }
public string Content { get; set; } = string.Empty;
```

**Issue:** Domain entities should ideally be immutable or have controlled mutation to prevent accidental state changes.

**Recommendation:** Consider using init-only setters or a record:
```csharp
public record Document
{
    public required Guid Id { get; init; }
    public required string Filename { get; init; }
    public string? Title { get; init; }
    // etc.
}
```

Or use private setters with explicit update methods.

---

### 15. Inconsistent Null/Empty String Handling

- [ ] **Standardize null/empty string checks throughout codebase**

**Files:** Multiple

Some places use `string.IsNullOrWhiteSpace()`, others use `string.IsNullOrEmpty()`, and some use direct null checks.

**Recommendation:** Establish a convention:
- Use `string.IsNullOrWhiteSpace()` for user input validation
- Use `string.IsNullOrEmpty()` for internal string checks
- Document the convention in CLAUDE.md

---

## Future Improvements (Not Bugs)

These are suggestions for future development, not issues requiring fixes:

- [ ] **Add structured logging infrastructure** (e.g., Serilog) for better debugging
- [ ] **Add error reporting/telemetry** for understanding failures in production
- [ ] **Refactor `App.axaml.cs`** - initialization is complex and hard to test
- [ ] **Add metadata/settings migration strategy** for handling format version changes
- [ ] **Consider adding undo/redo support** for document editing

---

## Architecture Observations

### Strengths
- **Clean layered architecture** - Dependencies flow correctly (Domain ← Application ← Infrastructure ← Desktop)
- **Well-documented interfaces** - `IDocumentRepository`, `IMetadataStore`, `ISettingsStore` are well-defined
- **Thread safety awareness** - Locks are used appropriately in most places
- **Atomic file writes** - Prevents corruption during writes
- **Single-instance lock** - Prevents data conflicts
- **Excellent test coverage** for TabManager (70+ tests)

### Security Considerations
- Storage directory paths should be validated to prevent traversal attacks
- File names use GUIDs which prevents injection (good)
- Settings don't contain credentials (good)

---

## Task Summary

### Must Fix (Critical)
- [x] Fix race condition in `CreateTabAsync` (#1)
- [x] Implement `IDisposable` for `JsonMetadataStore` (#2)
- [x] Add error handling for fire-and-forget async in `EditorViewModel` (#3)

### Should Fix (High Priority)
- [x] Apply `AutoSaveDelayMs` setting to `AutoSaveService` (#4)
- [x] Add try-catch to all `async void` event handlers (#5)
- [ ] Add warning/migration for storage directory changes (#6)

### Recommended (Medium Priority)
- [ ] Call `AppSettings.Validate()` after loading (#7)
- [ ] Add cancellation token support to async methods (#8)
- [ ] Extract string constants to dedicated class (#9)
- [ ] Add ViewModel test coverage (#10)
- [ ] Verify process name in lock service (#11)

### Nice to Have (Low Priority)
- [ ] Extract common tab-finding logic (#12)
- [ ] Extract magic numbers to named constants (#13)
- [ ] Make domain entities immutable (#14)
- [ ] Standardize null/empty string handling (#15)

---

## Checklist for Completion

When all critical and high-priority issues are resolved, the reviewer should:

- [ ] Re-run all existing tests to ensure no regressions
- [ ] Manually test tab creation under concurrent conditions
- [ ] Verify app shutdown disposes resources correctly
- [ ] Test settings changes and ensure they apply correctly
- [ ] Review any new code for the same patterns identified in this review
