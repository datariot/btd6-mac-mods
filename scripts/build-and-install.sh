#!/usr/bin/env bash
#
# build-and-install.sh — builds EVERY mod in src/ and drops them into BTD6's Mods folder.
# Designed so a kid can run it with ONE command. Lots of friendly messages.
#
set -uo pipefail

step() { printf "\n👉 %s\n" "$1"; }
ok()   { printf "   ✅ %s\n" "$1"; }
bad()  { printf "   ❌ %s\n" "$1"; }

ROOT="$(cd "$(dirname "$0")/.." && pwd)"

# --- 0. Feature flag: developer "util" mods ----------------------------------
# A mod whose folder contains a ".util" marker file is a developer utility
# (diagnostics, probes) — it is SKIPPED by default so the live game stays clean.
# Pass --with-utils (or -u) to build+install those too.
WITH_UTILS=0
for arg in "$@"; do
  case "$arg" in
    -u|--with-utils|--utils) WITH_UTILS=1 ;;
    -h|--help)
      echo "usage: build-and-install.sh [--with-utils]"
      echo "  --with-utils   also build mods marked as developer utilities (a .util file in the mod folder)"
      exit 0 ;;
  esac
done

# --- 1. Is the build tool installed? -----------------------------------------
step "Checking that the build tool (dotnet) is installed..."
if ! command -v dotnet >/dev/null 2>&1; then
  bad "I can't find 'dotnet'. A grown-up needs to install the .NET SDK first."
  echo "      Ask a grown-up to run:   brew install --cask dotnet-sdk"
  exit 1
fi
ok "Found dotnet ($(dotnet --version))"

# --- 2. Find the game's Mods folder ------------------------------------------
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

# --- 3. Build + install every mod in src/ ------------------------------------
built=0
failed=0
skipped_utils=()
for proj in "$ROOT"/src/*/*.csproj; do
  [ -f "$proj" ] || continue
  name="$(basename "$proj" .csproj)"
  # Developer utilities (folder has a .util marker) are off unless --with-utils.
  if [ -f "$(dirname "$proj")/.util" ] && [ "$WITH_UTILS" -eq 0 ]; then
    skipped_utils+=("$name")
    continue
  fi
  step "Building $name..."
  if ! dotnet build -c Release "$proj" -o "$(dirname "$proj")/bin" >/dev/null; then
    bad "$name didn't build — show this window to a grown-up."
    failed=$((failed+1))
    continue
  fi
  dll="$(dirname "$proj")/bin/$name.dll"
  if [ ! -f "$dll" ]; then
    bad "$name said it built but I can't find $name.dll."
    failed=$((failed+1))
    continue
  fi
  cp "$dll" "$MODS/"
  xattr -dr com.apple.quarantine "$MODS/$name.dll" 2>/dev/null
  ok "Installed $name.dll"
  built=$((built+1))
done

echo ""
if [ "${#skipped_utils[@]}" -gt 0 ]; then
  printf "ℹ️  Skipped %d developer util(s): %s\n" "${#skipped_utils[@]}" "${skipped_utils[*]}"
  printf "   (run with --with-utils to include them)\n\n"
fi
if [ "$built" -gt 0 ] && [ "$failed" -eq 0 ]; then
  cat <<DONE
🎉 ALL DONE! Installed $built mods.

   1) Open Bloons TD 6
   2) Start a game on any map
   3) Mega Cash keeps your money maxed out — buy whatever you want! 💰
      (Game Speed: press 1-5 to change speed. Unlimited Upgrades: max all 3 paths.)
DONE
else
  echo "Built $built mod(s); $failed had problems. Show the window above to a grown-up."
  exit 1
fi
