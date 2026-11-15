# Sun Switch — Platform & Character Art

Owner: platforms, platform prefabs, phase materials, character visual.

## Folders

| Path | Owner | Contents |
| --- | --- | --- |
| `Assets/Prefabs/Platforms/` | Platform art | Prefabs level designers place (`Platform_Medium_Day`, …) |
| `Assets/Prefabs/Platforms/Base/` | Platform art | Base prefabs the variants inherit from — do not place these |
| `Assets/Art/Materials/Phase/` | Platform art | `M_Phase_Day/Night/Both`, `M_Ghost_Day/Night`, `M_Checkpoint_Beacon/Lit` |
| `Assets/Art/Shaders/` | Platform art | `SunSwitch/Storybook` and `SunSwitch/StorybookGhost` |
| `Assets/Prefabs/Obstacles/` | Look & feel | Obstacle prefabs level designers place (`KillBar_Day`, `JumpPad_Both`, `Shard_Night`, `KillPlane`, `SummitGoal`, …); `Base/` is not for placing |
| `Assets/Art/Materials/Obstacles/` | Look & feel | `M_Shard_Day/Night` |
| `Assets/Prefabs/UI/` | Look & feel | `HUD.prefab` (in-game HUD) |
| `Assets/Art/UI/` | Look & feel | HUD icon sprites (`HUD_Flame`, `HUD_Sun`, `HUD_Moon`, `HUD_Ring`, `HUD_Shard`) and `M_InkIris` (death wipe) |
| `Assets/Art/Meshes/` | Platform art | Real-size platform meshes (`Ruins/`) and obstacle meshes (`Obstacles/`) |
| `Assets/Art/Editor/` | Platform art | Platform tools (`Sun Switch > Art > …`) |
| `Assets/Art/Characters/Lightkeeper/` | Character art | Lightkeeper model (KayKit), lantern, animator controller |
| `Assets/Art/Materials/Character/` | Character art | `M_Lightkeeper`, `M_LanternGlow`, `M_LandingShadow` |
| `Assets/Art/Materials/Sky/` | Look & feel | `M_StorybookSky` |
| `Assets/Art/Scripts/` | Character art, look & feel | `LanternGlow`, `DayNightLook`, `DayNightDebugToggle`, `Shard`, `CheckpointLamp`, `CheckpointNotice`, `HintZone`, `HintPanel`, `PhaseFlipCue`, `LandingShadow`, `DeathRespawnFx`, `InkIris` (runtime). Obstacle and phase behaviour lives in `Assets/Scripts` (gameplay). |
| `Assets/Prefabs/Characters/` | Character art | `Lightkeeper.prefab` |
| `Assets/Scenes/Sandbox/Platforms_Sandbox.unity` | Platform art | Preview / test scene, not in Build Settings |
| `Assets/Scenes/Sandbox/Level_Greybox.unity` | Look & feel | The whole level plan grey-boxed and playable, not in Build Settings |
| `ArtSource/` (repo root, outside `Assets/`) | Platform art | Blender `.blend` sources; only exported `.fbx` goes into `Assets/` |

Only the owner edits these files. Ask for a change instead of editing them directly — this is what keeps prefab merges conflict-free.

## Prefab contract

```
Platform_Medium_Day      root — BoxCollider + PhaseObject, never scaled
└─ Visual                mesh + material; replaced with final art later
```

- 1 m grid. Platform pivot is the **centre of the top surface**, so y = the height the player stands on. Walls pivot at the bottom centre.
- Collider lives on the root (or a `Collision` child for round shapes), never on `Visual`. Final art may be swapped inside `Visual` without changing collision.
- Final art keeps its top face flush with the collider top; decorative edges may overhang at most ~5 cm.
- Phase variants differ only in material and the root `PhaseObject`'s tag and ghost material (`M_Ghost_Day` / `M_Ghost_Night`; Both variants have none). The base prefab's `PhaseObject` lists every renderer and collider, so the whole prefab turns ghost together.

