#!/usr/bin/env bash
set -euo pipefail

architecture="${1:-arm64}"
case "$architecture" in
  arm64) runtime="maccatalyst-arm64" ;;
  x64) runtime="maccatalyst-x64" ;;
  *) echo "Usage: $0 [arm64|x64]" >&2; exit 2 ;;
esac

root="$(cd "$(dirname "$0")/.." && pwd)"
project="$root/src/EosToQLab.Application/EosToQLab.Application.csproj"
publish_dir="$root/artifacts/publish/$runtime"
dist_dir="$root/dist"

rm -rf "$publish_dir"
mkdir -p "$publish_dir" "$dist_dir"

dotnet publish "$project" \
  -f net10.0-maccatalyst \
  -c Release \
  -r "$runtime" \
  --self-contained true \
  -p:CreatePackage=false \
  -p:PublishDir="$publish_dir/"

app_path="$(find "$publish_dir" -maxdepth 2 -type d -name 'EosToQLab.app' -print -quit)"
if [[ -z "$app_path" ]]; then
  echo "EosToQLab.app was not found under $publish_dir" >&2
  exit 1
fi

codesign --force --deep --sign - "$app_path"
output_app="$dist_dir/EosToQLab-$architecture.app"
rm -rf "$output_app"
cp -R "$app_path" "$output_app"

ditto -c -k --sequesterRsrc --keepParent \
  "$output_app" \
  "$dist_dir/EosToQLab-$architecture.zip"

printf 'Created:\n  %s\n  %s\n' "$output_app" "$dist_dir/EosToQLab-$architecture.zip"
