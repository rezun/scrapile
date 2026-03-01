# Versioning

Version is defined in `Scrapile.Desktop/Scrapile.Desktop.csproj`. When updating, change all three properties together:
- `<Version>` - Assembly version
- `<CFBundleVersion>` - macOS bundle version
- `<CFBundleShortVersionString>` - macOS display version

Version scheme: **major.minor.patch**
- **Minor** (second place): new or changed features
- **Patch** (third place): bug fixes

# Architecture

Scrapile is a cross-platform scratchpad/note-taking desktop application with a clean layered architecture:

```
Desktop (Avalonia UI + MVVM)
    ↓
Application (Services & DTOs)
    ↓
Domain (Entities & Interfaces)
    ↓
Infrastructure (File System & JSON Storage)
```

**Projects:**
- **Scrapile.Domain** - Core entities (Document, Tab, Metadata, AppSettings) and interfaces (IDocumentRepository, IMetadataStore, ISettingsStore)
- **Scrapile.Application** - Services (DocumentService, TabManager, AutoSaveService, SettingsService), DTOs, and helpers
- **Scrapile.Infrastructure** - FileSystemDocumentRepository, JsonMetadataStore, JsonSettingsStore implementations
- **Scrapile.Desktop** - Avalonia UI with ViewModels and Views

**Core Design Concept:** "Frictionless capture with optional promotion" - users create tabs and jot notes without save dialogs; content auto-saves silently; titles are optional for discoverability.

# Platform Considerations

- Targets Windows, macOS, and Linux
- Keyboard shortcuts are platform-aware: code checks `OperatingSystem.IsMacOS()` for Cmd vs Ctrl
- Settings paths handled by JsonSettingsStore for each platform
