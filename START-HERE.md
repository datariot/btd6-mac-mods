# 🎮 START HERE — Hugh's Guide

Hi Hugh! 👋

You're about to do something that a lot of grown-up programmers find tricky: getting a **mod**
(a little add-on you write yourself) into a real video game. This is real hacking — the good kind.

It's totally okay if parts feel confusing. **You can't break the computer by typing the commands in
this guide.** If something looks weird, that's normal — just grab David and show him the screen.

We'll go one small mission at a time. Take your time. 🐢

---

## 🧠 First, three words you'll hear a lot

- **Terminal** — a window where you *type* commands to the computer instead of clicking. It looks
  plain and a little spooky, but it's just a place to talk to the Mac with words.
- **Command** — one line you type (or paste) and then press **Return** to run.
- **Mod** — an add-on for the game. This project has a bunch! The first one you'll see is **Mega
  Cash**, which keeps your money maxed out so you can buy any tower you want. 💰

---

## 👀 How the Terminal works (read this once)

- The Terminal shows a line with a `%` or `$` at the end. That's it saying **"I'm ready, type
  something."**
- You **type or paste** a command, then press **Return** to run it.
- To **copy** a command from this guide: highlight it and press **⌘ + C**.
  To **paste** it into the Terminal: click in the Terminal and press **⌘ + V**.
- **Tip:** press the **↑ (up arrow)** in the Terminal to bring back the last command you typed, so you
  don't have to retype it.
- If a command prints a wall of text — that's normal! Computers are chatty.

---

## 🧑 Before you start (this part is David's job — one time only)

> Hugh, skip to **Mission 1** below. This box is for David.

These steps need an admin password or a GitHub login, so a grown-up does them once:

1. Install the build tool:  `brew install --cask dotnet-sdk`
2. Put this project on the laptop (clone it to the Desktop):
   ```bash
   cd ~/Desktop
   git clone https://github.com/datariot/btd6-mac-mods.git
   ```
3. Make sure **Bloons TD 6** and **MelonLoader** are installed, and the game has been opened once
   (see the main `README.md` for the full install steps).

When that's done, the project folder lives at **`~/Desktop/btd6-mac-mods`** and Hugh can take over. 🎉

---

# 🚀 Hugh's Missions

Do these in order. After each one there's a ☐ box — imagine checking it off when it works!

---

## Mission 1 — Open the Terminal

1. Press **⌘ + Space** (hold the Command key, tap the spacebar). A little search box pops up.
2. Type the word:  `terminal`
3. Press **Return**.

A plain window opens with some text and a blinking cursor. **That's the Terminal!**

☐ *Done when: a Terminal window is open.*

---

## Mission 2 — Go to our project

Computers have folders inside folders. We need to tell the Terminal: *"go into our project folder."*
The command for "go into a folder" is `cd` (it means **c**hange **d**irectory — directory is just
another word for folder).

**Copy this, paste it into the Terminal, and press Return:**

```bash
cd ~/Desktop/btd6-mac-mods
```

You should see the line change to show it's now inside `btd6-mac-mods`. Nothing else happens — that's
correct! `cd` is quiet when it works.

☐ *Done when: the command ran with no red error.*

---

## Mission 3 — Look at the game

Let's have the computer check out the game and make sure everything's ready. We have a helper that
does the looking for you.

**Type this and press Return:**

```bash
./scripts/diagnose-mac.sh
```

It'll print a bunch of stuff with ✓ checkmarks. You don't have to understand all of it! You're just
looking to see lots of green ✓ marks. If you see scary red ✗ marks, take a screenshot and show David.

☐ *Done when: it finishes and you saw some ✓ checkmarks.*

---

## Mission 4 — Build the mods and put them in the game

This is the big one! This command turns all the code into mods and slips them into the game. One command
builds **every** mod and installs them.

**Type this and press Return:**

```bash
./scripts/build-and-install.sh
```

It will think for a bit (building takes a little time the first time — be patient ⏳), then print:

```
🎉 ALL DONE! Installed N mods.
```

If it says ❌ anywhere, don't worry — show David the window.

☐ *Done when: you see "🎉 ALL DONE!"*

---

## Mission 5 — Play the game (this is the test!)

1. Open **Bloons TD 6** like you normally would.
2. **Start a game** on any map.
3. Look at your **money** (top-left). With **Mega Cash** running, it stays maxed out — you can buy
   any tower you want, right away! 💰

☐ *Done when: you started a game and saw tons of money.*

---

## Mission 6 — Did it work?! 🥁

- **You had a giant pile of money the whole game** → YOU DID IT. You built mods and made them run
  inside a real game. That is genuinely awesome. 🏆 Go tell David!
- **Money looked normal (not maxed)** → that's okay! Run the look-around helper and show David what it
  says:
  ```bash
  ./scripts/diagnose-mac.sh
  ```

**Try the other mods too!** While you're in a game:

- **Game Speed** — press **1, 2, 3, 4, 5** to make the game go faster (5 = super fast! ⚡)
- **Unlimited Upgrades** — every monkey can max all **three** upgrade paths
- **Ability Monkey** — press the **`** key (top-left, under Esc) for a special panel

☐ *Done when: you tried Mega Cash (and maybe a few others — great job!).*

---

## 🆘 If something goes wrong

- **You see red text / the word "error".** Don't panic, nothing is broken. Take a screenshot
  (**⌘ + Shift + 4**, then drag a box) and show David.
- **You typed a command and nothing happened.** Make sure you pressed **Return**. Some commands are
  quiet when they work — that's fine.
- **"command not found".** You might be in the wrong folder. Run Mission 2's `cd` command again, then
  try again.
- **You're stuck or it's not fun anymore.** Take a break! This stuff is hard and you've already done
  more than most kids your age. 💪

---

## 📓 Bonus: write in the lab notebook

Real hackers keep notes. Open `JOURNAL.md` and write down what happened today — did it work? what was
confusing? That's how you get better. (David can show you how to edit it.)

You've got this, Hugh. Have fun. 🚀
