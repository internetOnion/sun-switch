# Sun Switch — Menu UI Development Plan

Status: planning only. No Unity implementation is included in this change.
Art specification: [Menu UI art direction](UI_ART_STYLE.md).
Development branch: `ui-development`.

## 1. Outcome and scope

Build a cohesive menu system using the approved hand-drawn sky/cloud and celestial
style. First deliver an isolated, reviewable menu preview; connect it to gameplay
only after the relevant owners expose and agree on integration points.

In scope:

| Screen | Actions/content |
| --- | --- |
| Start | Play, Settings, Controls, Quit, one introduction line |
| Pause | Resume, Settings, Controls, Restart Run, Main Menu |
| Settings | Music volume, SFX volume, mouse sensitivity; Back |
| Controls | WASD move, mouse look, Space jump, right mouse phase switch, Esc pause |
| Confirmation | Confirm restart or leaving an active run; Cancel |
| Game over | Time, deaths, shards out of 12; Try Again, Main Menu |
| Victory | Ending line, time, deaths, shards out of 12; Play Again, Main Menu |

Out of scope: gameplay HUD, tutorial popups, checkpoints, lives implementation,
phase logic, level loading implementation, platforms, environment art, camera
behavior, and audio-system implementation. No shop, inventory, level selector,
save system, or additional progression mechanics.

## 2. Current repository and ownership constraints

At planning time the repo contains the Unity starter scene and editor/tutorial
scripts, not the planned game managers or Main/Section scenes. uGUI and the Input
System are already listed in the package manifest. Treat interfaces below as
proposals, not existing APIs.

The GDD assigns game flow/UI to P3, phase/Main scene ownership to P1, player/camera
to P2, and look/audio to P4. Main scene changes require a PR; section scenes remain
with their owners. This work does not change those ownership rules.

There is a pre-existing local modification to `Packages/packages-lock.json`.
Preserve it and keep it out of UI documentation commits.

## 3. Isolation strategy

Use the installed uGUI stack for a Canvas-based menu system. Develop in a new
`Assets/UI/Scenes/MenuPreview.unity` scene with its own preview fixtures. Do not
edit SampleScene or shared production scenes to demonstrate menus.

Proposed layout:

```text
Assets/UI/
  Art/                 Menu-only sprites, fonts, and provenance
  Prefabs/             MenuRoot and reusable controls/panels
  Scripts/             Presentation, navigation, and integration contracts
  Preview/             Fake run data and preview-only action handlers
  Scenes/              MenuPreview.unity
Docs/
  UI_ART_STYLE.md
  UI_DEVELOPMENT_PLAN.md
```

Keep preview objects out of production prefabs. Commit Unity-generated `.meta`
files alongside assets and preserve their GUIDs. Prefer small screen prefabs over
one giant scene containing all UI authoring work.

Do not change Packages, ProjectSettings, global input actions, build profiles,
world rendering assets, or other members' scripts during isolated development.
Coordinate any necessary shared-file changes separately with their owners.

## 4. Integration contract to agree before wiring gameplay

Menu views display state and emit user intent; they do not own run state.

| Boundary | Menu responsibility | Owning system responsibility |
| --- | --- | --- |
| Start/restart | Emit one request, disable duplicate submissions while pending | Game flow resets run and loads the appropriate sections |
| Pause/resume | Show/hide panel in response to confirmed flow state | Game flow controls pause, timer, gameplay input, cursor, and restoration |
| Main menu | Confirm abandoning an active run, then emit request | Game flow ends/unloads run and restores menu state |
| Results | Render an immutable snapshot of time, deaths, shards, and outcome | Game flow supplies final values without UI-side recomputation |
| Settings | Display values and send edits | Audio/player adapters validate, apply, and persist values |
| Quit | Emit explicit Quit intent | Application flow quits the standalone build; preview only logs intent |

Choose concrete interface/type names with P3 during implementation. Use explicit
references or adapters, not scene-name lookups or assumed singleton names. No
duplicate timer, lives, shard storage, save service, or phase manager in the UI.

