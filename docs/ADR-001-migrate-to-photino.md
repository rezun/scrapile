# ADR-001: Migrate UI from Avalonia to Photino.NET with HTML/CSS/JS Frontend

**Status:** Proposed
**Date:** 2026-02-05
**Decision Makers:** Steve

---

## Context

Scrapile is a cross-platform scratchpad/note-taking desktop application currently built with Avalonia UI. The application follows a clean layered architecture (Domain, Application, Infrastructure, Desktop) with MVVM pattern using CommunityToolkit.Mvvm. The Desktop layer consists of 9 AXAML views, 13 ViewModels, and ~7,700 lines of C# code including AvaloniaEdit integration with TextMate syntax highlighting for 17 languages.

The current Avalonia-based UI has been functional but has reached limitations that are impacting development velocity and blocking desired features.

## Motivation

### 1. OS-Level Spell Checking is Blocked

The application is a text editor at its core, and spell checking is a fundamental feature users expect. macOS provides excellent system-level spell checking, but Avalonia renders its own text controls using Skia/a drawing engine rather than native OS controls. This means macOS spell checking cannot integrate with Avalonia's TextBox or AvaloniaEdit. There is no viable workaround — this is an architectural limitation of drawn-UI frameworks.

A system webview (WebKit on macOS, WebView2 on Windows) uses native text input controls where OS spell checking works automatically, out of the box.

### 2. Avalonia's Small Ecosystem Limits Development Velocity

Avalonia has a small developer community relative to web technologies. This manifests as:

- **Limited learning resources** — documentation gaps, fewer tutorials, fewer Stack Overflow answers
- **Hard to find experienced developers** — the talent pool for Avalonia is narrow
- **Fewer third-party components** — when a needed control or behavior doesn't exist, you build it yourself
- **Slower problem resolution** — niche issues take longer to diagnose and fix

Web technologies (HTML, CSS, JavaScript) have the largest developer ecosystem in existence. Every problem has been solved, documented, and answered multiple times.

### 3. Future Web Application

The project specification (ProjectPlan.md) already identifies a "future web version" as a design consideration. The current architecture with its clean layer separation was built with this in mind.

However, with an Avalonia frontend, creating a web version means writing an entirely separate UI from scratch. If the frontend is already HTML/CSS/JS, the same UI code runs in a browser with minimal adaptation — only the communication layer changes (Photino IPC messages become HTTP/WebSocket calls to a server).

### 4. Blazor Was Evaluated and Rejected

Blazor Server was used in production in-house projects and found to have significant drawbacks: fragile SignalR connections and difficult state management. Blazor WebAssembly ships a .NET runtime into the browser, which is architecturally heavy for rendering what should be simple HTML.

A vanilla HTML/CSS/JS frontend is simpler, lighter, and uses the web platform as intended.

### 5. MAUI Was Evaluated and Rejected

.NET MAUI lacks Linux support. The recently announced "MAUI on Linux and Browser via Avalonia" initiative uses Avalonia as a rendering backend — meaning it inherits all of Avalonia's limitations (no OS spell checking, drawn UI) while adding the complexity of MAUI's API layer. The browser version renders to a canvas via WASM rather than producing real HTML, breaking web conventions (no Ctrl+F, no text selection, no screen reader support). It is also pre-release with no production timeline.

### 6. Electron Was Evaluated and Rejected

Electron is the most mature web-wrapper for desktop apps. However, Electron's backend is Node.js/JavaScript. Using Electron would require either:

- Rewriting all backend layers (Domain, Application, Infrastructure) in TypeScript — a full rewrite
- Using Electron.NET, a niche project with inconsistent maintenance that stacks two runtimes

Neither option preserves the existing C# investment.

## Decision

Migrate the Desktop UI layer from Avalonia to **Photino.NET** with a **vanilla HTML/CSS/JS** frontend, while preserving the existing C# backend layers and ViewModels.

### What is Photino.NET

Photino.NET is a .NET wrapper around the OS-native webview:

- **macOS:** WebKit (the Safari engine)
- **Windows:** WebView2 (Chromium-based, ships with Windows 10/11)
- **Linux:** WebKitGTK

It does **not** bundle a browser. The resulting application uses the system-installed webview, keeping binary sizes small. Photino provides a lightweight message-passing bridge between JavaScript in the webview and C# in the host process.

## Architecture

### Current Architecture (Avalonia)

```
Scrapile.Desktop (Avalonia UI)
  ├── Views (9 AXAML files)
  ├── ViewModels (13 C# classes)
  ├── Behaviors, Converters
  └── Platform Services (tray, hotkeys, autorun)
       ↓
Scrapile.Application (Services & DTOs)
       ↓
Scrapile.Domain (Entities & Interfaces)
       ↓
Scrapile.Infrastructure (File System & JSON Storage)
```

### Target Architecture (Photino)

```
Scrapile.Desktop (Photino.NET host)
  ├── wwwroot/
  │   ├── index.html
  │   ├── css/ (styles)
  │   └── js/ (vanilla JS, CodeMirror editor)
  ├── ViewModels (13 C# classes, adapted)
  ├── Bridge/ (JSON message passing between JS ↔ C#)
  └── Platform Services (tray, hotkeys, autorun)
       ↓
Scrapile.Application (Services & DTOs)  ← unchanged
       ↓
Scrapile.Domain (Entities & Interfaces)  ← unchanged
       ↓
Scrapile.Infrastructure (File System & JSON Storage)  ← unchanged
```

### Communication Pattern

The JS frontend and C# backend communicate via Photino's message-passing API:

