# UI Component & Styling Pipeline Rules

These guidelines apply when interacting with or expanding the UI Component and Styling system in this project:

## 1. Automated UI & Styling Creation
- When the user asks to add UI components, import designs from the web, convert Tailwind CSS or HTML snippets, or build UI elements from screenshots/descriptions, **ALWAYS use the UI Component Pipeline**.
- When the user asks to create or import color palettes, theme tokens, or CSS animations from web snippets/hex lists/keyframes, **use the Styling Pipeline**.
- Use the MCP tools (`build_ui_component`, `build_ui_theme`, `build_ui_animation`, `build_ui_style_class`) or CLI:
  - `node Scripts/ui-cli.js build --code "..." --name "<Name>" --category "<Category>"`
  - `node Scripts/ui-cli.js theme --name "<Name>" --code "..."`
  - `node Scripts/ui-cli.js animation --name "<Name>" --code "..."`
  - `node Scripts/ui-cli.js style --name "<Name>" --theme-token <token> --anim-class <class>`
- Do not manually construct `.prefab` files or ScriptableObjects by raw string manipulation.

## 2. Component Organization & Folder Structure
- Always assign a sensible category for UI prefabs:
  - `Buttons`, `Cards`, `Inputs`, `Badges`, `Modals`, `HUD`.
- Every component, theme, animation, and style class MUST have its own self-contained folder:
  - `Assets/_Project/Presets/<Category>/<ComponentName>/`
  - `Assets/_Project/Styling/Themes/<ThemeName>/`
  - `Assets/_Project/Styling/Animations/<AnimName>/`
  - `Assets/_Project/Styling/Styles/<StyleName>/`

## 3. Thumbnails & Previews
- Every generated component prefab must have an auto-framed 256×256 thumbnail rendered against standard dark neutral `#1E2025`.
- Every generated theme palette must have a dynamic 8×8 square swatch grid thumbnail (up to 64 colors) rendered against `#1E2025`.
- Every generated animation class must have its waveform thumbnail in its folder.
- **Never trigger a batch re-bake of all library thumbnails**; only bake on demand or on save/creation.

## 4. Spawning Into Scenes
- When the user asks to see or test a component in the scene, use:
  `node Scripts/ui-cli.js spawn "<Name>"` (or MCP tool `spawn_ui_component`).

## 5. Mandatory Verification Protocol (Build -> Inspect -> Validate)
- Never assume `build` success implies visual correctness.
- Always run `node Scripts/ui-cli.js inspect --name "<Name>"` (or `inspect_ui_component`) after building.
- Check:
  - `isVerticalDistorted`: Must be `false`.
  - `hasLowContrast`: Must be `false` (contrast ratio must be $\ge 3.5:1$ between text and background).
  - Bounds & Collisions: Child widths must not overflow container width; elements must not overlap unexpectedly.

## 6. Parametric Color & Contrast Guardrails
- **60-30-10 Rule**: 60% dominant background surfaces (low saturation $S < 15\%$), 30% secondary cards/panels ($+8\%\dots+12\%$ brightness elevation), 10% high-contrast accents.
- **Ban Pure Extremes**: Never use $S=100\%$ with $V=100\%$ (neon). General UI saturation must stay between $60\%\dots85\%$.
- **No Pure Black / White**: Replace `#000000` with dark surface tones ($V \in [8\%, 15\%]$) and `#FFFFFF` with soft off-whites ($V \in [92\%, 98\%]$).
- **Tinted Neutrals**: Container surfaces must inherit anchor hue ($S \in [4\%, 12\%]$) to maintain chromatic cohesion.
- **WCAG Contrast**: Maintain a minimum contrast ratio $\ge 4.5:1$ between text/icons and parent container backdrops.

## 7. Cross-Project Package Sharing (Side-by-Side Local Package)
- For multi-project development, maintain this UI library as a standalone Git repo linked via `"file:../../ui-quality-life"` in `Packages/manifest.json`.
- This ensures local edits remain writable and propagate across all consumer projects instantly without submodule conflicts or git remote confusion.
