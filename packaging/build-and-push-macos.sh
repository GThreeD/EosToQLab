#!/usr/bin/env bash
set -euo pipefail

root="$(cd "$(dirname "$0")/.." && pwd)"
cd "$root"

if [[ "$(uname -s)" != "Darwin" ]]; then
  echo "Dieses Skript muss auf macOS ausgeführt werden." >&2
  exit 1
fi

if ! command -v gh >/dev/null 2>&1; then
  echo "GitHub CLI fehlt. Installation: brew install gh" >&2
  exit 1
fi

version="${1:?Version erforderlich, zum Beispiel v1.0.0}"

if [[ ! "$version" =~ ^v[0-9]+\.[0-9]+\.[0-9]+([.-][A-Za-z0-9.-]+)?$ ]]; then
  echo "Ungültige Version: $version" >&2
  echo "Beispiel: v1.0.0 oder v1.0.0-beta.1" >&2
  exit 1
fi

if [[ -n "$(git status --porcelain)" ]]; then
  echo "Das Git-Arbeitsverzeichnis enthält nicht commitete Änderungen." >&2
  exit 1
fi

echo "Verwende:"
dotnet --version
xcodebuild -version

rm -rf "$root/dist"
mkdir -p "$root/dist"

./packaging/publish-macos.sh arm64
./packaging/publish-macos.sh x64

arm64_zip="$root/dist/EosToQLab-arm64.zip"
x64_zip="$root/dist/EosToQLab-x64.zip"

for file in "$arm64_zip" "$x64_zip"; do
  if [[ ! -f "$file" ]]; then
    echo "Release-Datei fehlt: $file" >&2
    exit 1
  fi
done

gh release create "$version" \
  "$arm64_zip#EosToQLab für Apple Silicon" \
  "$x64_zip#EosToQLab für Intel Macs" \
  --title "EosToQLab $version" \
  --generate-notes \
  --target "$(git rev-parse HEAD)"

echo "Release $version wurde veröffentlicht."