| Prefab | Size (w × h × d, m) |
| --- | --- |
| Platform_Small | 2 × 0.5 × 2 |
| Platform_Medium | 4 × 0.5 × 4 |
| Platform_Wide | 6 × 0.5 × 6 |
| Platform_Bridge | 2 × 0.4 × 8 |
| Platform_Pillar | ⌀2 × 1 |
| Platform_Moving | 3 × 0.4 × 3, with `MovingPlatform` and `PointA` / `PointB` ends (default 6 m along +x; move them per instance) |
| Wall | 4 × 4 × 0.5 |
| Checkpoint | 4 × 0.5 × 4, Both only, with beacon and `Checkpoint` (see below) |

**Checkpoint** (`Assets/Prefabs/Platforms/Base/Checkpoint.prefab`, placed directly):

```
Checkpoint     Both platform, BoxCollider, CheckpointLamp
├─ Beacon      cream pole
├─ Lamp        the lamp on top of the pole (always there; CheckpointLamp drives its glow)
├─ Lit         point light; off until the checkpoint is reached
├─ Flare/Beam  column of light for the reached burst (saved off)
├─ Ring        ring that rolls across the platform in the burst (saved off)
├─ Spawn       where the player respawns (platform centre, facing +z); move or turn per instance
└─ Trigger     trigger box above the platform + Checkpoint (Spawn Point = Spawn, Active Indicator = Lit)
```

Tick *Is Start* on `Trigger`'s `Checkpoint` for the very first one in Section 1.

**The lamp tells the player where they stand** (`CheckpointLamp`, art-side; it only reads the gameplay scripts):

| State | Looks like |
| --- | --- |
| Not reached yet | A faint ember that pulses slowly, so a checkpoint ahead is visible as "a lamp to light" |
| Reached | About 1 s: the light flashes, the lamp pops, a beam shoots 7 m up, a ring rolls across the platform and sparks rise. The HUD shows "Checkpoint" under the timer (`CheckpointNotice`). Not shown for the start checkpoint |
| Current (the respawn point) | Bright, breathing gently, lighting the platform |
| Reached earlier | Still lit, but dimmer, so the current one stands out |
| Respawned at | A short flash when the player comes back to it after a death |

The colour follows the phase like the Lightkeeper's lantern: gold by Day, blue by Night. Reaching an older checkpoint again makes it current and plays the burst again. Tuning (brightness per state, burst length, beam height, ring size, spark count) is on `CheckpointLamp`. `Update Platform Prefabs` recreates it with the script's defaults.

## Colour code

| Phase | Material | Colour |
| --- | --- | --- |
| Day | `M_Phase_Day` | `#E9B64F` sun gold |
| Night | `M_Phase_Night` | `#6670B8` night indigo |
| Both | `M_Phase_Both` | `#9A958C` warm grey stone |
| Ghost (inactive phase) | `M_Ghost_Day` / `M_Ghost_Night` | same hue, 30 % opacity, no shadows |

## Storybook look

All phase materials use `SunSwitch/Storybook` (`Assets/Art/Shaders/`): two-band painted lighting with tinted shadows, world-space paper grain and paint blotches, a cream rim highlight, and an ink outline. Ghost materials use `SunSwitch/StorybookGhost`: a faint transparent fill with bright edges and diagonal hatch lines, no shadows.

- Day/night lighting changes come from the scene's directional light colour and intensity; the shader follows them, no material swaps needed.
- Edge ink and outlines need data baked into the mesh by `MeshInk` (edge distances in UV1/UV2, smoothed normals in UV3). Grey-box meshes and the ruin models get it automatically. Any other mesh using this shader renders without edge ink and with an outline hull.
- `Sun Switch > Art > Update Platform Prefabs` refreshes materials, meshes and gameplay components (`PhaseObject`, `MovingPlatform`, `Checkpoint`) inside the existing prefabs and variants, and is safe to run at any time. `Build Grey-box Platforms` replaces the prefabs and should no longer be used once platforms are placed in scenes.

## Ruin models

The final platform visuals are floating sky-ruins: flat paved tops (exactly at the collider top), chipped corners inside the footprint, stepped masonry sides, and a jagged rock hanging underneath. Pillar is a broken column; Wall is a crumbling brick wall.

