# UI Quality of Life & Component Pipeline

This project is a Unity 6 tool for rapid UI prototyping, preset management, automated UI creation, and CSS-like styling & animations.

## Automated UI Creation (CLI & MCP)
- When adding UI components, translating web designs (Tailwind CSS, HTML, CSS), or generating UI from user prompts/images, **use the UI Component Pipeline**.
- When adding **Color Palettes / Themes** or **CSS Animations**, use the Styling Pipeline.
- **CLI Commands**:
  - `node Scripts/ui-cli.js build --code "..." --name "<Name>" --category "<Category>"`
  - `node Scripts/ui-cli.js build --spec <path-to-spec.json>`
  - `node Scripts/ui-cli.js theme --name "<Name>" --code "<css-or-tailwind>"`
  - `node Scripts/ui-cli.js animation --name "<Name>" --code "<hover-or-keyframes>"`
  - `node Scripts/ui-cli.js style --name "<Name>" --theme-token <token> --anim-class <class>`
  - `node Scripts/ui-cli.js spawn "<Name>"`
  - `node Scripts/ui-cli.js list`
  - `node Scripts/ui-cli.js status`
- **MCP Server**: Registered under plugin `.agents/plugins/ui-component-pipeline/` exposing:
  - `build_ui_component`: Build and bake UI prefabs from code or specs.
  - `build_ui_theme`: Build and bake UI theme palettes (8x8 dynamic square grid thumbnail) from CSS variables, Tailwind tokens, or hex lists.
  - `build_ui_animation`: Build UI animation classes with state machines, curves, and thumbnails from CSS pseudo-classes or `@keyframes`.
  - `build_ui_style_class`: Bundle color tokens, corner radii, and animation classes.
  - `spawn_ui_component`: Instantly instantiate components into the active scene Canvas.
  - `list_ui_presets`: Query library presets and tags.

## Core Architecture & Principles
- Follow OOP & SOLID principles:
  - `UIPresetItem`: ScriptableObject representing component metadata, search tags, prefab reference, and thumbnail.
  - `UIThemePalette`: Centralized CSS variables and color token library with 8x8 dynamic square swatch thumbnails.
  - `UIAnimationClass`: CSS-like Visual State Machine (`UIVisualState`) supporting hover, press, focus, disabled, click bounce, and loops.
  - `UIStyleClass`: Compound class bundling theme color, corner radius, and animation profiles.
  - `UIComponentBuilder` & `UIStylingBuilder`: Core pipeline engines for GameObject instantiation, asset creation, and thumbnail baking.
  - `UIEditorBridgeServer`: Dual-mode bridge (HTTP `http://127.0.0.1:8765/` and file queue in `Assets/_Project/Pipeline/Queue/`).
  - **Folder Structure**: Every preset and styling asset gets its own self-contained directory (`Assets/_Project/Presets/<Cat>/<Name>/`, `Assets/_Project/Styling/Themes/<Name>/`, `Animations/<Name>/`, `Styles/<Name>/`).
- **Performance**: Never batch re-bake all thumbnails. Only bake thumbnails on-demand or after save/creation.

## Cross-Project Package Sharing (Side-by-Side Local Package)
- In multi-project workflows, keep this UI library as a standalone Git repo linked via `"file:../../ui-quality-life"` in `Packages/manifest.json`.
- Edits stay writable, new components propagate across all consumer projects instantly, and `git add .` / `git remote` remain strictly isolated per repository.
