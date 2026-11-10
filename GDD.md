# Sun Switch — Game Design Document

A 3D obby where the world flips between **Day** and **Night**. Platforms, hazards and collectibles exist in Day, Night, or Both — the player switches phase to make the path appear.

- **Engine:** Unity 6000.3.8f1 (pinned — everyone uses exactly this version), URP, Input System
- **Team:** 4 members
- **Timeline:** 7 days
- **Deliverable:** runnable Unity project (repo) + Windows standalone build

---

## 1. Fiction & Setting

You are the last **Lightkeeper**. The sun and moon have fallen out of sync — climb the tower and restore the cycle.

- One intro line on the start screen, one ending beat at the summit. No dialogue, NPCs or cutscenes.
- Setting: a **vertical tower of floating sky-ruins**, climbed bottom to top.

## 2. Core Mechanic — Phase Switch

| Rule | Decision |
|---|---|
| Control | **Free toggle** with **Right Mouse**, usable anytime, including mid-air |
| Cooldown | **0.4s**, shown as a radial fill on the HUD sun/moon icon |
| Object phases | Every phased object is tagged **Day**, **Night**, or **Both** |
| What switching does | Objects **appear / disappear only** (collider + renderer). No objects transform behaviour |
| Overlap rule | Switch is **blocked** if the player overlaps any object of the incoming phase — buzz SFX + red flash |
| Inactive phase | Shown as **ghost silhouettes** (transparent, non-solid) so players can plan jumps |
| Auto-cycle | Section 4 only: world flips on a fixed timer, **manual switch disabled** |

### Colour code

| Phase | Colour |
|---|---|
| Day | Gold / orange |
| Night | Blue / purple |
| Both | Neutral grey stone |

### Switch presentation

- Cross-fade over **0.25s**: directional light colour/intensity, skybox tint/exposure, fog colour.
- Audio cue: high chime → Day, low hum → Night.

## 3. Player & Camera

- **Starter Assets Third Person Controller** (`CharacterController`) with **Cinemachine** orbit camera.
- Additions: coyote time (~0.1s), jump buffer, slightly faster fall gravity, moving-platform support.
- **No sprint** — single fixed run speed so jump distances are consistent.

### Controls

| Action | Input |
|---|---|
| Move | WASD |
| Look / orbit | Mouse |
| Jump | Space |
| Phase switch | Right Mouse |
| Pause | Esc |

## 4. Fail State, Progression & Goals

- **3 lives for the entire run.**
- On death (void fall or kill surface): respawn at the **last checkpoint**, lose 1 life.
- At **0 lives: full game over** — restart from Section 1; timer, deaths and shards reset.
- **Checkpoints** always sit on **Both** platforms and save the **phase** active when touched; respawn restores that phase.
- **Collectibles:** 12 shards total, **3 per section**, each **Day-only or Night-only**. Score only.
- **End screen:** time, death count, shards collected (e.g. `Shards 7/12`).

## 5. Obstacle Toolkit

All pieces can be tagged Day / Night / Both.

- Static platforms, walls, kill surfaces
- Moving platforms (back and forth)
- Spinning kill bars
- Jump pads

## 6. Course Structure

One continuous course, ~8–10 minutes for a first-time player, 4 sections.

| # | Tier | Introduces | Test |
|---|---|---|---|
| 1 | Mossy Base | Switching on the ground, Day vs Night paths, ghosts, blocked-switch feedback | Easy, wide platforms, little void |
| 2 | Aqueducts | **Mid-air switching**, jump pads | Phase-locked shards on risky side paths |
| 3 | Clockwork | **Phased hazards** — switch to dodge as well as land | Hardest skill section |
| 4 | Summit | **Auto-cycle finale** (e.g. flip every 2s), manual switch disabled | Short (~60–90s) rhythm run to the sun/moon mechanism |

## 7. Art & Audio

- **Gameplay geometry:** primitives / ProBuilder with the 3 colour-code materials — readable and accurate collision.
- **Decoration, skybox, SFX, music:** free CC0 sources (Kenney, OpenGameArt, Pixabay).
- All sources listed in `CREDITS.md`. **Every imported asset must be committed.**

## 8. Team Ownership

Each member owns one system (days 1–2) and one section (days 3–5).

| Member | System | Section |
|---|---|---|
| P1 | **Phase core** — `WorldPhase` manager, `PhaseObject` (Day/Night/Both, ghost material, collider toggle), switch input, cooldown, overlap block, auto-cycle mode | 3 — Clockwork |
| P2 | **Player & camera** — Starter Assets setup, coyote time, jump buffer, moving-platform support, kill/respawn, checkpoints | 2 — Aqueducts |
| P3 | **Game flow & UI** — lives, game over, timer, death counter, shards, HUD, start screen, end screen | 1 — Mossy Base |
| P4 | **Look & feel** — light/sky/fog cross-fade, materials, ghost shader, SFX, music, obstacle prefabs, build pipeline | 4 — Summit |

## 9. Project Workflow

- **Scenes:** `Main.unity` (player, camera, managers, UI) additively loads `Section1.unity` … `Section4.unity`.
  - `Main.unity` owned by P1, changed only via PR.
  - Each section scene is edited **only by its owner**.
- **Shared obstacles are prefabs** so tuning applies everywhere.
- **Git:** one short-lived branch per person, merged into `main` at least daily.
- **Fresh-clone test** on another member's machine **every day from day 2**: clone → open → open `Main.unity` → Play must just work.
- **Build Settings:** `Main` + 4 section scenes listed.

## 10. Schedule

| Day | Goal | Done when |
|---|---|---|
| 1 | Systems skeleton | Starter Assets + Cinemachine installed and committed; run, jump, switch in a grey-box scene; `PhaseObject` + ghosts work; `Main` + section scenes pushed |
| 2 | Systems complete | Checkpoints, lives, respawn, HUD, cross-fade, 3 obstacle prefabs; first fresh-clone test + Windows build |
| 3 | Grey-box all sections | Full course playable end to end with plain cubes |
| 4 | **Playtest #1 + feature freeze** | Outsider plays, deaths noted; no new features after today |
| 5 | Tuning + art pass | Difficulty fixed, materials, dressing, shards placed |
| 6 | Audio, menus, polish + **Playtest #2** | Start/end screens, music/SFX, bug fixes |
| 7 | **Submit by midday** | Final fresh-clone test, Windows build zipped, README done |

**Cut rule:** if systems aren't done by end of day 2, **cut moving platforms first**.

## 11. Submission Checklist

- [ ] Repo opens cleanly in Unity 6000.3.8f1 from a fresh clone
- [ ] Opening `Main.unity` and pressing Play starts the game
- [ ] Windows standalone build zipped and attached
- [ ] `README.md` — Unity version, how to run, controls
- [ ] `CREDITS.md` — all third-party assets and their licences

## 12. Known Risks

- **3 lives with full game over is harsh** — watch playtest #1 closely; tune difficulty down if testers can't pass Section 2.
- **Moving platforms with `CharacterController`** need extra code — first thing cut if behind.
- **Unity scene merge conflicts** — avoided only if the scene-ownership rule is followed.
