#!/usr/bin/env bash
#
# install-runtime.sh — installs the patched native-arm64 MelonLoader runtime into
# Bloons TD 6 so mods run on Apple Silicon Macs (no CrossOver, no Rosetta).
#
# Run this once. Then build the mods (see btd6-mac-mods repo) and launch with play.sh.
#
set -uo pipefail

step() { printf "\n👉 %s\n" "$1"; }
ok()   { printf "   ✅ %s\n" "$1"; }
bad()  { printf "   ❌ %s\n" "$1"; }

HERE="$(cd "$(dirname "$0")" && pwd)"
GAME="$HOME/Library/Application Support/Steam/steamapps/common/BloonsTD6"

# --- 0. Sanity checks --------------------------------------------------------
step "Checking this is an Apple Silicon Mac..."
if [ "$(uname -m)" != "arm64" ]; then
  bad "This runtime is for Apple Silicon (M1–M4) Macs only. uname says: $(uname -m)"
  echo "      On an Intel Mac, use the CrossOver route instead (see btd6-mac-mods PART-F)."
  exit 1
fi
ok "Apple Silicon detected."

step "Checking Bloons TD 6 is installed via Steam..."
if [ ! -d "$GAME/BloonsTD6.app" ]; then
  bad "Can't find BTD6 at: $GAME"
  echo "      Install Bloons TD 6 in Steam and launch it once normally, then re-run this."
  exit 1
fi
ok "Found BTD6: $GAME"

# --- 1. Copy the MelonLoader runtime into the game ---------------------------
step "Installing the patched MelonLoader runtime..."
cp -R "$HERE/MelonLoader" "$GAME/"
cp "$HERE/MelonLoader.Bootstrap.dylib" "$GAME/"
cp "$HERE/melonloader-launch.sh" "$GAME/"
chmod +x "$GAME/melonloader-launch.sh"
# These files aren't notarized by Apple — clear the quarantine flag so they load.
xattr -dr com.apple.quarantine "$GAME/MelonLoader" "$GAME/MelonLoader.Bootstrap.dylib" "$GAME/melonloader-launch.sh" 2>/dev/null
ok "Runtime installed."

# --- 2. GameAssembly.dylib symlink (the icall injector needs it findable) ----
step "Linking GameAssembly.dylib..."
ln -sf "$GAME/BloonsTD6.app/Contents/Frameworks/GameAssembly.dylib" "$GAME/GameAssembly.dylib"
ok "Linked."

# --- 3. Mods folder ----------------------------------------------------------
mkdir -p "$GAME/Mods"
ok "Mods folder ready: $GAME/Mods"

# --- 4. .NET 6 arm64 runtime (MelonLoader needs it) --------------------------
step "Checking for the .NET 6 (arm64) runtime..."
if [ -x "$HOME/.dotnet/dotnet" ] && "$HOME/.dotnet/dotnet" --list-runtimes 2>/dev/null | grep -q 'Microsoft.NETCore.App 6\.'; then
  ok "Already installed at ~/.dotnet"
else
  echo "   Installing .NET 6 arm64 runtime to ~/.dotnet (this downloads ~80 MB)..."
  if curl -fsSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 6.0 --runtime dotnet --arch arm64; then
    ok "Installed .NET 6 arm64 runtime."
  else
    bad "Couldn't auto-install .NET 6. Install it manually, then re-run:"
    echo "      curl -fsSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 6.0 --runtime dotnet --arch arm64"
  fi
fi

# --- 5. Window-state settings (avoid the macOS 'reopen windows?' modal) ------
step "Setting window preferences..."
defaults write com.ninjakiwi.bloonstd6 NSQuitAlwaysKeepsWindows -bool false 2>/dev/null
defaults write com.ninjakiwi.bloonstd6 ApplePersistenceIgnoreState -bool true 2>/dev/null
ok "Done."

# --- 6. Drop the launcher ----------------------------------------------------
cp "$HERE/play.sh" "$GAME/play.sh"
chmod +x "$GAME/play.sh"
ok "Launcher installed: $GAME/play.sh"

cat <<DONE

🎉 Runtime installed!

Next steps:
  1) Get the mods:   git clone https://github.com/datariot/btd6-mac-mods.git
  2) Build+install:  cd btd6-mac-mods && ./scripts/build-and-install.sh
                     (needs the .NET SDK:  brew install --cask dotnet-sdk)
  3) Play:           bash "$GAME/play.sh"

The FIRST launch regenerates the IL2CPP interop assemblies — give it a minute the
first time. After that it's quick.
DONE
