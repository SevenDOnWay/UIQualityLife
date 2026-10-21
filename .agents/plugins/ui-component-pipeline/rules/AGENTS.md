# UI Component Pipeline Rules

These guidelines apply when interacting with or expanding the UI Component system in this project:

## 1. Automated UI Creation
- When the user asks to add UI components, import designs from the web, convert Tailwind CSS or HTML snippets, or build UI elements from screenshots/descriptions, **ALWAYS use the UI Component Pipeline**.
- Use either the MCP tool `build_ui_component` or the CLI:
  `node Scripts/ui-cli.js build --code "..." --name "<Name>" --category "<Category>"`
- Do not manually construct `.prefab` files by string manipulation or ask the user to manually create GameObjects in the scene hierarchy.

## 2. Component Organization & Taxonomy
- Always assign a sensible category:
  - `Buttons`: Action buttons, icon buttons, toggle pills, button groups.
  - `Cards`: Product cards, profile cards, info containers, notification widgets.
  - `Inputs`: Text fields, search bars, sliders, number steppers, toggles.
  - `Badges`: Tags, status badges, price pills, notifications counters.
  - `Modals`: Alert dialogs, confirmation popups, modal sheets.
  - `HUD`: Health bars, stamina gauges, player frames, minimap borders.
- Categories automatically serve as search tags and suggestion chips in the UI Quality of Life shelf.

## 3. Thumbnails & Previews
- Every generated component must have an auto-framed 256×256 thumbnail rendered against the standard dark neutral background (`#1E2025`).
- The pipeline handles thumbnail generation automatically at creation/save time.
- **Never trigger a batch re-bake of all library thumbnails**; only bake on demand or on save to keep the library scalable to 100+ items.

## 4. Spawning Into Scenes
- When the user asks to see or test a component in the scene, use:
  `node Scripts/ui-cli.js spawn "<Name>"` (or MCP tool `spawn_ui_component`).
- If no Canvas exists in the open scene, the bridge will automatically locate or create a Canvas and EventSystem.

## 5. Cross-Project Package Sharing (Side-by-Side Local Package)
- For multi-project development, maintain this UI library as a standalone Git repo linked via `"file:../../ui-quality-life"` in `Packages/manifest.json`.
- This ensures local edits remain writable and propagate across all consumer projects instantly without submodule conflicts or git remote confusion.

