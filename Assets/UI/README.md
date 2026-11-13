# Menu Canvas design

Open `Assets/Scenes/StartGame.unity` in **Unity 6000.3.8f1** and press Play.
`Assets/UI/Scenes/MenuPreview.unity` provides the same isolated review setup.
Neither scene loads gameplay or changes the build scene list.

## Editing the Canvas

Expand **Menu Root → Menu Canvas** in the Hierarchy. Start, Pause, Settings,
Controls, Confirmation, GameOver, Victory and Message are saved screen objects.
Select their RectTransforms, live Text labels, ribbons, sliders and artwork to
edit them in the Inspector. Toggle screen objects in edit mode to inspect them.
Play mode starts on Start and the view manages visibility and input.

The Canvas uses 1920 × 1080 with Expand scaling. Decorative graphics do not
intercept input. Palette, irregular parchment, quiet grain, outlined cloud banks,
celestial crest and torn ribbons follow `Docs/UI_ART_STYLE.md`. Typography uses
Unity's bundled placeholder font; the final display font still needs a licensed
art pass. Artwork provenance is recorded in `Art/ASSET_SOURCES.md`.

`Prefabs/MenuRoot.prefab` contains the reusable menu presentation and serialized
button actions, without preview fixtures, camera, EventSystem or a flow adapter.
The two scenes additionally contain a preview adapter, a neutral background
camera, a single Input System EventSystem and a separate preview toolbar.

**Sun Switch → UI → Rebuild isolated menu scenes** regenerates both scenes and
the prefab from `StartMenuView.BuildCanvas`. This replaces manual scene/prefab
layout edits; keep those edits or incorporate them into the builder before using
the rebuild command. Normal Play does not rebuild the saved Canvas.

## Preview navigation

- Use the preview toolbar or F1 Start, F2 Pause, F3 Game Over and F4 Victory.
- Mouse and arrow keys navigate controls; Enter activates the selected button.
- Settings has local simulated volume/sensitivity sliders with visible values.
  Changes are explicitly preview-only, do not affect gameplay and are not saved.
- Back/Esc restores the calling screen and its keyboard focus. Esc on Pause
  emits a preview Resume intent. Esc on Start/results does not resume a run.
- Restart Run and leaving a paused run show a confirmation with Cancel selected.
  The parent screen remains visible but cannot receive input through the modal.
- Results show an immutable supplied time/deaths/shards snapshot. F3/F4 supply
  labeled preview data; the UI does not track gameplay statistics.
- Play, Quit, Resume and Restart show preview messages. Main Menu returns to
  Start locally. No levels load, no application quits and no run data changes.

Button states use cream labels, gold focus outlines and chevrons, pressed label
offsets and short unscaled transitions. Screen entrances fade over 150 ms.

## Integration boundary

Implement `MenuFlowAdapter.Request` in the owning game-flow system and call
`BindFlow`. Start/Quit also retain their dedicated abstract methods. Call
`CompleteRequest` on failure or return to release duplicate-request protection.
Use `ShowPause`, `ShowStart` and `ShowResults` for confirmed game-flow states.
A missing adapter shows an unavailable message; settings are disabled without
the explicit preview fixture. Real settings providers remain an integration task.
The view does not own scene loading, cursor lock, time scale, saves or run state.
Provide the production EventSystem when integrating the prefab.

## Verification

`MenuVerification.Verify` checks navigation/confirmation paths, duplicate start
requests, missing-provider settings and serialized button actions. It renders
11 review images under the Unity project's `Review` directory: four Start aspect
ratios, Settings, Controls, bright/dark Pause, Confirmation, Game Over and Victory.
It also checks text height and artwork meshes. The menu command is **Sun Switch
→ UI → Verify navigation and render review**. Run it with no unsaved scene edits.

`MenuPlayVerification.Run` checks actual Play-mode initial focus, keyboard submit,
restored focus, Cancel-first confirmation, parent input blocking and results
focus. Verification is performed in an isolated project copy under `.utmp` so
the user's open project and unrelated shared settings remain untouched.

Gameplay/provider integration, a licensed display font, menu audio and manual
mouse/keyboard usability review remain separate handoff work.
