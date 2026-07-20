#!/usr/bin/env bash
set -euo pipefail

architecture="${1:?Architecture required: arm64 or x64}"

root="$(cd "$(dirname "$0")/.." && pwd)"
project="$root/src/EosToQLab.Application/EosToQLab.Application.csproj"
dist_dir="$root/dist"

case "$architecture" in
  arm64)
    runtime="maccatalyst-arm64"
    ;;
  x64)
    runtime="maccatalyst-x64"
    ;;
  *)
    echo "Unsupported architecture: $architecture" >&2
    exit 1
    ;;
esac

rm -rf "$dist_dir/EosToQLab-$architecture"
mkdir -p "$dist_dir/EosToQLab-$architecture"

dotnet publish "$project" \
  -f net10.0-maccatalyst \
  -c Release \
  -r "$runtime" \
  --self-contained true \
  -p:CreatePackage=false

app_path="$root/src/EosToQLab.Application/bin/Release/net10.0-maccatalyst/$runtime/EosToQLab.app"

if [[ ! -d "$app_path" ]]; then
  echo "EosToQLab.app was not found at $app_path" >&2
  exit 1
fi

cp -R "$app_path" "$dist_dir/EosToQLab-$architecture/"

ditto -c -k \
  --sequesterRsrc \
  --keepParent \
  "$dist_dir/EosToQLab-$architecture/EosToQLab.app" \
  "$dist_dir/EosToQLab-$architecture.zip"

echo "Created: $dist_dir/EosToQLab-$architecture.zip"