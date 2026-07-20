#!/usr/bin/env bash
set -euo pipefail

root="$(cd "$(dirname "$0")/.." && pwd)"
cd "$root"

if [[ "$(uname -s)" != "Darwin" ]]; then
  echo "Dieses Skript muss auf macOS ausgeführt werden." >&2
  exit 1
fi

if [[ -n "$(git status --porcelain)" ]]; then
  echo "Das Git-Arbeitsverzeichnis enthält nicht commitete Änderungen." >&2
  echo "Bitte zuerst committen oder stashen." >&2
  exit 1
fi

echo "Verwende:"
dotnet --version
xcodebuild -version

dotnet workload install maui --skip-manifest-update

./packaging/publish-macos.sh arm64
./packaging/publish-macos.sh x64

git add \
  dist/EosToQLab-arm64.zip \
  dist/EosToQLab-x64.zip

if git diff --cached --quiet; then
  echo "Keine Änderungen an den Build-Artefakten."
  exit 0
fi

git commit -m "Update macOS builds [skip ci]"
git push

echo "macOS-Builds wurden erstellt und gepusht."