Only one production system should manage time scale, input enablement, and cursor
lock. UI code must not independently reset these globals. Menu transitions should
use unscaled time. Ensure one active EventSystem/input module after integration;
the preview's standalone setup must not be copied blindly into Main.

A missing integration adapter must produce a clear preview-only/unavailable state,
not silently pretend gameplay or a setting was changed. Settings stay disabled
until a real provider is available; preview fixtures can simulate them explicitly.

## 5. Navigation rules

- Start → Settings or Controls → Back returns to Start.
- Playing → Pause → Settings or Controls → Back returns to Pause, still paused.
- Esc closes the topmost submenu/modal before changing the parent menu.
- Esc on Pause requests Resume; Start and results screens do not pause/resume.
- Canceling restart/exit returns to the calling menu with focus restored.
- Confirming restart requests a full run reset as specified in the GDD.
- Game-over and victory screens cannot resume an ended run.
- Support mouse and keyboard navigation with visible focus and sensible defaults.
- Block clicks through modal overlays and prevent double activation during loading.

## 6. Implementation stages

### Stage A — Functional menu preview

Create the isolated scene, Canvas scaling, reusable placeholder controls, screen
navigation, modal stack, and keyboard focus. Add explicit preview buttons or
fixtures to show paused, game-over, and victory states using fake data.

Done when every screen and return path works without any game managers or shared
scene edits. Preview actions must be identifiable as simulations.

### Stage B — Shared visual components

Create original cloud corners, parchment panels, ink ribbons, sun/moon crest,
keycaps, and minimal settings/results icons. Apply the art guide and implement
normal, focused, hovered, pressed, and disabled states. Keep text live and reuse
prefabs so visual changes propagate consistently.

Done when the Start menu and one secondary panel establish the full style at
multiple resolutions. Review those before expanding asset production.

### Stage C — Complete menu presentation

Apply shared components to pause, controls, settings, confirmations, and results.
Use 2D sky/cloud art for full-screen menus and dimmed overlays for pause. Add short
unscaled transitions; defer decorative cloud animation and sound polish if needed.

Done when all menu states are visually consistent and usable with mouse/keyboard.

### Stage D — Coordinated integration

With P3, connect menu intents/results to game flow. With P2/P4, connect sensitivity
and volume adapters. Have P1 integrate the production menu prefab into Main via
the required PR. Keep preview fixtures out of the build; coordinate build-profile
changes rather than editing them as part of art work.

Done when starting, pausing, resuming, restarting, returning to menu, losing, and
winning work against real game state without duplicate global managers.

### Stage E — Verification and handoff

Check 720p, 1080p, 16:10, and ultrawide layouts; focus order; long labels; input
blocking; and bright/dark pause backdrops. Verify settings against their real
providers. Repeatedly open/close menus to catch duplicate event subscriptions.

Integration checks: pause freezes the intended gameplay and timer; resume restores
prior input/cursor state; confirm/cancel do not lose state unexpectedly; restart
resets time/deaths/shards according to the GDD; results match the supplied snapshot;
and quit behaves correctly in a Windows build. Run the team's fresh-clone check.

Add targeted automated checks for navigation and action dispatch if logic warrants
them; use Unity visual/play-mode checks for presentation. No tests are needed for
this documentation-only change.

## 7. Conflict prevention and merge checklist

- Work on `ui-development`; review status before staging and stage explicit paths.
- Keep unrelated user changes intact. Do not reformat or reserialize shared assets.
- Split later work into reviewable commits: preview, components, screens, integration.
- Coordinate ownership before changing a shared prefab/scene; update from main before integration.
- Never resolve scene YAML conflicts by blindly choosing one side; ask the scene owner to reconcile.
- Before merging, confirm the diff is limited to agreed UI files and deliberate integration edits.
- Verify references/GUIDs, missing scripts, asset provenance, and Unity import/console errors.

These boundaries reduce conflict risk; they cannot guarantee a conflict-free merge
while teammates continue changing the project.
