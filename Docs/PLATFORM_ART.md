# Sun Switch — Platform & Character Art

Owner: platforms, platform prefabs, phase materials, character visual.

## Folders

| Path | Owner | Contents |
| --- | --- | --- |
| `Assets/Prefabs/Platforms/` | Platform art | Prefabs level designers place (`Platform_Medium_Day`, …) |
| `Assets/Prefabs/Platforms/Base/` | Platform art | Base prefabs the variants inherit from — do not place these |
| `Assets/Art/Materials/Phase/` | Platform art | `M_Phase_Day/Night/Both`, `M_Ghost_Day/Night` |
| `Assets/Art/Shaders/` | Platform art | `SunSwitch/Storybook` and `SunSwitch/StorybookGhost` |
| `Assets/Prefabs/Obstacles/` | Look & feel | Obstacle prefabs level designers place (`KillBar_Day`, `JumpPad_Both`, `Shard_Night`, …); `Base/` is not for placing |
| `Assets/Art/Materials/Obstacles/` | Look & feel | `M_Shard_Day/Night` |
| `Assets/Art/Meshes/` | Platform art | Real-size platform meshes (`Ruins/`) and obstacle meshes (`Obstacles/`) |
| `Assets/Art/Editor/` | Platform art | Platform tools (`Sun Switch > Art > …`) |
| `Assets/Art/Characters/Lightkeeper/` | Character art | Lightkeeper model (KayKit), lantern, animator controller |
| `Assets/Art/Materials/Character/` | Character art | `M_Lightkeeper`, `M_LanternGlow` |
| `Assets/Art/Materials/Sky/` | Look & feel | `M_StorybookSky` |
| `Assets/Art/Scripts/` | Character art, look & feel | `LanternGlow`, `DayNightLook`, `DayNightDebugToggle`, `Spinner`, `KillZone`, `JumpPad`, `Shard` (runtime) |
| `Assets/Prefabs/Characters/` | Character art | `Lightkeeper.prefab` |
| `Assets/Scenes/Sandbox/Platforms_Sandbox.unity` | Platform art | Preview / test scene, not in Build Settings |
| `ArtSource/` (repo root, outside `Assets/`) | Platform art | Blender `.blend` sources; only exported `.fbx` goes into `Assets/` |

Only the owner edits these files. Ask for a change instead of editing them directly — this is what keeps prefab merges conflict-free.

## Prefab contract

```
Platform_Medium_Day      root — BoxCollider, phase component goes here, never scaled
└─ Visual                mesh + material; replaced with final art later
```

- 1 m grid. Platform pivot is the **centre of the top surface**, so y = the height the player stands on. Walls pivot at the bottom centre.
- Collider lives on the root (or a `Collision` child for round shapes), never on `Visual`. Final art may be swapped inside `Visual` without changing collision.
- Final art keeps its top face flush with the collider top; decorative edges may overhang at most ~5 cm.
- Phase variants differ only in material (and, once it exists, the phase setting on the root component).

| Prefab | Size (w × h × d, m) |
| --- | --- |
| Platform_Small | 2 × 0.5 × 2 |
| Platform_Medium | 4 × 0.5 × 4 |
| Platform_Wide | 6 × 0.5 × 6 |
| Platform_Bridge | 2 × 0.4 × 8 |
| Platform_Pillar | ⌀2 × 1 |
| Platform_Moving | 3 × 0.4 × 3 (movement script added by gameplay logic) |
| Wall | 4 × 4 × 0.5 |
| Checkpoint | 4 × 0.5 × 4, Both only, with beacon |

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
- `Sun Switch > Art > Apply Storybook Look` refreshes materials and meshes inside the existing prefabs and is safe to run at any time. `Build Grey-box Platforms` replaces the prefabs and should no longer be used once platforms are placed in scenes.

## Ruin models

The final platform visuals are floating sky-ruins: flat paved tops (exactly at the collider top), chipped corners inside the footprint, stepped masonry sides, and a jagged rock hanging underneath. Pillar is a broken column; Wall is a crumbling brick wall.

- Source: `ArtSource/ruin_platforms.py`, a Blender 5 script. Every shape comes from a fixed seed, so re-running it gives identical models. Run it in Blender's Scripting tab (or through the Blender MCP), call `build_all()`, then `export_all("<repo>/Assets/Art/Meshes/Ruins")`.
- Unity import: `RuinMeshPostprocessor` sets the import settings and bakes the ink data (`MeshInk`) automatically. Nothing to configure by hand.
- Prefabs: `Apply Storybook Look` puts the ruin model in each `Visual` slot when one exists, otherwise the grey-box mesh. Colliders are unchanged.
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

