# HelloBTD6 — the injection smoke test

The smallest possible MelonLoader mod. It doesn't change the game; it just logs a line so we can
confirm **MelonLoader can inject and run our C# on this Mac**. That's the make-or-break question for
the whole project.

## What you need

- The **.NET SDK** (8.x is fine): https://dotnet.microsoft.com/download — or `brew install dotnet-sdk`
- BTD6 + MelonLoader already installed (✓ you have this).

## Steps

1. **Find the paths.** From the repo root:
   ```bash
   ../../scripts/diagnose-mac.sh
   ```
   Note the `MelonLoader dir`, the `Mods dir`, and whether the references in `HelloBTD6.csproj`
   point at real files. Adjust `<GameDir>` / `<MelonDir>` in the `.csproj` if needed.

2. **Build:**
   ```bash
   dotnet build -c Release
   # or override paths without editing the file:
   # dotnet build -c Release -p:MelonDir="/full/path/to/MelonLoader"
   ```
   Output: `bin/Release/HelloBTD6.dll`

3. **Install:** copy `HelloBTD6.dll` into the game's `Mods/` folder (diagnose-mac.sh prints it).

4. **Launch BTD6**, let it sit on the menu ~15 seconds, quit.

5. **Check the log:**
   ```bash
   ../../scripts/diagnose-mac.sh   # it tails the log and looks for "HelloBTD6"
   ```

## Reading the result

- ✅ You see `Hello from Hugh's mod! Injection works.` and `heartbeat` lines → **macOS modding is
  viable.** Next: try dropping in BTD Mod Helper + a real mod.
- ❌ No HelloBTD6 lines, or the log shows IL2CPP/injection errors → injection is the wall. Copy the
  errors into `../../JOURNAL.md` and we look at the CrossOver/Wine fallback.

## Likely gotchas (write what you hit into the JOURNAL)

- **`MelonLoader.dll` HintPath wrong** — its location differs between MelonLoader versions
  (sometimes `net6/`, sometimes `Managed/`). The build error tells you the missing file; fix the
  HintPath (or pass `-p:MelonDir=...`). This smoke test deliberately references *only* MelonLoader —
  no UnityEngine — so this is the only reference that can go wrong.
- **`TargetFramework` mismatch** — if the log says the mod targets the wrong runtime, switch
  `net6.0` ↔ the version MelonLoader reports.
- **Gatekeeper / quarantine** — macOS may quarantine a freshly built dll; if it's ignored silently,
  try `xattr -dr com.apple.quarantine bin/Release/HelloBTD6.dll`.
