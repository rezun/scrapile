# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Versioning

Version is defined in `Scrapile.Desktop/Scrapile.Desktop.csproj`. When updating, change all three properties together:
- `<Version>` - Assembly version
- `<CFBundleVersion>` - macOS bundle version
- `<CFBundleShortVersionString>` - macOS display version

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

# Publish macOS and Windows (self-contained + slim variants)
./publish.sh

# Publish macOS only
dotnet msbuild Scrapile.Desktop/Scrapile.Desktop.csproj -t:BundleApp -p:RuntimeIdentifier=osx-arm64 -p:Configuration=Release -p:UseAppHost=true -p:SelfContained=true

# Publish Windows only
dotnet publish Scrapile.Desktop/Scrapile.Desktop.csproj -r win-x64 -c Release -p:SelfContained=true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true

# Publish Linux only
dotnet publish Scrapile.Desktop/Scrapile.Desktop.csproj -r linux-x64 -c Release -p:SelfContained=true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
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

## Publishing & Icons

**Icons** (in `Scrapile.Desktop/Assets/`):
- `app-icon.png` - Window icon (512x512, used by Avalonia at runtime)
- `Scrapile.ico` - Windows executable icon (multi-size: 16-256px, embedded via `<ApplicationIcon>`)
- `Scrapile.icns` - macOS app bundle icon (multi-size, referenced by `CFBundleIconFile`)

**macOS Bundling** uses `Dotnet.Bundle` NuGet package:
- Bundle properties configured in `Scrapile.Desktop.csproj` (`CFBundleName`, `CFBundleIdentifier`, etc.)
- Creates `.app` bundle with proper `Info.plist` and icon in `Contents/Resources/`
- Note: App is not code-signed; users may need to right-click → Open on first launch

**Publish Script** (`publish.sh`):
- Builds macOS (arm64) and Windows (x64) with both self-contained and framework-dependent variants
- Linux builds are supported but currently commented out
- Output structure:
  - `pub/macos/` and `pub/macos-slim/` - macOS `.app` bundles
  - `pub/windows/` and `pub/windows-slim/` - Windows executables
  - `pub/linux/` and `pub/linux-slim/` - Linux executables (when enabled)
- Self-contained: ~90-110MB (includes .NET runtime)
- Framework-dependent (slim): ~30MB (requires .NET 9.0 installed)

## Key Files

| File | Purpose |
|------|---------|
| `docs/ProjectPlan.md` | Master specification - full requirements and design rationale |
| `docs/ImplementationPlan.md` | Task-based roadmap with progress tracking |
| `publish.sh` | Cross-platform publish script (macOS, Windows, Linux) |
| `Scrapile.Application/Services/TabManager.cs` | Tab lifecycle management |
| `Scrapile.Application/Services/AutoSaveService.cs` | Debounced save logic |
| `Scrapile.Desktop/ViewModels/MainWindowViewModel.cs` | Main coordinator ViewModel |

## Testing

- xUnit with Fact/Theory attributes
- Test classes follow `[SubjectUnderTest]Tests` naming
- Application tests target net10.0, other projects use net9.0