- **`DayNightLook`** (one per scene) drives all of it. The phase system calls `DayNightLook.Instance.SetDay(isDay)` when the world switches; `SetDay(isDay, instant: true)` skips the fade (respawn, scene load). Anything else that follows the phase listens to the static `DayNightLook.Changed` event. The Lightkeeper's `LanternGlow` already does.
- All values are in the component's Inspector (Day / Night blocks). Toggle *Is Day* to preview Night in the editor.
- The sky blends on the global shader float `_SunSwitchNight` (0 = Day, 1 = Night), so the material asset never changes at runtime. Other shaders can read it too.
- Ambient uses a three-colour probe set by script (sky ambient can't be re-baked every frame), so scenes don't need baked lighting.
- **Setup:** `Sun Switch > Art > Add Day-Night Look To Open Scene` adds the sky, the `DayNightLook` (linked to the scene's directional light) and, in sandbox scenes only, `DayNightDebugToggle` (press **T** in Play mode). Safe to run again. Run it on `Main.unity` once it exists. Section scenes load additively, so they don't need their own.
- Fog stays on in both phases. URP strips fog shader variants from builds when no scene uses fog, so keep the setup in a scene that's in Build Settings.

## Obstacles

GDD §5 obstacle toolkit. Every obstacle comes in `_Day`, `_Night` and `_Both` variants (phase colour only), except shards, which are always Day-only or Night-only (GDD §4).

| Prefab | Size | What it does | Hook |
| --- | --- | --- | --- |
| `KillBar_X` | post ⌀0.8 m; bar 5.5 m long at 0.75 m, 90°/s | Striped bar spinning on a post. Clear it by jumping. Fits on `Platform_Wide`. | `KillZone.PlayerKilled` |
| `JumpPad_X` | 2 × 0.17 × 2 m | Carved stone with a rune disc; squashes when used | `JumpPad.PlayerLaunched` |
| `KillSurface_S_X` / `KillSurface_M_X` | 2 × 2 m / 4 × 4 m, spikes to 0.44 m | Bed of red-brown ink-thorn spikes | `KillZone.PlayerKilled` |
| `Shard_Day` / `Shard_Night` | ~0.8 m crystal, 0.6 m pickup radius | Floats, bobs and turns; hides when picked up | `Shard.Collected` |

- **Placing:** obstacles sit *on* a platform. Their pivot is the bottom centre, so put them at the platform's top y. Shards pivot at their centre and float (about 1.2 m above the surface is a comfortable grab).
- **Tuning per instance:** `Spinner` on `KillBar/Arm` (degrees per second, start angle, so neighbouring bars can be out of step); `JumpPad` on `JumpPad/Trigger` (*Launch Height* in metres, default 5).
- **Hazard look:** the danger parts use darker red-brown vertex colours under the phase colour, so a kill bar or spike bed reads as dangerous in gold, indigo and grey.
- **Source:** `ArtSource/obstacles.py` (Blender 5): `blender -b --factory-startup --python ArtSource/obstacles.py -- "<repo>/Assets/Art/Meshes/Obstacles"`. `RuinMeshPostprocessor` bakes the ink on import, the same as the ruins.
- **Prefabs:** `Sun Switch > Art > Build Obstacle Prefabs` creates or updates them in place (same GUIDs, so placed instances keep working). `Sun Switch > Art > Add Obstacle Row To Sandbox` puts one of each in `Platforms_Sandbox` at z = 36.

### Hand-offs

The obstacles only *report* what happened, through static C# events. The owners of each system decide what it means. The player must be tagged **`Player`** (Unity's built-in tag). A plain `CharacterController` is enough; no Rigidbody is needed on the player.

- **P2 (player):** subscribe to `KillZone.PlayerKilled` → death / respawn / lose a life. Subscribe to `JumpPad.PlayerLaunched` → set the vertical velocity to `pad.LaunchSpeed(Gravity)`. With the Starter Assets gravity of −15 and the default 5 m, that's 12.25 m/s.
- **P3 (score):** subscribe to `Shard.Collected` → count it (`Shards 7/12`). On game over call `Shard.ResetAll()`, which brings every shard back and raises `Shard.ShardsReset`.
- **P1 (phase):** the phased variants are ordinary phased objects: turn their colliders and renderers on and off like the platforms. A hidden kill zone can't kill, because its trigger collider is off.

## Needs agreement with the gameplay-logic owner

- Name and fields of the phase component (e.g. `PhaseObject` with a Day / Night / Both enum — not Unity tags).
- Whether it swaps to `M_Ghost_*` or uses its own ghost shader.
- Moving-platform script: the logic owner writes it; the platform owner attaches it to `Platform_Moving` variants.

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

- Add `CharacterController` and the Starter Assets `ThirdPersonController` (+ input) to the prefab root, or nest the prefab under the player object and move the `Animator` reference there. `ThirdPersonController` looks for the `Animator` on its own GameObject.
- `AC_Lightkeeper` uses the Starter Assets parameter names: `Speed`, `MotionSpeed`, `Grounded`, `Jump`, `FreeFall`. The blend tree thresholds are walk 2 and run 5.335 (the Starter Assets defaults). Retune them to the final run speed.
- The KayKit clips have no footstep / land animation events; `ThirdPersonController` works without them.

### Hand-off to phase core (P1)

- Nothing to wire for the lantern: it follows `DayNightLook` (see *Day / Night presentation*). Calling `DayNightLook.Instance.SetDay(isDay)` on each switch is enough.

### Ink on skinned meshes

`MeshInk.Bake(mesh, skinned: true)` keeps bone weights and bind poses, and stores the smoothed outline normal in the tangent (UV3.w = 1 tells the shader), because skinning bends tangents with the bones but not UV3. Each KayKit body part follows one bone, so the baked edge distances stay correct while animating.
