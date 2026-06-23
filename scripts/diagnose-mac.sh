#!/usr/bin/env bash
#
# diagnose-mac.sh — run this on the Mac where BTD6 + MelonLoader are installed.
#
# It answers: Where is BTD6? Is it IL2CPP? Is MelonLoader installed and what version?
# Where's the Mods folder? And most importantly: what does the MelonLoader log say
# about whether it injected?
#
# Usage:  ./scripts/diagnose-mac.sh
#
set -uo pipefail

say()  { printf "\n\033[1;36m== %s ==\033[0m\n" "$1"; }
ok()   { printf "  \033[1;32m✓\033[0m %s\n" "$1"; }
warn() { printf "  \033[1;33m!\033[0m %s\n" "$1"; }
bad()  { printf "  \033[1;31m✗\033[0m %s\n" "$1"; }

# --- 1. Find the BTD6 install -------------------------------------------------
say "Locating BloonsTD6"

CANDIDATES=(
  "$HOME/Library/Application Support/Steam/steamapps/common/BloonsTD6"
  "/Applications/BloonsTD6.app"
)
BTD6_DIR=""
for c in "${CANDIDATES[@]}"; do
  if [ -e "$c" ]; then BTD6_DIR="$c"; break; fi
done

if [ -z "$BTD6_DIR" ]; then
  warn "Not in the usual spots. Searching (this can take a minute)…"
  BTD6_DIR="$(find "$HOME/Library/Application Support/Steam" /Applications -iname 'BloonsTD6*' -maxdepth 6 2>/dev/null | head -n1)"
fi

if [ -z "$BTD6_DIR" ]; then
  bad "Could not find BloonsTD6. Is it installed via Steam on this Mac?"
  exit 1
fi
ok "Found: $BTD6_DIR"

# Steam app bundles the game inside the .app; normalize to the folder that holds the data.
APP="$(find "$BTD6_DIR" -maxdepth 2 -iname 'BloonsTD6.app' 2>/dev/null | head -n1)"
[ -z "$APP" ] && [ -d "$BTD6_DIR" ] && APP="$BTD6_DIR"
ok "App/data root: $APP"

# --- 2. Confirm it's IL2CPP (this is the hard-mode flag) ----------------------
say "Engine type (IL2CPP vs Mono)"

if find "$APP" -iname 'GameAssembly.dylib' 2>/dev/null | grep -q .; then
  GA="$(find "$APP" -iname 'GameAssembly.dylib' 2>/dev/null | head -n1)"
  warn "IL2CPP detected (GameAssembly.dylib). This is the 'flaky on macOS' path."
  ok "  $GA"
elif find "$APP" -iname '*.dll' -path '*Managed*' 2>/dev/null | grep -q .; then
  ok "Looks like Mono (Managed/ assemblies). MelonLoader is much happier here."
else
  warn "Could not clearly determine engine type. Listing data dir contents:"
  find "$APP" -maxdepth 3 -iname '*.dylib' 2>/dev/null | sed 's/^/    /'
fi

# --- 3. MelonLoader presence + version ----------------------------------------
say "MelonLoader"

ML_DIR="$(find "$APP" "$BTD6_DIR" -maxdepth 4 -type d -iname 'MelonLoader' 2>/dev/null | head -n1)"
if [ -z "$ML_DIR" ]; then
  bad "No MelonLoader/ directory found near the game."
else
  ok "MelonLoader dir: $ML_DIR"
  ML_DLL="$(find "$ML_DIR" -iname 'MelonLoader.dll' 2>/dev/null | head -n1)"
  if [ -n "$ML_DLL" ]; then
    ok "MelonLoader.dll: $ML_DLL"
    # Try to read the assembly version cheaply.
    VER="$(strings "$ML_DLL" 2>/dev/null | grep -Eo '0\.[0-9]+\.[0-9]+(\.[0-9]+)?' | head -n1)"
    [ -n "$VER" ] && ok "Reported version-ish string: $VER"
    echo "    -> Set MELONLOADER_DLL in HelloBTD6.csproj to this path when building."
  else
    warn "MelonLoader dir exists but MelonLoader.dll not found inside it."
  fi
fi

# --- 4. Mods folder -----------------------------------------------------------
say "Mods folder"

MODS_DIR="$(find "$APP" "$BTD6_DIR" -maxdepth 4 -type d -iname 'Mods' 2>/dev/null | head -n1)"
if [ -z "$MODS_DIR" ]; then
  warn "No Mods/ folder yet (MelonLoader usually creates it on first launch)."
else
  ok "Mods dir: $MODS_DIR"
  echo "    Drop HelloBTD6.dll here to test, then launch the game."
  COUNT="$(find "$MODS_DIR" -iname '*.dll' 2>/dev/null | wc -l | tr -d ' ')"
  ok "Currently $COUNT .dll(s) in Mods/"
  find "$MODS_DIR" -iname '*.dll' 2>/dev/null | sed 's/^/      /'
fi

# --- 5. The log (the most useful part) ----------------------------------------
say "MelonLoader log (latest)"

LOG="$(find "$BTD6_DIR" "$APP" -iname 'Latest.log' -path '*MelonLoader*' 2>/dev/null | head -n1)"
[ -z "$LOG" ] && LOG="$(find "$BTD6_DIR" "$APP" -maxdepth 5 -iname 'Latest.log' 2>/dev/null | head -n1)"

if [ -z "$LOG" ]; then
  warn "No MelonLoader Latest.log found yet. Launch the game once, then re-run this."
else
  ok "Log: $LOG"
  echo "    --- last 40 lines ---"
  tail -n 40 "$LOG" | sed 's/^/    /'
  echo
  if grep -qi 'HelloBTD6' "$LOG"; then
    ok "Our test mod (HelloBTD6) appears in the log — INJECTION WORKS. 🎉"
  fi
  if grep -qiE 'error|exception|failed' "$LOG"; then
    warn "Log contains errors/exceptions — skim them above; that's our clue."
  fi
fi

say "Done"
echo "Paste the log output into JOURNAL.md so we have a record of this run."
