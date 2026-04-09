# Avalonia 12 Upgrade Notes

Reference for the 11.3.11 → 12.0.0 migration. Captures what changed, why, and
what future maintainers should know. Written immediately after the upgrade, so
anything marked "at time of upgrade" may have moved on — verify before acting.

## Package changes

| Package | Before | After | Notes |
| --- | --- | --- | --- |
| `Avalonia` | 11.3.11 | 12.0.0 | |
| `Avalonia.Desktop` | 11.3.11 | 12.0.0 | |
| `Avalonia.Themes.Fluent` | 11.3.11 | 12.0.0 | |
| `Avalonia.Fonts.Inter` | 11.3.11 | 12.0.0 | |
| `Avalonia.AvaloniaEdit` | 11.3.0 | 12.0.0 | |
| `AvaloniaEdit.TextMate` | 11.3.0 | 12.0.0 | Transitively requires `TextMateSharp.Grammars >= 2.0.3`. |
| `TextMateSharp.Grammars` | 1.0.69 | 2.0.3 | Bumped to satisfy AvaloniaEdit.TextMate 12's floor. |
| `Avalonia.Xaml.Behaviors` | 11.2.0 | **removed** | No v12 release at time of upgrade. Its single use site was inlined (see below). |
| `Avalonia.Diagnostics` | 11.3.11 | **removed** | No v12 release at time of upgrade. Re-add when published (see "Deferred" below). |

## Breaking changes encountered

Reference: <https://docs.avaloniaui.net/docs/avalonia12-breaking-changes>

### 1. `Avalonia.Xaml.Behaviors` has no v12 release

The only consumer in the project was `Scrapile.Desktop/Behaviors/DocumentTextBindingBehavior.cs`,
a `Behavior<TextEditor>` that two-way-synced `EditorViewModel.Content` with
AvaloniaEdit's `TextEditor.Text` (which does not support direct binding).

**Resolution:** dropped the package entirely and inlined the sync as direct
event subscriptions in `EditorView.axaml.cs`:

- Subscribe to `ContentEditor.TextChanged` in `OnLoaded`, unsubscribe in `OnUnloaded`.
- Subscribe to `EditorViewModel.PropertyChanged` in `SubscribeToViewModel`,
  filter for `nameof(EditorViewModel.Content)`, unsubscribe on data context change
  and in `OnUnloaded`.
- Single `_isSyncingContent` re-entrancy guard to prevent the echo loop.
- Caret preservation on vm → editor pushes (`Math.Min(CaretOffset, newText.Length)`).
- Initial vm → editor sync performed inside `SubscribeToViewModel` under the guard.

The `Scrapile.Desktop/Behaviors/` folder and its single file were deleted.
XML namespaces `xmlns:i="using:Avalonia.Xaml.Interactivity"` and
`xmlns:behaviors="using:Scrapile.Desktop.Behaviors"` were removed from
`EditorView.axaml`.

### 2. `BindingPlugins` and the data annotations workaround

`Avalonia.Data.Core.Plugins.BindingPlugins` and
`DataAnnotationsValidationPlugin` were deleted in v12. The project had a
`DisableAvaloniaDataAnnotationValidation` method in `App.axaml.cs` that walked
`BindingPlugins.DataValidators` to remove the annotations plugin — a
workaround for the well-known `CommunityToolkit.Mvvm` double-validation issue.

**Resolution:** deleted the method and its call site. v12 disables the data
annotations plugin by default precisely because of the CommunityToolkit
conflict, so the workaround is now obsolete. Also dropped the
`using Avalonia.Data.Core;`, `using Avalonia.Data.Core.Plugins;`, and
`using System.Linq;` (the last was only used by this method).

### 3. `GotFocusEventArgs` renamed to `FocusChangedEventArgs`

`InputElement.GotFocus` and `InputElement.LostFocus` now fire with
`FocusChangedEventArgs` (which carries info about the previous and current
focused elements) instead of `GotFocusEventArgs`.

**Resolution:** `EditorView.axaml.cs` — `OnTitleGotFocus(object?, GotFocusEventArgs)`
→ `OnTitleGotFocus(object?, FocusChangedEventArgs)`. Body unchanged.

### 4. Clipboard API rewritten around `IAsyncDataTransfer`

`IDataObject` / `DataObject` were replaced with `IAsyncDataTransfer` /
`DataTransfer`. For the simple text case, convenience extension methods live in
the namespace `Avalonia.Input.Platform.ClipboardExtensions` (`SetTextAsync`,
`TryGetTextAsync`, etc.). These are extension methods, so source-level
`clipboard.SetTextAsync(content)` still works **once the namespace is imported**.

**Resolution:** `MainWindow.axaml.cs` — added `using Avalonia.Input.Platform;`.
The call site at `OnClipboardCopyRequested` (~line 409) is unchanged. Only
text is copied, so the verbose `DataTransfer`/`DataTransferItem` form wasn't
needed.

If richer clipboard support is added later, the non-extension form is:

```csharp
var item = new DataTransferItem();
item.Set(DataFormat.Text, "some text");
var data = new DataTransfer();
data.Add(item);
await clipboard.SetDataAsync(data);
```