- Source: `ArtSource/ruin_platforms.py`, a Blender 5 script. Every shape comes from a fixed seed, so re-running it gives identical models. Run it in Blender's Scripting tab (or through the Blender MCP), call `build_all()`, then `export_all("<repo>/Assets/Art/Meshes/Ruins")`.
- Unity import: `RuinMeshPostprocessor` sets the import settings and bakes the ink data (`MeshInk`) automatically. Nothing to configure by hand.
- Prefabs: `Update Platform Prefabs` puts the ruin model in each `Visual` slot when one exists, otherwise the grey-box mesh. Colliders are unchanged.
- Nothing visual sticks out past the collider footprint. The hanging rock is decoration only and has no collider.
- Vertex colours shade the parts (paving, grout, sides, rock); the phase colour multiplies on top.

## Day / Night presentation

GDD §2 "Switch presentation": on every phase switch the world cross-fades over **0.25 s**.

| | Day | Night |
| --- | --- | --- |
| Sun / moon light | warm cream, 1.15, from (50°, −30°) | moon blue, 0.55, from (60°, −10°) |
| Sky (`SunSwitch/StorybookSky`) | sky blue → cream horizon, cloud wisps | night indigo → moon-blue horizon, stars |
| Fog (exp²) | pale haze `#DBE3E6`, 0.006 | deep indigo `#333B6B`, 0.008 |
| Ambient (sky / equator / ground) | cool blue / warm grey / earth | indigo shades |

