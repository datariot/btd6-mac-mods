#!/bin/bash
# Launch BTD6 with the patched MelonLoader + your mods, natively on Apple Silicon.
# Mods live in the game's Mods/ folder (build them from the btd6-mac-mods repo).
GAME="$HOME/Library/Application Support/Steam/steamapps/common/BloonsTD6"

# Stop any running copy first.
kill -9 $(pgrep -f 'BloonsTD6.app/Contents/MacOS/BloonsTD6') 2>/dev/null; sleep 1

# Clear macOS saved window state so a prior hard crash doesn't show the
# "reopen windows?" prompt that would block this launch.
rm -rf "$HOME/Library/Saved Application State/com.ninjakiwi.bloonstd6.savedState" 2>/dev/null

echo "Launching BTD6 with mods... (first launch regenerates interop, give it a minute)"
DOTNET_ROOT="$HOME/.dotnet" DOTNET_MULTILEVEL_LOOKUP=0 \
  /bin/bash "$GAME/melonloader-launch.sh" "$GAME/BloonsTD6.app"