### 5. `DragDrop.DoDragDropAsync` now requires `PointerPressedEventArgs`

Previously accepted the base `PointerEventArgs`, so it was fine to start a drag
from a `PointerMoved` handler. v12 tightened the signature to
`DoDragDropAsync(PointerPressedEventArgs, IDataTransfer, DragDropEffects)`.

This clashed with the existing drag-threshold pattern in `TabListView.axaml.cs`:
the drag is primed in `OnTabPointerPressed` but only *begins* in
`OnTabPointerMoved` once the pointer has moved `DragThreshold` pixels away from
the press point. The `PointerMoved` handler only has a `PointerEventArgs`.

**Resolution:** captured the original `PointerPressedEventArgs` into a new
`_pendingDragPressArgs` field when priming the drag in `OnTabPointerPressed`,
and pass it through to `DoDragDropAsync` in `OnTabPointerMoved`. The field is
cleared in every reset path (pointer released before threshold, button released,
drag start, `OnTabPointerReleased`).

**⚠ Watch for:** this assumes `PointerPressedEventArgs` can be safely retained
across event dispatches. Avalonia's routed event args are plain managed
objects, so this appears to work, but if drags ever misbehave (wrong device,
wrong button state, inconsistent coordinates) the cause may be stale event args.
Runtime-verify drag-and-drop tab reordering after this change.

### 6. `TextBox.Watermark` deprecated in favor of `PlaceholderText`

Simple rename, no semantic change. 8 sites updated across `EditorView.axaml`,
`FindBarView.axaml`, `SearchOverlay.axaml`, `SettingsWindow.axaml`,
`WelcomeWindow.axaml`.

## Non-issues (verified dodged)

- `.NET 8+` target requirement — project was already on `net9.0`.
- Compiled bindings on by default — csproj already had
  `<AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>`,
  so v12's default-flip is a no-op.
- Binding hierarchy restructure — no `new Binding(...)` in C#; all bindings are
  XAML markup extensions, which are unaffected.
- Direct2D1 renderer removal — project uses Skia (Avalonia default), no opt-in
  to Direct2D anywhere.
- Android/iOS bootstrap changes — desktop-only project.
- `IDataObject` removal — only clipboard usage was `SetTextAsync`; covered
  above.
- `Avalonia.Browser.Blazor` / Tizen removals — not referenced.
- `ViewLocator.cs` — touches no binding internals, pure `Type.GetType` lookup.

## Deferred / to revisit

### `Avalonia.Diagnostics` (F12 DevTools inspector)

No v12 release at time of upgrade (latest was `11.3.13`). The package was
already conditioned out of Release builds via
`<IncludeAssets Condition="'$(Configuration)' != 'Debug'">None</IncludeAssets>`,
so its absence only affects the dev experience, not shipped binaries.

**To re-add when published:** restore the original reference block, no other
code changes needed (it self-attaches via `AttachDevTools` on windows).

```xml
<PackageReference Include="Avalonia.Diagnostics" Version="12.0.0">
  <IncludeAssets Condition="'$(Configuration)' != 'Debug'">None</IncludeAssets>
  <PrivateAssets Condition="'$(Configuration)' != 'Debug'">All</PrivateAssets>
</PackageReference>
```

Monitor <https://www.nuget.org/packages/Avalonia.Diagnostics>.

### `Avalonia.Xaml.Behaviors`

Not planned for re-add. We only used it for one tiny behavior which is now
inlined. If a future need appears for event-to-command wiring or triggers, the
package would need to be re-added *and* have a v12 release.

### Avalonia Accelerate telemetry (new in v12)

Every build now prints:

> `Avalonia Accelerate Community requires telemetry. To opt out, please upgrade to a paid tier.`
>
> <https://avaloniaui.net/accelerate/>

This is a new commercial/licensing model Avalonia introduced with v12. The
telemetry ships in built binaries unless opted out via a paid tier. This is a
policy decision, not a code one — review Avalonia's licensing page and decide
whether the telemetry is acceptable for Scrapile, and if not, evaluate the paid
tier or alternatives. Not resolved as part of the upgrade.

## Manual verification checklist

Build + tests pass, but the following were not exercised by automated tests
and should be spot-checked on each platform before shipping:

- [X] **Editor text editing** — type, undo/redo, switch tabs. Confirms the
      inlined `DocumentTextBindingBehavior` replacement round-trips content
      through the view model and auto-save still fires.
- [X] **Drag-and-drop tab reordering** — the `PointerPressedEventArgs`
      retention pattern is new; confirm drags initiate past the threshold,
      complete correctly, and also cancel cleanly if the button is released
      before the threshold.
- [X] **Clipboard copy** — copy tab content via the context menu and via
      `CopyCurrentTabToClipboard`. Confirms `ClipboardExtensions.SetTextAsync`
      behaves identically to the old `IClipboard.SetTextAsync`.
- [X] **Title field focus** — click into the title TextBox, press Escape to
      cancel. Confirms the `FocusChangedEventArgs` handler still captures the
      pre-edit value.
