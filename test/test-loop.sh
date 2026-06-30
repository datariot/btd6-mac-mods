#!/bin/bash
# Autonomous BTD6-port detour test loop — no human play needed.
#  Bug A (overrun SIGILL): verified from the log (the fix self-checks for overrun).
#  Bug B (exit SIGBUS): triggered by a clean Apple-Event quit, then crash-report check.
set +e
GAME="$HOME/Library/Application Support/Steam/steamapps/common/BloonsTD6"
LOG="$GAME/MelonLoader/Latest.log"
DR=(~/Library/Logs/DiagnosticReports ~/Library/Logs/DiagnosticReports/Retired)
latest_crash(){ find "${DR[@]}" -iname 'BloonsTD6-*.ips' 2>/dev/null -exec basename {} \; | sort | tail -1; }
proc(){ pgrep -f 'MacOS/BloonsTD6$'; }

# Autonomously dismiss the title "Press Start" screen with an HID-level CGEvent click.
# Unity reads raw HID input and IGNORES AppKit "System Events" synthetic clicks, so we post a real
# CoreGraphics mouse click (hidclick) at the window centre. This is what actually advances the splash.
HIDCLICK="$(dirname "$0")/hidclick"
nudge_start(){
  osascript -e 'tell application "BloonsTD6" to activate' 2>/dev/null
  [ -x "$HIDCLICK" ] && "$HIDCLICK" 704 440 2>/dev/null
}

echo "=== [setup] kill any running instance + clear stale log ==="
pkill -9 -f 'MacOS/BloonsTD6$' 2>/dev/null; sleep 2
rm -f "$LOG"   # play.sh recreates a fresh Latest.log so detection sees only this run
BASE=$(latest_crash); echo "baseline crash: $BASE"

echo "=== [launch] ==="
cd ~/Workspace/macos-il2cpp-port && nohup ./play.sh >/tmp/btd6-loop-play.log 2>&1 &
# wait for menu (profile load) or death — cold boots can take a few minutes.
# Once the process is alive, nudge the title screen each iteration so profile load
# can proceed without a human clicking Start.
for i in $(seq 1 48); do
  grep -qE "Granted 999,999,999" "$LOG" 2>/dev/null && break
  if proc >/dev/null 2>&1 && [ "$i" -ge 3 ]; then nudge_start; fi
  sleep 5
done

MENU=$(grep -qE "Granted 999,999,999" "$LOG" && echo yes || echo no)
echo "menu reached: $MENU"

echo "=== [Bug A check] overrun guard / near-island ==="
ISLAND=$(grep -c "via near-island" "$LOG" 2>/dev/null)
OVERRUN=$(grep -c "OVERRUN NOT PREVENTED" "$LOG" 2>/dev/null)
grep "arm64-detour" "$LOG" 2>/dev/null | tail -3
# Bug A is proven by the fix's own post-hook byte check: a short fn hooked via the near-island with
# its neighbour verified intact, and zero overruns. This is independent of reaching the menu.
if [ "$ISLAND" -ge 1 ] && [ "$OVERRUN" -eq 0 ]; then
  echo "BUG_A: PASS (short-fn hooked via island, neighbour intact, overruns=$OVERRUN; boot_menu=$MENU)"
elif [ "$OVERRUN" -gt 0 ]; then
  echo "BUG_A: FAIL (overruns=$OVERRUN — a hook still clobbered its neighbour)"
else
  echo "BUG_A: INCONCLUSIVE (no short-fn hook seen this run; island=$ISLAND, menu=$MENU)"
fi

echo "=== [Mod Helper check] hooks, content APIs, class injection ==="
# Requires ModHelperProbe.dll deployed in Mods/ (a BloonsTD6Mod that self-tests Mod Helper on arm64).
if grep -q '\[PROBE\]' "$LOG" 2>/dev/null; then
  PFAIL=$(grep -c '\[PROBE\] FAIL' "$LOG" 2>/dev/null)
  INJECT=$(grep -c 'PASS custom ProbeTower registered' "$LOG" 2>/dev/null)
  HOOKS=$(grep -cE 'hook On(GameModelLoaded|MainMenu) FIRED' "$LOG" 2>/dev/null)
  grep -E '\[PROBE\] (PASS|FAIL|hook).*' "$LOG" 2>/dev/null | grep -iE 'FAIL|ProbeTower registered|OnMainMenu FIRED' | tail -4
  if [ "$PFAIL" -eq 0 ] && [ "$INJECT" -ge 1 ] && [ "$HOOKS" -ge 2 ]; then
    echo "MOD_HELPER: PASS (no probe failures, class-injection ok, hooks fired=$HOOKS)"
  else
    echo "MOD_HELPER: FAIL (probe_failures=$PFAIL, class_injection_pass=$INJECT, hooks=$HOOKS)"
  fi
else
  echo "MOD_HELPER: SKIP (ModHelperProbe not deployed / no [PROBE] output)"
fi

echo "=== [Bug B check] clean exit via Apple Event, watch for SIGBUS ==="
PID=$(proc)
osascript -e 'tell application "BloonsTD6" to quit' 2>/dev/null
# give it a clean-shutdown window; fall back to SIGTERM if still alive
for i in $(seq 1 12); do proc >/dev/null || break; sleep 1; done
if proc >/dev/null; then echo "(apple-event quit didn't exit; sending SIGTERM)"; kill -TERM "$PID" 2>/dev/null; sleep 3; fi
proc >/dev/null && { echo "(still alive; SIGKILL)"; kill -9 "$PID" 2>/dev/null; }
# crash reports land several seconds after the fault — poll, don't check once.
NEWC=$(latest_crash)
for i in $(seq 1 20); do
  NEWC=$(latest_crash)
  [ "$NEWC" != "$BASE" ] && break
  sleep 1
done
if [ "$NEWC" != "$BASE" ]; then
  echo "BUG_B: crash on exit -> $NEWC"
  F=$(find "${DR[@]}" -iname "$NEWC" 2>/dev/null | head -1)
  tail -n +2 "$F" | python3 -c "import sys,json;d=json.load(sys.stdin);t=d['threads'][d.get('faultingThread',0)];i=d['usedImages'][t['frames'][0]['imageIndex']];print('  signal:',d.get('exception',{}).get('signal'),'fault:',i.get('name'),'frame1:',(t['frames'][1].get('symbol','') if len(t['frames'])>1 else ''))" 2>/dev/null
else
  echo "BUG_B: PASS (clean exit, no new crash)"
fi
echo "=== [done] ==="