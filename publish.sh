#!/bin/bash
set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PUB_DIR="$SCRIPT_DIR/pub"
PROJECT="$SCRIPT_DIR/Scrapile.Desktop/Scrapile.Desktop.csproj"

# Clean and create pub directory
rm -rf "$PUB_DIR"
mkdir -p "$PUB_DIR/macos" "$PUB_DIR/windows" "$PUB_DIR/linux"

echo "Publishing Scrapile (self-contained)..."
echo ""

# macOS (Apple Silicon) - .app bundle is inherently a "single package"
echo "Building macOS (arm64)..."
dotnet msbuild "$PROJECT" \
    -t:BundleApp \
    -p:RuntimeIdentifier=osx-arm64 \
    -p:Configuration=Release \
    -p:UseAppHost=true \
    -p:SelfContained=true \
    -verbosity:quiet

cp -R "$SCRIPT_DIR/Scrapile.Desktop/bin/Release/net9.0/osx-arm64/publish/Scrapile.app" "$PUB_DIR/macos/"

# Windows (x64) - single exe + native dlls
echo "Building Windows (x64)..."
dotnet publish "$PROJECT" \
    -r win-x64 \
    -c Release \
    -p:UseAppHost=true \
    -p:SelfContained=true \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    --verbosity quiet

cp "$SCRIPT_DIR/Scrapile.Desktop/bin/Release/net9.0/win-x64/publish/Scrapile.Desktop.exe" "$PUB_DIR/windows/"
# Native libs that can't be bundled
cp "$SCRIPT_DIR/Scrapile.Desktop/bin/Release/net9.0/win-x64/publish/"*.dll "$PUB_DIR/windows/" 2>/dev/null || true

# Linux (x64) - single binary + native libs
echo "Building Linux (x64)..."
dotnet publish "$PROJECT" \
    -r linux-x64 \
    -c Release \
    -p:UseAppHost=true \
    -p:SelfContained=true \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    --verbosity quiet

cp "$SCRIPT_DIR/Scrapile.Desktop/bin/Release/net9.0/linux-x64/publish/Scrapile.Desktop" "$PUB_DIR/linux/"
# Native libs that can't be bundled
cp "$SCRIPT_DIR/Scrapile.Desktop/bin/Release/net9.0/linux-x64/publish/"*.so "$PUB_DIR/linux/" 2>/dev/null || true

echo ""
echo "Done! Output in $PUB_DIR:"
echo ""
echo "macos:"
ls -lh "$PUB_DIR/macos"
echo ""
echo "windows:"
ls -lh "$PUB_DIR/windows"
echo ""
echo "linux:"
ls -lh "$PUB_DIR/linux"
