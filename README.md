# 🎰 Slot Machine Game — Unity Assignment
### Underpin Technology | Unity Developer Intern

---

## 📖 Game Overview

A fully functional 3-reel slot machine game built in **Unity 6.3 LTS**.  
The player spins 3 reels, each displaying one of 4 classic symbols. A **win occurs when all 3 reels show the same symbol**. The game features smooth scrolling reel animations, a weighted RNG system, multiple bet options, a payout system, a bonus free-spin feature, and a complete UI built using Unity's Canvas system with the provided assets.

---

## 🎮 How to Play

1. Use the **◀ / ▶ arrow buttons** to change your bet (10 / 50 / 100 coins)
2. Press the **SPIN button** or **pull the lever** on the right side of the machine
3. Watch the 3 reels spin and stop — left → center → right
4. **Win** if all 3 reels land on the same symbol!
5. If you run out of coins, choose **YES** to restart or **NO** to stop

---

## 🚀 How to Run the WebGL Build

### Option A — Local Server (recommended)
```bash
# Using Node.js / npx
cd Build/WebGL
npx serve .

# Then open http://localhost:3000 in your browser
```

### Option B — VS Code Live Server
1. Open the `Build/WebGL/` folder in VS Code
2. Right-click `index.html` → **Open with Live Server**

### Option C — Python
```bash
cd Build/WebGL
python -m http.server 8080
# Open http://localhost:8080
```

> ⚠️ WebGL builds **cannot** be opened directly as a file (`file://`). Always use a local server.

---

## 🃏 Symbols & Paytable

| Symbol | Name   | Payout (× Bet) | Rarity  |
|--------|--------|---------------|---------|
| 7️⃣    | Seven  | **10×**       | Rare    |
| 🔔     | Bell   | **5×**        | Uncommon|
| ▬      | Bar    | **3×**        | Common  |
| 🍒     | Cherry | **2×**        | Common  |

---

## ⭐ Bonus Feature — Free Spins

- Landing **3 Cherries** awards **3 Free Spins**
- Free spins do **not** deduct from your balance
- The free spin counter is shown above the machine during bonus rounds
- This was chosen as the bonus because Cherry is the classic slot machine bonus trigger

---

## 🏗️ Project Structure

```
Assets/
├── Scripts/
│   ├── SymbolData.cs       — ScriptableObject: symbol config (sprite, payout, weight)
│   ├── Reel.cs             — Reel animation, strip scrolling, snap-to-result
│   ├── PayoutManager.cs    — Win detection, payout calculation, events
│   ├── SlotMachine.cs      — Core game controller; coordinates all systems
│   ├── UIManager.cs        — All UI updates (balance, bet, messages, popups)
│   ├── LeverButton.cs      — Lever click/pull animation handler
│   ├── AudioManager.cs     — Singleton audio player for all SFX
│   └── WinAnimator.cs      — Pulsing win panel animation
├── Prefabs/
│   ├── Reel.prefab         — Single reel (strip + visible slots)
│   └── SlotMachine.prefab  — Full assembled machine
├── Animations/
│   └── LeverPull.anim      — Lever pull animation clip
├── ScriptableObjects/
│   ├── Symbol_Seven.asset
│   ├── Symbol_Cherry.asset
│   ├── Symbol_Bell.asset
│   └── Symbol_Bar.asset
├── UI/
│   └── (all provided PNG assets)
├── Sounds/
│   └── (optional SFX clips)
└── Scenes/
    └── SlotGame.unity      — Main game scene
Build/
└── WebGL/
    ├── index.html
    └── ...
README.md
```

---

## 🧠 My Thought Process & Approach

### Architecture
I structured the game around **single-responsibility classes**:
- `Reel.cs` knows only about spinning and stopping
- `PayoutManager.cs` knows only about win logic
- `SlotMachine.cs` orchestrates everything but delegates all display work to `UIManager.cs`

This keeps the code modular — you could swap the payout logic or animation system without touching the others.

### Reel Animation
The reel animation works by scrolling a vertical "strip" of symbol Image GameObjects downward using `transform.anchoredPosition`. The strip wraps seamlessly (like a ticker tape). When stopping, the strip is hidden and three static Image slots show the final result — this gives a clean, perfectly aligned stop every time without fighting physics or snapping issues.

Deceleration uses a **quadratic ease-out** (`t * t`) which feels natural and casino-like.

### RNG & Fairness
Symbols are assigned **weight values** (e.g. Seven = weight 1, Cherry = weight 5). The weighted pool is expanded at startup into a flat list, and `Random.Range` picks from it. This is the standard weighted-random approach used in game development — simple, readable, and fair.

### Bonus Feature
The Cherry free-spin bonus was chosen because it's a classic slot machine mechanic that adds excitement without complex state machines. It re-uses the existing spin flow — free spins just skip the bet deduction step.

### WebGL Compatibility
- Used `UnityEngine.Random` (not `System.Random`) throughout — WebGL-safe
- Avoided any threading or file I/O
- All UI is built on Unity's Canvas system — renders correctly in browser

---

## 🛠️ Built With

- **Unity 6.3 LTS**
- **TextMeshPro** for all UI text
- **Unity UI (uGUI)** Canvas system
- **C#** — OOP architecture with ScriptableObjects, Coroutines, and Events

---

*Built for the Underpin Technology Unity Developer Intern Assignment — May 2026*
