# Sun Switch — Menu UI Art Direction

Status: proposed implementation guide, based on the approved visual concept.
Scope: game menus only. The implementation plan is maintained separately on the
UI development branch.

## Visual identity

A hand-drawn celestial storybook above the clouds. Combine imperfect charcoal
outlines, warm paper textures, torn ink ribbons, illustrated clouds, and sun/moon
ornaments. The mood is curious, airy, and gently mysterious rather than horror.

The supplied resource-sheet reference informs the drawing technique, not its
characters, items, or economy. The generated gameplay mockup establishes the
palette and surface treatment, not a requirement to build its scenery or HUD.
Create original menu artwork; do not crop assets from either reference.

## Scope boundaries

Included: start, pause, settings, controls, confirmation, game-over, and victory
menus; shared buttons, panels, icons, typography, and menu transitions.

Excluded: gameplay HUD, platforms, characters, tower models, world clouds,
lighting, skyboxes, materials, level geometry, and phase effects. Menu sky/cloud
art is an independent 2D backdrop and must not modify the game environment.

## Palette

| Token | Color | Use |
| --- | --- | --- |
| Ink | `#282321` | Text, outlines, ribbon backing |
| Parchment | `#F1E5CA` | Panels and light text on ink |
| Cloud cream | `#FFF5DE` | Cloud highlights and small accents |
| Sky blue | `#8DBAD8` | Main-menu background |
| Sun gold | `#E9B64F` | Primary emphasis and sun ornaments |
| Sunset orange | `#C97645` | Small warm accents |
| Night indigo | `#424A78` | Moon ornaments and twilight backgrounds |
| Moon blue | `#B6CCE8` | Cool highlights |
| Muted red | `#AD5149` | Destructive-action accents |

Use ink on parchment and cream on ink for readable text. Gold and pale blue are
accents, not body text on cream. Validate actual contrast after importing assets.
Never communicate selection, disabled state, or destructive actions by color alone.

## Shape, texture, and illustration

- Panels: slightly irregular parchment edges, thin ink contour, restrained grain.
- Buttons: dark torn ribbons with generous clear centers for live text.
- Clouds: rounded hand-inked curls, clustered at corners and lower edges.
- Celestial motifs: expressive sun rays, crescent moons, a few tiny stars.
- Borders: modest gold/ink decoration; avoid heavy ornate frames on every control.
- Keep texture away from small text. Use consistent outline weight at display size.
- Keep clouds visually decorative; no illustrations of playable platforms are
  required for the menus.

Use separate background, panel, ornament, and icon sprites. Keep labels as live
text, never baked into images. Use scalable panel centers and separate corner
ornaments so resizing does not stretch cloud curls or sun rays.

## Typography and layout

Use an expressive, readable storybook display face for titles and a simple face
for controls, descriptions, and statistics. Font selection must satisfy the
project's asset policy; record the source and license before import. Use a
placeholder font during the functional pass rather than delaying implementation.

Design baseline: 1920 × 1080, with responsive anchors and a minimum 64 px outer
margin at that size. Starting type sizes: title 64–80 px, panel heading 40–48 px,
button 28–32 px, body 24–28 px. Allow text to wrap; do not shrink long labels until
they become hard to read. Buttons should be at least 56 px tall at the baseline.

Keep one obvious primary action per screen. Maintain consistent Back placement.
Decorative sprites must not intercept pointer input.

## Screen treatments

| Menu | Composition and appearance |
| --- | --- |
| Start | Blue sky with layered illustrated clouds; sun/moon crest and SUN SWITCH title; centered vertical button stack; short introduction underneath |
| Pause | Dimmed gameplay with a centered parchment panel and small cloud corners; no replacement world background |
| Settings | Parchment panel, readable labeled sliders, visible values, consistent Back button |
| Controls | Compact parchment instruction card with illustrated keycaps and mouse icon |
| Confirmation | Small modal above the calling menu; clear consequence and Cancel emphasized as safe default |
| Game over | Muted twilight cloud backdrop, dim celestial crest, concise statistics and Try Again |
| Victory | Warm sky, parted illustrated clouds, restored sun/moon crest and prominent results |

Start introduction: "The sun and moon have fallen out of sync. Restore the cycle."
Victory line: "The cycle is restored."

## Interaction states and motion

- Normal: cream label on dark ribbon.
- Hover/keyboard focus: gold outline plus a visible marker or underline.
- Pressed: a small downward offset and darker fill; keep the label readable.
- Disabled: reduced emphasis, no activation, and a clear reason when needed.
- Destructive: explicit wording such as Restart Run, with a confirmation dialog.

Use short 120–200 ms fades and gentle button movement. Optional slow cloud drift
belongs only to menu decoration and is polish, not an initial requirement. Avoid
flashing and excessive motion. Pause-menu animation must work while gameplay is
paused. Menu sounds, if added, are coordinated with the audio owner.

## Asset handoff and review

Store menu assets under `Assets/UI/Art/` with descriptive names and Unity `.meta`
files. Record provenance in `Assets/UI/Art/ASSET_SOURCES.md`; reconcile with the
shared `CREDITS.md` during integration. Existing GDD requirements for imported
assets remain applicable; do not assume generated imagery is automatically CC0.

Review at 1280 × 720, 1920 × 1080, 16:10, and an ultrawide aspect ratio. Backgrounds
may crop, but text and controls must remain visible. Check mouse and keyboard
focus, text contrast, disabled states, and readability over both bright and dark
gameplay backgrounds. Judge the UI separately from environment art quality.
