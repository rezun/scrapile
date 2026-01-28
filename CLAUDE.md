# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build Commands

```bash
# Build solution
dotnet build Scrapile.slnx

# Run tests
dotnet test Scrapile.slnx

# Run specific test project
dotnet test Scrapile.Application.Tests/Scrapile.Application.Tests.csproj

# Run desktop app
dotnet run --project Scrapile.Desktop/Scrapile.Desktop.csproj
```

## Architecture

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

## Key Patterns

- **Dependency Injection** - Services registered in `Scrapile.Desktop/DependencyInjection/ServiceCollectionExtensions.cs`
- **MVVM** - ViewModels use CommunityToolkit.Mvvm with `[ObservableProperty]` attributes
- **Repository Pattern** - `IDocumentRepository` abstracts file storage
- **Auto-Save** - 500ms debounce after last keystroke, atomic writes (temp file + rename)
- **Thread Safety** - Locks for tab list access, SemaphoreSlim for metadata file access

## Data Storage

- **Documents:** Plain text UTF-8 files named `{timestamp}_{guid}.txt` in user-configurable directory
- **Metadata:** `.ephemeral_metadata.json` stores open tabs, recently closed items, document titles
- **Settings:** `settings.json` in platform-specific config directory

## Platform Considerations

- Targets Windows, macOS, and Linux
- Keyboard shortcuts are platform-aware: code checks `OperatingSystem.IsMacOS()` for Cmd vs Ctrl
- Settings paths handled by JsonSettingsStore for each platform

## Key Files

| File | Purpose |
|------|---------|
| `ProjectPlan.md` | Master specification - full requirements and design rationale |
| `ImplementationPlan.md` | Task-based roadmap with progress tracking |
| `Scrapile.Application/Services/TabManager.cs` | Tab lifecycle management |
| `Scrapile.Application/Services/AutoSaveService.cs` | Debounced save logic |
| `Scrapile.Desktop/ViewModels/MainWindowViewModel.cs` | Main coordinator ViewModel |

## Testing

- xUnit with Fact/Theory attributes
- Test classes follow `[SubjectUnderTest]Tests` naming
- Application tests target net10.0, other projects use net9.0
