#!/usr/bin/env bash
# Download a community BTD6 Mod Helper mod's latest release DLL, convert it to AnyCPU (community mods
# ship as x64, which this arm64 port rejects), and install it into the game's Mods folder.
# Usage: tools/install-community-mod.sh <github-owner/repo>      e.g. doombubbles/Unlimited5thTiers
set -euo pipefail
REPO_ARG="${1:?usage: install-community-mod.sh <owner/repo>}"
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
GAME="$HOME/Library/Application Support/Steam/steamapps/common/BloonsTD6"
MODS="$(find "$GAME" -maxdepth 4 -type d -iname Mods 2>/dev/null | head -n1)"
[ -n "$MODS" ] || { echo "Mods folder not found — open the game once first"; exit 1; }

echo ">> finding latest release DLL for $REPO_ARG"
URL=$(curl -fsSL "https://api.github.com/repos/$REPO_ARG/releases/latest" \
  | python3 -c "import sys,json;d=json.load(sys.stdin);print(next(a['browser_download_url'] for a in d['assets'] if a['name'].endswith('.dll')))")
NAME="$(basename "$URL")"
echo ">> downloading $NAME"
curl -fsSL "$URL" -o "$MODS/$NAME"
xattr -dr com.apple.quarantine "$MODS/$NAME" 2>/dev/null || true

echo ">> converting to AnyCPU (if needed)"
dotnet run --project "$ROOT/tools/anycpu-convert" -c Release -- "$MODS/$NAME"

echo ">> installed $NAME into $MODS"
echo "   launch with ~/Workspace/macos-il2cpp-port/play.sh"
