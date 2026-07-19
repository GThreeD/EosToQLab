#!/usr/bin/env bash
set -euo pipefail

root="$(cd "$(dirname "$0")/.." && pwd)"
project="$root/src/EosToQLab.Application"
bundle_id="com.gthreed.eostoqlab"
process_name="EosToQLab"

# Rider can stop the debugger before UIKit has fully removed the previous
# Mac Catalyst scene. Kill any leftover process before deleting only the
# disposable debug output and saved-window/scene state.
pkill -x "$process_name" >/dev/null 2>&1 || true

rm -rf "$project/bin/Debug/net10.0-maccatalyst"
rm -rf "$project/obj/Debug/net10.0-maccatalyst"
rm -rf "$HOME/Library/Saved Application State/$bundle_id.savedState"
rm -rf "$HOME/Library/Containers/$bundle_id/Data/Library/Saved Application State/$bundle_id.savedState"

printf 'Reset Mac Catalyst debug state for %s. Rebuild and start the app again.\n' "$bundle_id"
