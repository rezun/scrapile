#!/bin/bash
set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PUB_DIR="$SCRIPT_DIR/pub"
PROJECT="$SCRIPT_DIR/Scrapile.Desktop/Scrapile.Desktop.csproj"

# Clean and create pub directories
rm -rf "$PUB_DIR"
mkdir -p "$PUB_DIR/macos" "$PUB_DIR/macos-slim"
mkdir -p "$PUB_DIR/windows" "$PUB_DIR/windows-slim"
mkdir -p "$PUB_DIR/linux" "$PUB_DIR/linux-slim"

echo "Publishing Scrapile..."
echo ""

# =============================================================================
# macOS (Apple Silicon)
# =============================================================================

# macOS - Self-contained
echo "Building macOS (arm64) - self-contained..."
dotnet msbuild "$PROJECT" \
    -t:BundleApp \
    -p:RuntimeIdentifier=osx-arm64 \
    -p:Configuration=Release \
    -p:UseAppHost=true \
    -p:SelfContained=true \
    -verbosity:quiet

cp -R "$SCRIPT_DIR/Scrapile.Desktop/bin/Release/net9.0/osx-arm64/publish/Scrapile.app" "$PUB_DIR/macos/"

# macOS - Framework-dependent (slim)
echo "Building macOS (arm64) - slim..."
dotnet msbuild "$PROJECT" \
    -t:BundleApp \
    -p:RuntimeIdentifier=osx-arm64 \
    -p:Configuration=Release \
    -p:UseAppHost=true \
    -p:SelfContained=false \
    -verbosity:quiet

cp -R "$SCRIPT_DIR/Scrapile.Desktop/bin/Release/net9.0/osx-arm64/publish/Scrapile.app" "$PUB_DIR/macos-slim/"

# =============================================================================
# Windows (x64)
# =============================================================================

# Windows - Self-contained
echo "Building Windows (x64) - self-contained..."
dotnet publish "$PROJECT" \
    -r win-x64 \
    -c Release \
    -p:UseAppHost=true \
    -p:SelfContained=true \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    --verbosity quiet

cp "$SCRIPT_DIR/Scrapile.Desktop/bin/Release/net9.0/win-x64/publish/Scrapile.exe" "$PUB_DIR/windows/"
# Native libs that can't be bundled
cp "$SCRIPT_DIR/Scrapile.Desktop/bin/Release/net9.0/win-x64/publish/"*.dll "$PUB_DIR/windows/" 2>/dev/null || true

# Windows - Framework-dependent (slim)
echo "Building Windows (x64) - slim..."
dotnet publish "$PROJECT" \
    -r win-x64 \
    -c Release \
    -p:UseAppHost=true \
    -p:SelfContained=false \
    -p:PublishSingleFile=true \
    --verbosity quiet

cp "$SCRIPT_DIR/Scrapile.Desktop/bin/Release/net9.0/win-x64/publish/Scrapile.exe" "$PUB_DIR/windows-slim/"
# Native libs that can't be bundled
cp "$SCRIPT_DIR/Scrapile.Desktop/bin/Release/net9.0/win-x64/publish/"*.dll "$PUB_DIR/windows-slim/" 2>/dev/null || true

# =============================================================================
# Linux (x64)
# =============================================================================

# Linux builds are not needed at this time, so they are commented out.

# # Linux - Self-contained
# echo "Building Linux (x64) - self-contained..."
# dotnet publish "$PROJECT" \
#     -r linux-x64 \
#     -c Release \
#     -p:UseAppHost=true \
#     -p:SelfContained=true \
#     -p:PublishSingleFile=true \
#     -p:IncludeNativeLibrariesForSelfExtract=true \
#     --verbosity quiet

# cp "$SCRIPT_DIR/Scrapile.Desktop/bin/Release/net9.0/linux-x64/publish/Scrapile" "$PUB_DIR/linux/"
# # Native libs that can't be bundled
# cp "$SCRIPT_DIR/Scrapile.Desktop/bin/Release/net9.0/linux-x64/publish/"*.so "$PUB_DIR/linux/" 2>/dev/null || true

# # Linux - Framework-dependent (slim)
# echo "Building Linux (x64) - slim..."
# dotnet publish "$PROJECT" \
#     -r linux-x64 \
#     -c Release \
#     -p:UseAppHost=true \
#     -p:SelfContained=false \
#     -p:PublishSingleFile=true \
#     --verbosity quiet

# cp "$SCRIPT_DIR/Scrapile.Desktop/bin/Release/net9.0/linux-x64/publish/Scrapile" "$PUB_DIR/linux-slim/"
# # Native libs that can't be bundled
# cp "$SCRIPT_DIR/Scrapile.Desktop/bin/Release/net9.0/linux-x64/publish/"*.so "$PUB_DIR/linux-slim/" 2>/dev/null || true

# =============================================================================
# Summary
# =============================================================================

echo ""
echo "Done! Output in $PUB_DIR:"
echo ""
echo "=== Self-contained (includes .NET runtime) ==="
echo ""
echo "macos:"
ls -lh "$PUB_DIR/macos"
echo ""
echo "windows:"
ls -lh "$PUB_DIR/windows"
echo ""
echo "linux:"
ls -lh "$PUB_DIR/linux"
echo ""
echo "=== Slim (requires .NET runtime installed) ==="
echo ""
echo "macos-slim:"
ls -lh "$PUB_DIR/macos-slim"
echo ""
echo "windows-slim:"
ls -lh "$PUB_DIR/windows-slim"
echo ""
echo "linux-slim:"
ls -lh "$PUB_DIR/linux-slim"
