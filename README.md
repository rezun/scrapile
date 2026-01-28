# Scrapile

A cross-platform scratchpad for quick, disposable note-taking. Write now, organize later (or never).

## Philosophy

Most note-taking apps force you to create titles, choose folders, and organize before you even start writing. Scrapile flips this around:

**Default Mode - Zero Friction:**
- Open a tab, paste or type, close when done
- Everything auto-saves silently in the background
- No titles required, no folders, no decisions
- Perfect for clipboard dumps, quick thoughts, meeting notes, code snippets

**Optional Promotion - When Content Matters:**
- Realize something is valuable? Add a title to make it findable
- Titled documents stand out in search results
- Most notes stay untitled and that's fine

Think of it like Gmail: most emails get archived without labels, but adding context to important ones makes them instantly findable.

## Features

- **Multi-tab interface** with vertical tab layout
- **Auto-save** - 500ms after you stop typing, no save button needed
- **Session restore** - Close the app, reopen later, everything's still there
- **Document search** (Ctrl/Cmd+P) - Find any document by title or content
- **Recently closed** (Ctrl/Cmd+Shift+T) - Reopen tabs you closed
- **Quick stats** - Word count shown on each tab
- **Duplicate, export, copy** - All the standard operations
- **Dark mode** - Easy on the eyes

## Keyboard Shortcuts

| Action | Windows/Linux | macOS |
|--------|---------------|-------|
| New Tab | Ctrl+T | Cmd+T |
| Close Tab | Ctrl+W | Cmd+W |
| Reopen Closed Tab | Ctrl+Shift+T | Cmd+Shift+T |
| Search | Ctrl+P | Cmd+P |
| Edit Title | F2 | F2 |
| Duplicate Tab | Ctrl+Shift+D | Cmd+Shift+D |
| Copy to Clipboard | Ctrl+Shift+C | Cmd+Shift+C |
| Save As | Ctrl+Shift+S | Cmd+Shift+S |
| Next Tab | Ctrl+Tab | Cmd+Tab |
| Previous Tab | Ctrl+Shift+Tab | Cmd+Shift+Tab |
| Settings | Ctrl+, | Cmd+, |

## Installation

### Run from Source

Requires [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0).

```bash
git clone https://github.com/yourusername/scrapile.git
cd scrapile
dotnet run --project Scrapile.Desktop/Scrapile.Desktop.csproj
```

### Build

```bash
dotnet build Scrapile.slnx
```

### Publish

Build self-contained executables for all platforms:

```bash
./publish.sh
```

Output in `pub/`:
- `pub/macos/Scrapile.app` - macOS application bundle (Apple Silicon)
- `pub/windows/Scrapile.Desktop.exe` - Windows single-file executable
- `pub/linux/Scrapile.Desktop` - Linux single-file executable

All builds are self-contained (no .NET runtime required on target machine).

## Data Storage

Documents are stored as plain text files in a configurable directory (default: `~/Documents/Scrapile/`).

- Each document is a `.txt` file
- Session state stored in `.ephemeral_metadata.json`
- Settings stored in your system's config directory

You own your data. It's just text files.

## Technology

Built with:
- [Avalonia UI](https://avaloniaui.net/) - Cross-platform .NET UI framework
- [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) - MVVM utilities
- .NET 9.0

## License

[Add your license here]