```
JS → C#:  window.external.sendMessage(JSON.stringify({ action, payload }))
C# → JS:  window.SendMessage(jsonString)
```

ViewModels serialize their state as JSON and push updates to the frontend. The frontend sends user actions (create tab, update content, search) as JSON messages to C#. A `MessageRouter` in C# dispatches messages to the appropriate ViewModel or service.

### Code Editor

Replace AvaloniaEdit + TextMate with **CodeMirror 6**:

- Lightweight, modular architecture
- Built-in syntax highlighting for all 17 currently supported languages
- Built-in search/replace
- Excellent keyboard handling and accessibility
- Active development and large community
- OS spell checking works in the webview's contenteditable elements

## What Changes

| Component | Action | Notes |
|-----------|--------|-------|
| Scrapile.Domain | **No change** | Entities, interfaces unchanged |
| Scrapile.Infrastructure | **No change** | Repositories, stores unchanged |
| Scrapile.Application | **No change** | Services, DTOs, helpers unchanged |
| ViewModels | **Adapt** | Core logic preserved; remove AXAML-specific patterns, add JSON serialization for state pushing |
| AXAML Views (9 files) | **Replace** | Rewritten as HTML/CSS/JS |
| Code-behind (3 large files) | **Replace** | Keyboard handling moves to JS; dialog orchestration moves to HTML modals and C# bridge |
| AvaloniaEdit + TextMate | **Replace** | CodeMirror 6 via JS |
| Converters, Behaviors | **Remove** | CSS and JS handle these concerns natively |
| DI Registration | **Adapt** | Update for Photino hosting |
| App Startup | **Rewrite** | PhotinoWindow setup instead of Avalonia Application |
| Global Hotkeys (SharpHook) | **Keep** | SharpHook is not Avalonia-dependent |
| System Tray | **Evaluate** | May need platform-specific implementation |
| Autorun Service | **Keep** | Platform-specific, not Avalonia-dependent |
| Publish/Bundling | **Rewrite** | New publish pipeline; no more Dotnet.Bundle for macOS |

## What We Gain

- **OS spell checking** on all platforms, automatically
- **CSS styling** — dramatically easier theming and visual refinement
- **CodeMirror** — richer editor ecosystem than AvaloniaEdit
- **Web-ready frontend** — the same HTML/CSS/JS runs in a browser for a future web version
- **Larger talent pool** — HTML/CSS/JS knowledge is ubiquitous
- **Abundant resources** — every UI problem has existing solutions and documentation
- **Smaller binary** — no bundled browser; system webview adds zero to binary size
- **Faster UI iteration** — browser DevTools (inspect, debug, hot-reload CSS) work in the webview

## Risks and Mitigations

| Risk | Severity | Mitigation |
|------|----------|------------|
| **Photino.NET is less mature than Avalonia** | Medium | Photino's scope is small (host a webview, pass messages). Less surface area means less that can go wrong. The complex UI logic lives in standard web tech, not Photino. |
| **System tray support is limited in Photino** | Medium | SharpHook already handles global hotkeys independently. Tray icon may require platform-specific code or a companion library. Evaluate during implementation. |
| **Webview rendering differences across platforms** | Low | WebKit (macOS) and WebView2 (Windows) are both modern engines. For a UI of this complexity, cross-browser differences are minimal. Test on all platforms during migration. |
| **JS interop overhead for editor** | Low | CodeMirror runs entirely in JS. The only IPC is content changes (debounced, same as current auto-save) and commands. Not performance-sensitive. |
| **Learning vanilla JS** | Low | The JS required is thin UI wiring (~1,000-2,000 lines), not complex application logic. C# syntax familiarity transfers well. |

## Migration Plan

### Phase 1: Foundation

1. Create a new Photino.NET desktop project alongside the existing Avalonia project
2. Set up the HTML/CSS/JS scaffold (index.html, basic styles, JS entry point)
3. Implement the C# ↔ JS message bridge (MessageRouter, JSON serialization)
4. Wire up DI and existing services to the Photino host

### Phase 2: Core UI

5. Build the tab list in HTML/CSS/JS, connected to TabListViewModel via the bridge
6. Integrate CodeMirror 6 as the editor, connected to EditorViewModel
7. Implement the title bar, status bar, and save status display
8. Add keyboard shortcut handling in JS (Cmd/Ctrl detection, tab navigation)

### Phase 3: Features

9. Implement the search overlay (global document search)
10. Implement the in-document find bar (CodeMirror's built-in search)
11. Build the settings window as an HTML modal or separate page
12. Implement message/confirmation dialogs in HTML

### Phase 4: Platform Integration

13. System tray icon — evaluate Photino support, implement platform-specific fallback if needed
14. Global hotkeys — verify SharpHook works with Photino host
15. Autorun at startup — verify existing platform-specific code works unchanged
16. File picker / save-as dialogs via Photino or native interop

### Phase 5: Polish and Cutover

17. Theme support (light/dark) via CSS custom properties
18. Cross-platform testing (macOS, Windows, Linux)
19. Update publish scripts for Photino distribution
20. Remove the Avalonia Desktop project

### Validation Approach

Each phase should produce a working (if incomplete) application. Phase 2 completion is the key milestone — if the tab list, editor, and auto-save work correctly through the Photino bridge, the approach is validated. If fundamental issues emerge in Phase 1 or 2, the Avalonia project remains intact as a fallback.

## References

- [Photino.NET](https://www.tryphotino.io/)
- [CodeMirror 6](https://codemirror.net/)
- [Scrapile Project Specification](./ProjectPlan.md)
