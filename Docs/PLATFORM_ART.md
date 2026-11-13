# Sun Switch — Platform & Character Art

Owner: platforms, platform prefabs, phase materials, character visual.

## Folders

| Path | Owner | Contents |
| --- | --- | --- |
| `Assets/Prefabs/Platforms/` | Platform art | Prefabs level designers place (`Platform_Medium_Day`, …) |
| `Assets/Prefabs/Platforms/Base/` | Platform art | Base prefabs the variants inherit from — do not place these |
| `Assets/Art/Materials/Phase/` | Platform art | `M_Phase_Day/Night/Both`, `M_Ghost_Day/Night` |
| `Assets/Art/Shaders/` | Platform art | `SunSwitch/Storybook` and `SunSwitch/StorybookGhost` |
| `Assets/Art/Meshes/` | Platform art | Real-size platform meshes (grey-box now, Blender exports later) |
| `Assets/Art/Editor/` | Platform art | Platform tools (`Sun Switch > Art > …`) |
| `Assets/Art/Characters/Lightkeeper/` | Character art | Lightkeeper model (KayKit), lantern, animator controller |
| `Assets/Art/Materials/Character/` | Character art | `M_Lightkeeper`, `M_LanternGlow` |
| `Assets/Art/Scripts/` | Character art | `LanternGlow` (runtime) |
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

- Call `LanternGlow.SetDay(bool isDay)` on the player's lantern when the world switches. It fades over 0.25 s, the same as the world lighting.

### Ink on skinned meshes

`MeshInk.Bake(mesh, skinned: true)` keeps bone weights and bind poses, and stores the smoothed outline normal in the tangent (UV3.w = 1 tells the shader), because skinning bends tangents with the bones but not UV3. Each KayKit body part follows one bone, so the baked edge distances stay correct while animating.