- **`DayNightLook`** (one per scene) drives all of it. In Play mode it follows `WorldPhase` by itself: each `OnPhaseChanged` fades to the new phase, and a change with `WorldPhase.LastChangeInstant` (respawn, `ForceSet(phase, instant: true)`) snaps without a fade. Nothing needs to call it. Without a `WorldPhase` (art-only scenes), `DayNightLook.Instance.SetDay(isDay)` still works. Anything else that follows the phase listens to the static `DayNightLook.Changed` event. The Lightkeeper's `LanternGlow` already does.
- All values are in the component's Inspector (Day / Night blocks). Toggle *Is Day* to preview Night in the editor.
- The sky blends on the global shader float `_SunSwitchNight` (0 = Day, 1 = Night), so the material asset never changes at runtime. Other shaders can read it too.
- Ambient uses a three-colour probe set by script (sky ambient can't be re-baked every frame), so scenes don't need baked lighting.
- **Setup:** `Sun Switch > Art > Add Day-Night Look To Open Scene` adds the sky and the `DayNightLook` (linked to the scene's directional light). In sandbox scenes only it also adds `DayNightDebugToggle` and a `WorldPhase`, so pressing **T** in Play mode switches the whole world: platforms, ghosts, hazards and sky. Safe to run again. Run it on `Main.unity` once it exists (its `WorldPhase` comes from the Managers object). Section scenes load additively, so they don't need their own.
- Fog stays on in both phases. URP strips fog shader variants from builds when no scene uses fog, so keep the setup in a scene that's in Build Settings.

## Obstacles

GDD §5 obstacle toolkit. Every obstacle comes in `_Day`, `_Night` and `_Both` variants, except shards, which are always Day-only or Night-only (GDD §4). The behaviour is the gameplay scripts in `Assets/Scripts`; the prefabs only wire them up.

| Prefab | Size | What it does | Scripts |
| --- | --- | --- | --- |
| `KillBar_X` | post ⌀0.8 m; bar 5.5 m long at 0.75 m, 90°/s | Striped bar spinning on a post. Clear it by jumping. Fits on `Platform_Wide`. | `SpinningBar` + `KillSurface` on `Arm` |
| `JumpPad_X` | 2 × 0.17 × 2 m | Carved stone with a rune disc; squashes when used | `JumpPad` on `Trigger` |
| `KillSurface_S_X` / `KillSurface_M_X` | 2 × 2 m / 4 × 4 m, spikes to 0.44 m | Bed of red-brown ink-thorn spikes | `KillSurface` on `Kill` |
| `Shard_Day` / `Shard_Night` | ~0.8 m crystal, 0.6 m pickup radius | Floats, bobs and turns; hides when picked up | `Shard` (art) |
| `KillPlane` | invisible 40 × 40 m trigger, top at the pivot | Catches falls under a section before the void line far below | `KillSurface` |
| `SummitGoal` | 1.8 m stone pillar with a glowing crown; 3 × 2.5 × 3 m trigger | The finish. Touching it switches on the ending beat (a 12 m column of light) and wins the run | `SummitGoal` on `Trigger` |

- **Phases:** every root has a `PhaseObject` (tag and ghost material set by the variant). In the other phase the obstacle shows as a ghost and its colliders, triggers included, are off, so a ghost hazard can't kill, a ghost pad can't launch and a ghost shard can't be picked up. Switching a hazard on around the player kills them (`KillSurface` checks overlaps too).
- **Placing:** obstacles sit *on* a platform. Their pivot is the bottom centre, so put them at the platform's top y. Shards pivot at their centre and float (about 1.2 m above the surface is a comfortable grab). Put a `KillPlane` a few metres under each section; scale its x / z to fit.
- **Tuning per instance:** `SpinningBar` on `KillBar/Arm` (degrees per second, axis, start angle, so neighbouring bars can be out of step); `JumpPad` on `JumpPad/Trigger` (launch speed, default 14 m/s ≈ 6.5 m up at Starter Assets gravity; direction for angled pads).
- **Hazard look:** the danger parts use darker red-brown vertex colours under the phase colour, so a kill bar or spike bed reads as dangerous in gold, indigo and grey.
- **Source:** `ArtSource/obstacles.py` (Blender 5): `blender -b --factory-startup --python ArtSource/obstacles.py -- "<repo>/Assets/Art/Meshes/Obstacles"`. `RuinMeshPostprocessor` bakes the ink on import, the same as the ruins.
- **Prefabs:** `Sun Switch > Art > Build Obstacle Prefabs` creates or updates them in place (same GUIDs, so placed instances keep working). `Sun Switch > Art > Add Obstacle Row To Sandbox` puts one of each in `Platforms_Sandbox` at z = 36.

### Shards and game flow

`Shard` stays an art-side script because the game flow (`ShardTracker`) is built on it:

- `Shard.Collected` (once per shard), `Shard.ResetAll()` (brings every shard back and raises `Shard.ShardsReset`), `IsCollected`. These names stay as they are.
- A shard finds the player by its `PlayerDeath` component, like the gameplay scripts. The `Player` tag is still accepted.
- The pickup trigger belongs to the `PhaseObject`; `Shard` only hides and shows the crystal. That's why `ResetAll()` can't make a ghost shard collectable.

## Playable sandbox

`Sun Switch > Art > Make Sandbox Playable` turns `Platforms_Sandbox` into a small test level, so the art can be played with the real controller before `Main.unity` exists. Safe to run again: it rebuilds the course and reuses the rest.

- **Player:** the scene's Lightkeeper gets `CharacterController`, `PlayerInput` (Starter Assets actions, *Send Messages*), `StarterAssetsInputs`, `LightkeeperController`, `PlayerDeath`, `PhaseSwitchInput`, `PhaseOverlapChecker` and `PlatformRider`, plus a `PlayerCameraRoot` child at 1.375 m. It's tagged `Player` and sits on the *Ignore Raycast* layer, so the ground check doesn't hit its own capsule.
- **Camera:** the Main Camera gets a `CinemachineBrain`; the Starter Assets `PlayerFollowCamera` follows `PlayerCameraRoot`.
- **Managers:** `Sandbox Managers` has `RespawnManager` (with *Respawn On Death* off), `GameManager`, `LivesSystem`, `RunTimer` and `ShardTracker`. `ShardTracker`'s total is the number of shards in the sandbox, not the tower's 12. `WorldPhase` sits on the `DayNightLook` object.
- **Game flow:** Play opens on the storybook Start screen (`MenuRoot` with `GameMenuFlow`). Then: 3 lives, a run timer, shards, Esc to pause, Game Over after the third death, and the Victory screen at the summit. An `EventSystem` (Input System UI module) drives the menu buttons. Untick *Start In Menu* on `GameManager` to skip the Start screen while testing art.
- **HUD:** `HUD.prefab` (see below).
- **Controls:** WASD / mouse to move and look, Space to jump, **Right Mouse** to switch, **Esc** to pause, **T** to force a switch (ignores cooldown and overlap).
- **Test Course** (along +x at z = −16): start checkpoint → Both hop → Day and Night stepping stones (switch mid-run) → kill bar → jump pad up to a Night-only ledge 5 m higher (switch in the air) with a Night shard → checkpoint → moving platform → Day landing → Night spikes with a Day shard above them → Night wall to pass in Day → one more hop to the `SummitGoal`. A `KillPlane` under it catches falls.

This is a test rig, not the game's player: the player prefab, follow camera and managers for `Main.unity` belong to P2 and P1. The component list above is the same one their scene checklist asks for, so it can be copied. `Main.unity` is made by `Sun Switch > Build Game Scenes` (see Game scenes below).

## Level grey-box

`Sun Switch > Art > Build Level Grey-box` places the Sun Switch Level Plan into `Assets/Scenes/Sandbox/Level_Greybox.unity`: the four sections around the tower, 12 shards, 6 checkpoints, a kill plane per section, the Summit `AutoCycleZone`, the `SummitGoal` and the instruction hints. It adds the same player, camera, managers, menu and HUD as the playable sandbox, with the player on CP1. Safe to run again: the section roots are rebuilt and the rest is reused.

- **One root per section** (`Section 1 - Mossy Base` … `Section 4 - Summit`), so each owner can copy theirs into `SectionN.unity` once those scenes exist. World positions are already final, so pasting keeps them.
- **The positions live in `LevelGreyboxBuilder.cs`**, one line per prefab with a comment on what it's for. Change a beat there, rebuild, and keep the plan in step.
- **Handoffs:** the corner checkpoints (CP2, CP3, CP4) are where sections meet. Move beats inside a section freely, but keep those fixed.
- **Checked:** a route bot played the whole grey-box in a player build with the real controls: every hop, all 12 shards, each checkpoint's respawn and facing, the kill planes and the win. 90/90 checks, four runs in a row.
- **Instructions:** six `HintZone` boxes (`Hint - …` under each section root) show a line at the bottom of the HUD while the player stands in them. Words in [brackets] are drawn as keys.

  | Hint | Where | Says |
  |---|---|---|
  | Controls | CP1 | [WASD] move, [Space] jump, [Shift] sprint, [Mouse] look around |
  | Switch | end of the first Day platform and the switch station | [Right Mouse] switch Day / Night. Some platforms only exist at Night. |
  | Ghost | the Day platform before the ghost wall | A Day wall is only a ghost at Night. Switch, then walk through it. |
  | Mid-air Switch | CP2 and the next platform | Switch in mid-air: jump first, then [Right Mouse] before you land. |
  | Phased Hazards | CP3b, before the kill bars | Hazards follow the phase too: a Day bar is only a ghost at Night. |
  | Auto-cycle | CP4 | Here the world flips by itself. Wait on safe ground, and jump when the platforms blink. |

  A hint is done once the player walks out of its box alive, and doesn't come back. Falling out of the bottom or dying doesn't count, so the lesson is still there after the respawn. Edit the text and boxes in `LevelGreyboxBuilder.cs`; select a hint to see its box as a gold gizmo.
- **Summit rhythm (Section 4):** in the auto-cycle, a hop onto a platform that isn't there yet only works if you take off just before the flip and land just after it, a window of about 0.5-0.7 s. So every phased platform there sits next to a Both platform where the player can stand, watch a flip or two and catch the beat. Only two hops go straight from one phased platform to another (Night 18.5 > Day 23 and Night 30 > Day 33.5 on the stair), and each comes right after a rest.
- **Flip warning:** `PhaseFlipCue` on the `AutoCycleZone` shows each flip coming for the last 0.6 s: platforms about to vanish blink faster and faster, ghosts of platforms about to appear fill in, and the Lightkeeper's lantern pulses on the same beat. "It's blinking" means "jump now". It only uses property blocks and drops them at the flip, so `PhaseObject` keeps control of materials and colliders.
- **Kill bars:** each arm reaches about 3 m from the post. On a 6 × 6 m platform, only the corners are outside that reach, so aim landings at a corner. Crossing behind an arm, moving the same way it does, is the easy line.

## Game scenes

`Sun Switch > Build Game Scenes` makes the scenes the game ships with, from the grey-box layout:

| Scene | Holds |
|---|---|
| `Assets/Scenes/Main.unity` | Day/Night look and sun, `Managers` (`WorldPhase`, `RespawnManager`, `GameManager`, `LivesSystem`, `RunTimer`, `ShardTracker` set to 12, `SectionLoader`), the Lightkeeper on CP1, the follow camera, `MenuRoot` with `GameMenuFlow`, the HUD and an `EventSystem` |
| `Assets/Scenes/Sections/Section1-4.unity` | One grey-box section root each: platforms, shards, checkpoints, hazards and hints. `Section4` also has the Summit `AutoCycleZone` and the `SummitGoal` |

- **How it runs:** Build Settings lists `Main` first, then `Section1` to `Section4`. Press Play in `Main`; `SectionLoader` loads the four sections into it (about 0.1 s) and `GameManager` waits for them before showing the Start screen. `Main` stays the active scene, so its lighting and fog are the ones you see.
- **Editing a section:** open `Main`, then drag `SectionN` into the Hierarchy to open it alongside. Play still works: sections that are already open aren't loaded twice.
- **Owners:** `Main` is P1's; `Section1` P3, `Section2` P2, `Section3` P1, `Section4` P4 (GDD). Once a section scene exists, running the builder again never touches it, so owners can replace the grey-box pieces freely. To replace one with the layout in `LevelGreyboxBuilder.cs`, use `Sun Switch > Rebuild Section Scene > Section N` (it asks first, and keeps the scene asset and its Build Settings entry). `Main` gets missing pieces added and keeps the rest.
- **Sandboxes stay test scenes:** `Platforms_Sandbox` and `Level_Greybox` keep the T-key debug switch and aren't in Build Settings. `StartGame.unity` and `Assets/UI/Scenes/MenuPreview.unity` are the menu team's preview scenes.
- **Checked:** a development player build of the five scenes loads them all, keeps one `WorldPhase` and one `DayNightLook`, counts 6 checkpoints, 12 shards and 6 hints, starts on CP1 from the Start screen, respawns at a checkpoint in another section, and wins at the `SummitGoal` in `Section4`.

## HUD

`Assets/Prefabs/UI/HUD.prefab`, built by `Sun Switch > Art > Build HUD Prefab` (safe to run again; sprites and prefab are rebuilt in place). It's in the menu's storybook style (`Docs/UI_ART_STYLE.md`): ink on parchment plates, sun gold and moon blue accents. The menu art direction leaves the gameplay HUD out of its scope, so it lives here.

- **Lives** (top left): three lantern flames. A lost one greys out and bounces.
- **Run** (top centre): `PhaseCooldownIcon` (sun or moon inside a gold ring that refills over the switch cooldown, shakes red when a switch is blocked) and the run timer. The line under the timer is a `PhaseThemedGraphic`: gold by Day, blue by Night.
- **Shards** (top right): crystal and `x/total`.
- **Checkpoint notice** (under the timer): "Checkpoint" with a lamp flame, for about 2 s when a new checkpoint is reached (`CheckpointNotice`).
- **Ink wipe** (full screen, behind the plates): the death / respawn iris (`InkIris`, `M_InkIris`), so the life just lost stays visible over the ink.
- **Hint** (bottom centre): a parchment strip with the instruction for the `HintZone` the player is in. When a switch is blocked by a ghost, it says "Can't switch while inside a ghost. Step out of it first." for 2.5 s (`HintPanel`). Cross-fades between messages.
- `HUDController` on the root (with a `CanvasGroup`) shows it while playing or paused. The canvas is an overlay with sort order −10, so the menu draws over it.
- Icons are baked sprites in `Assets/Art/UI` (ink outline and white fill), tinted by the Image colour. The text uses Unity's built-in font, like the menu, until the licensed display font arrives.

## Character: the Lightkeeper

A small hooded figure in a parchment-cream hood and cape with a muted-red tunic, carrying a lantern on their back. The camera mostly sees the back, so the lantern is the part that shows the phase: it glows sun gold in Day and moon blue in Night. Cream and red stay readable on gold, indigo and grey platforms.

- **Model:** `KayKit/RogueHooded.fbx` from the free [KayKit Character Pack: Adventurers](https://kaylousberg.itch.io/kaykit-adventurers) (CC0), used unmodified. `LightkeeperPostprocessor` does everything on import: Humanoid avatar, scale to ~1.8 m, keeps only the clips below, removes the built-in weapons, bakes the texture into recoloured vertex colours and bakes the ink data. Nothing to set by hand.
- **Facing and rig fixes:** KayKit faces −Z in Unity, so the import turns the `Rig` node 180° (the character faces +Z like any Unity controller expects), and the clips use *Root Transform Rotation: Body Orientation*. Unity's automatic Humanoid mapping skips KayKit's `chest` bone; the import adds `Chest = chest` and reimports once.
- **Colours:** the recolour table is at the top of `LightkeeperPostprocessor` (texture swatch → colour). Change a colour there and the model reimports.
- **Clips:** `Idle`, `Walking_A`, `Running_A`, `Jump_Start`, `Jump_Idle`, `Jump_Land`, `Death_A`, `Cheer`. All in place (no root motion). The Humanoid avatar also accepts other humanoid animations (e.g. Starter Assets).
- **Lantern:** `ArtSource/lantern.py` (Blender 5) builds and exports `Lantern.fbx`, the same way as the ruins: `blender -b --factory-startup --python ArtSource/lantern.py -- "<repo>/Assets/Art/Characters/Lightkeeper"`.
- **Prefab:** `Sun Switch > Art > Build Lightkeeper` builds `Assets/Prefabs/Characters/Lightkeeper.prefab` and the materials, and is safe to run again. It creates `AC_Lightkeeper` only if it doesn't exist yet, so gameplay edits to the controller are kept.

```
Lightkeeper        model root: Animator (Humanoid, AC_Lightkeeper), faces +Z, pivot at the feet
├─ … chest bone
│    └─ Lantern    LanternGlow + Lantern_Light (point light, no shadows)
├─ Landing Shadow  LandingShadow: ink spot on the ground straight below
└─ Death FX        DeathRespawnFx: ink burst on a death, sparkles on the respawn
```

- **Death and respawn:** on a death the Lightkeeper bursts into ink scraps and disappears, and the HUD's ink iris (`InkIris`) closes in on the spot. It stays shut while the camera jumps back to the checkpoint, which hides the cut. On the respawn the iris opens on the player, sparkles rise around them in the phase colour, and the checkpoint's lamp gives a short flash. On the last life the ink stays shut under the Game Over screen; the next run, or the Start screen, opens it again.
- **Updating the prefab:** `Sun Switch > Art > Update Lightkeeper Effects` adds or refreshes only the landing shadow and the death / respawn effect on the existing prefab, so the scenes' player instances keep their overrides. `Build Lightkeeper` includes both.
- **Landing shadow:** a soft ink spot (`SunSwitch/LandingShadow`, drawn procedurally, no texture) on the solid ground straight below the Lightkeeper, so players can see where a jump will land. It gets a little smaller and fainter with height (fully at 8 m), and is darker at Night so it still reads on blue platforms. It ignores triggers, and ghost platforms have no collision, so over a ghost or the void there's no spot: no spot, no ground. While the character is dying the spot is hidden.

### Hand-off to player & camera (P2)

- Put `CharacterController`, `LightkeeperController` (+ Starter Assets input), `PlayerDeath`, `PhaseOverlapChecker` and `PlatformRider` on the prefab root, next to the `Animator`. `LightkeeperController` looks for the `Animator` on its own GameObject. Keep the `Player` tag.
- `AC_Lightkeeper` uses the Starter Assets parameter names: `Speed`, `MotionSpeed`, `Grounded`, `Jump`, `FreeFall`. The blend tree thresholds are walk 2 and run 5.335 (the Starter Assets defaults). Retune them to the final run speed.
- The KayKit clips have no footstep / land animation events; `ThirdPersonController` works without them.

### Hand-off to phase core (P1)

- Nothing to wire for the lantern: it follows `DayNightLook`, which follows `WorldPhase` (see *Day / Night presentation*).

### Ink on skinned meshes

`MeshInk.Bake(mesh, skinned: true)` keeps bone weights and bind poses, and stores the smoothed outline normal in the tangent (UV3.w = 1 tells the shader), because skinning bends tangents with the bones but not UV3. Each KayKit body part follows one bone, so the baked edge distances stay correct while animating.
