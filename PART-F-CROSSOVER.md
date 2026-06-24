# 🍾 Part F — The Bottle Plan (CrossOver)

> Read [START-HERE.md](START-HERE.md) first if you haven't. This guide is for **after** we found out
> that mods won't load the normal way on this Mac.

## What happened, and the new plan

We did everything right — installed the loader, built a mod, got it into the game — but the game
**never loaded the loader**. That's because this Mac uses a kind of game (called *IL2CPP*) that
Apple's security won't let us hook into. Grown-up programmers hit this exact wall all the time. It's
not a mistake we made. 💪

**The new plan:** instead of fighting the Mac, we're going to build a tiny **pretend Windows computer
*inside* the Mac** — it's called a **"bottle"** — and run the **Windows** version of Bloons TD 6 in
there. On Windows, mods load the easy way. 🎉

Think of it like this: the Mac won't let us into the game's engine room. So we're bringing our own
little Windows room, and running the game in *that*.

> ⚠️ Heads up: this part is a bigger project (a download or two, some waiting). A grown-up does most of
> the setup, and **Hugh gets the best part at the end: actually installing mods.** 🏆

---

## Who does what

- 🧑 **Grown-up (David):** Missions G1–G4 (installing things, signing in, downloads).
- 🧒 **Hugh:** Missions H1–H2 (launching the game and installing mods — the fun part!).

---

# 🧑 Grown-up setup

## G1 — Install CrossOver

1. Go to **https://www.codeweavers.com/crossover** and download the free **14-day trial** (Mac).
2. Open the downloaded file and drag **CrossOver** to Applications, like any Mac app.
3. Open CrossOver. If macOS warns it's from the internet, right-click the app → **Open** → **Open**.

> Why CrossOver and not the free options? On this **Intel** Mac, CrossOver 26 is the most reliable and
> the easiest to follow. The popular free tool (Whisky) never supported Intel and was discontinued.
> The free-but-fiddlier alternative is **Sikarugir** — ask Claude for that version if you'd rather not
> pay after the trial. (Note: CrossOver **27** drops Intel support — stay on **26**.)

## G2 — Install Windows Steam in a bottle

1. In CrossOver, click **Install a Windows Application**.
2. Search for **Steam** and pick it. CrossOver knows Steam and will make a bottle and install it for
   you. Click through and let it finish (this takes a few minutes).

## G3 — Sign in and download the *Windows* Bloons TD 6

1. CrossOver launches **Steam** (the Windows one, inside the bottle).
2. **Sign in with your normal Steam account** — the same one that owns BTD6. (You do *not* buy it
   again; you already own it.)
3. Find **Bloons TD 6** in your library and click **Install**. Because we're "in Windows" now, this
   downloads the **Windows** version. It's a few GB — good time for a snack. 🍎
4. When it's done, **launch BTD6 once** the normal way to make sure the plain game runs in the bottle.
   Then quit.

## G4 — Add MelonLoader (the Windows one) to the bottle

1. Download the **Windows** MelonLoader Installer from:
   https://github.com/LavaGang/MelonLoader.Installer/releases
   (get the Windows `.exe`, not the Mac file this time).
2. In CrossOver, open the **same bottle** Steam is in → use **Run Command** (or "Run a Windows
   Application") and pick the MelonLoader Installer `.exe` you downloaded.
3. In the installer, **select Bloons TD 6** and click **Install**.
4. **Launch BTD6 once more.** This time, on Windows, MelonLoader shows a **black console window** with
   text scrolling — that's the loader working! If you see that console, we've won. ✅ Quit the game.

> If MelonLoader doesn't appear, the usual fix is the same as before: re-run the MelonLoader installer
> and re-select BTD6 (Steam sometimes resets it). Then try again.

---

# 🧒 Hugh's missions (the payoff!)

## H1 — See YOUR mod finally say hello

Remember the `HelloBTD6` mod you built? It works on Windows too! Let's run it in the bottle.

1. Ask David to copy your **`HelloBTD6.dll`** (from `src/HelloBTD6/bin/Release/`) into the bottle's
   BTD6 **`Mods`** folder. (David: it's inside the bottle's BTD6 game folder — CrossOver can open the
   bottle's files for you.)
2. Launch **Bloons TD 6**.
3. Watch the black console window. Look for:
   > `=== Hello from Hugh's mod! Injection works. ===`

That's **your code**, running inside a real game. You officially made a mod work. 🏆🎉

## H2 — Install real mods (the easy way)

Now the fun part — adding mods other people made (and later, your own):

1. The easiest tool is **BTD Mod Helper** — it adds a **Mods button right inside the game's menu** with
   a whole browser of mods you can install with one click.
2. David can grab it here: https://github.com/gurrenm3/BTD-Mod-Helper (download `Btd6ModHelper.dll`),
   and drop it into the same **`Mods`** folder.
3. Launch BTD6 → you'll see a **Mods** button on the main menu → open it → browse → install whatever
   looks fun → restart the game. 🎮

From here you two can try lots of mods — and when you're ready, you already know how to build your own
(that's what `src/HelloBTD6` taught you). The whole point of this project! 🚀

---

## 🆘 If something goes wrong

- **A download is stuck or super slow.** That's normal for big game files — give it time, or pause and
  resume in Steam.
- **The game won't start in the bottle.** Quit CrossOver fully and reopen it; try launching BTD6
  again. Older Intel Macs can be a little slow — be patient.
- **No black console / no Mods button.** Re-run the Windows MelonLoader installer (G4), re-select
  BTD6, relaunch.
- **Stuck?** Take a screenshot (⌘ + Shift + 4) and show David. Write what happened in `JOURNAL.md` —
  that's how real hackers keep track.

You did the hard part already, Hugh. This is the victory lap. 🏁
