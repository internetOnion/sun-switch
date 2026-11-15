# Sun Switch

A 3D obby where the world flips between **Day** and **Night**. Platforms, hazards and shards exist in Day, Night, or both. You play the last Lightkeeper and switch the phase to make the path appear as you climb a tower of floating sky-ruins.

## Goal

Climb the tower to the summit and restore the cycle of the sun and moon. Along the way, collect the 12 shards (3 per section) and finish with as little time and as few deaths as you can.

## Rules

- **Switch phase** at any time, even in mid-air. There is a 0.4 second cooldown.
- Objects are tagged **Day** (gold), **Night** (blue/purple) or **Both** (grey stone). Only objects in the current phase are solid. The other phase shows as see-through ghosts so you can plan your jumps.
- A switch is **blocked** if you are overlapping an object of the incoming phase.
- You have **3 lives for the whole run**. Falling into the void or touching a kill surface sends you back to your last checkpoint and costs 1 life. At 0 lives it is game over and you restart from the beginning.
- **Checkpoints** save your position and the phase you were in when you touched them.
- **Shards** are each Day-only or Night-only and count for score only.
- The end screen shows your time, deaths and shards.

## Map

One continuous climb of four sections:

| # | Section | What it teaches |
|---|---|---|
| 1 | Mossy Base | Switching on the ground, ghosts, blocked switches |
| 2 | Aqueducts | Mid-air switching and jump pads |
| 3 | Clockwork | Phased hazards: switch to dodge as well as to land |
| 4 | Summit | Auto-cycle finale: the world flips on a timer and manual switching is disabled |

## Controls

| Action | Input |
|---|---|
| Move | W A S D |
| Look | Mouse |
| Jump | Space |
| Sprint | Shift |
| Switch Day / Night | Right Mouse |
| Pause | Esc |

## Technology

- Unity **6000.3.8f1** (use exactly this version) with the Universal Render Pipeline
- Input System, Cinemachine and the Starter Assets third-person controller
- C#. Gameplay scripts live in `Assets/Scripts`, and editor tools that build the scenes and prefabs are in `Assets/Art/Editor`
- Scenes: `Main.unity` holds the player, camera, managers and UI, and loads `Section1` to `Section4` additively

To run the project, open it in Unity 6000.3.8f1, open `Assets/Scenes/Main.unity` and press Play. To make a build, see [docs/RELEASE_BUILD.md](docs/RELEASE_BUILD.md). The design is in [GDD.md](GDD.md) and third-party assets are listed in [CREDITS.md](CREDITS.md).

## Contribution

1. Create a new branch off `main` for each feature or fix.
2. Keep scene changes small. Shared obstacles are prefabs, so tune them there.
3. Commit every imported asset together with its `.meta` file, and add its source and licence to `CREDITS.md`.
4. Check that `Main.unity` still plays from start to finish, then open a pull request into `main`.
