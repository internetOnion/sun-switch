# Sun Switch — Platform & Character Art

Owner: platforms, platform prefabs, phase materials, character visual.

## Folders

| Path | Owner | Contents |
| --- | --- | --- |
| `Assets/Prefabs/Platforms/` | Platform art | Prefabs level designers place (`Platform_Medium_Day`, …) |
| `Assets/Prefabs/Platforms/Base/` | Platform art | Base prefabs the variants inherit from — do not place these |
| `Assets/Art/Materials/Phase/` | Platform art | `M_Phase_Day/Night/Both`, `M_Ghost_Day/Night`, `M_Checkpoint_Beacon/Lit` |
| `Assets/Art/Shaders/` | Platform art | `SunSwitch/Storybook` and `SunSwitch/StorybookGhost` |
| `Assets/Prefabs/Obstacles/` | Look & feel | Obstacle prefabs level designers place (`KillBar_Day`, `JumpPad_Both`, `Shard_Night`, `KillPlane`, …); `Base/` is not for placing |
| `Assets/Art/Materials/Obstacles/` | Look & feel | `M_Shard_Day/Night` |
| `Assets/Art/Meshes/` | Platform art | Real-size platform meshes (`Ruins/`) and obstacle meshes (`Obstacles/`) |
| `Assets/Art/Editor/` | Platform art | Platform tools (`Sun Switch > Art > …`) |
| `Assets/Art/Characters/Lightkeeper/` | Character art | Lightkeeper model (KayKit), lantern, animator controller |
| `Assets/Art/Materials/Character/` | Character art | `M_Lightkeeper`, `M_LanternGlow` |
| `Assets/Art/Materials/Sky/` | Look & feel | `M_StorybookSky` |
| `Assets/Art/Scripts/` | Character art, look & feel | `LanternGlow`, `DayNightLook`, `DayNightDebugToggle`, `Shard` (runtime). Obstacle and phase behaviour lives in `Assets/Scripts` (gameplay). |
| `Assets/Prefabs/Characters/` | Character art | `Lightkeeper.prefab` |
| `Assets/Scenes/Sandbox/Platforms_Sandbox.unity` | Platform art | Preview / test scene, not in Build Settings |
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
Checkpoint     Both platform, BoxCollider
├─ Beacon      cream pole
├─ Lit         gold glow cap + point light; off until the checkpoint is reached
├─ Spawn       where the player respawns (platform centre, facing +z); move or turn per instance
└─ Trigger     trigger box above the platform + Checkpoint (Spawn Point = Spawn, Active Indicator = Lit)
```

Tick *Is Start* on `Trigger`'s `Checkpoint` for the very first one in Section 1.

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
└─ … chest bone
     └─ Lantern    LanternGlow + Lantern_Light (point light, no shadows)
```

### Hand-off to player & camera (P2)

- Put `CharacterController`, `LightkeeperController` (+ Starter Assets input), `PlayerDeath`, `PhaseOverlapChecker` and `PlatformRider` on the prefab root, next to the `Animator`. `LightkeeperController` looks for the `Animator` on its own GameObject. Keep the `Player` tag.
- `AC_Lightkeeper` uses the Starter Assets parameter names: `Speed`, `MotionSpeed`, `Grounded`, `Jump`, `FreeFall`. The blend tree thresholds are walk 2 and run 5.335 (the Starter Assets defaults). Retune them to the final run speed.
- The KayKit clips have no footstep / land animation events; `ThirdPersonController` works without them.

### Hand-off to phase core (P1)

- Nothing to wire for the lantern: it follows `DayNightLook`, which follows `WorldPhase` (see *Day / Night presentation*).

### Ink on skinned meshes

`MeshInk.Bake(mesh, skinned: true)` keeps bone weights and bind poses, and stores the smoothed outline normal in the tangent (UV3.w = 1 tells the shader), because skinning bends tangents with the bones but not UV3. Each KayKit body part follows one bone, so the baked edge distances stay correct while animating.
