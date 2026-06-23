#!/usr/bin/env bash
#
# build-and-install.sh — builds the HelloBTD6 test mod and drops it into BTD6's Mods folder.
# Designed so Hugh can run it with ONE command. Lots of friendly messages.
#
set -uo pipefail

step() { printf "\n👉 %s\n" "$1"; }
ok()   { printf "   ✅ %s\n" "$1"; }
bad()  { printf "   ❌ %s\n" "$1"; }

ROOT="$(cd "$(dirname "$0")/.." && pwd)"

# --- 1. Is the build tool installed? -----------------------------------------
step "Checking that the build tool (dotnet) is installed..."
if ! command -v dotnet >/dev/null 2>&1; then
  bad "I can't find 'dotnet'. A grown-up needs to install the .NET SDK first."
  echo "      Ask David to run:   brew install --cask dotnet-sdk"
  exit 1
fi
ok "Found dotnet ($(dotnet --version))"

# --- 2. Build the mod ---------------------------------------------------------
step "Building your mod (turning the C# code into a .dll the game can use)..."
if ! dotnet build -c Release "$ROOT/src/HelloBTD6/HelloBTD6.csproj"; then
  bad "The build didn't finish. Don't worry — show this whole window to David."
  exit 1
fi
DLL="$ROOT/src/HelloBTD6/bin/Release/HelloBTD6.dll"
if [ ! -f "$DLL" ]; then
  bad "It said it built, but I can't find HelloBTD6.dll. Show David this window."
  exit 1
fi
ok "Built your mod: HelloBTD6.dll"

# --- 3. Find the game's Mods folder ------------------------------------------
step "Finding the game's Mods folder..."
BTD6="$HOME/Library/Application Support/Steam/steamapps/common/BloonsTD6"
MODS="$(find "$BTD6" -maxdepth 4 -type d -iname 'Mods' 2>/dev/null | head -n1)"
if [ -z "$MODS" ]; then
  bad "I couldn't find the Mods folder."
  echo "      Has the game been opened ONE time after installing MelonLoader?"
  echo "      Try opening Bloons TD 6 once, quit, then run this again."
  exit 1
fi
ok "Found it: $MODS"

# --- 4. Copy the mod in ------------------------------------------------------
step "Putting your mod into the game..."
cp "$DLL" "$MODS/"
xattr -dr com.apple.quarantine "$MODS/HelloBTD6.dll" 2>/dev/null
ok "Your mod is now in the game!"

cat <<'DONE'

🎉 ALL DONE! Here's what to do next:

   1) Open Bloons TD 6
   2) Wait on the main menu for about 15 seconds, then quit the game
   3) Type this to see if your mod said hello:

         ./scripts/diagnose-mac.sh

DONE
