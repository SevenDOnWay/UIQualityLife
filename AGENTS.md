# UI Quality of Life & Component Pipeline

This project is a Unity 6 tool for rapid UI prototyping, preset management, and automated UI creation.

## Automated UI Creation (CLI & MCP)
- When adding UI components, translating web designs (Tailwind CSS, HTML, CSS), or generating UI from user prompts/images, **use the UI Component Pipeline**.
- **CLI Commands**:
  - `node Scripts/ui-cli.js build --code "..." --name "<Name>" --category "<Category>"`
  - `node Scripts/ui-cli.js build --spec <path-to-spec.json>`
  - `node Scripts/ui-cli.js spawn "<Name>"`
  - `node Scripts/ui-cli.js list`
  - `node Scripts/ui-cli.js status`
- **MCP Server**: Registered under plugin `.agents/plugins/ui-component-pipeline/` exposing:
  - `build_ui_component`: Build and bake UI prefabs from code or specs.
  - `spawn_ui_component`: Instantly instantiate components into the active scene Canvas.
  - `list_ui_presets`: Query library presets and tags.

## Core Architecture & Principles
- Follow OOP & SOLID principles:
  - `UIPresetItem`: ScriptableObject representing component metadata, search tags, prefab reference, and thumbnail.
  - `UIComponentBuilder`: Core pipeline engine for GameObject instantiation and thumbnail baking.
  - `UIEditorBridgeServer`: Dual-mode bridge (HTTP `http://127.0.0.1:8765/` and file queue in `Assets/_Project/Pipeline/Queue/`).
  - `UIThumbnailBaker`: Fixed-square (256×256) auto-framing thumbnail baker with dark neutral `#1E2025` backdrop and 12% padding.
- **Performance**: Never batch re-bake all thumbnails. Only bake thumbnails on-demand or after save/creation.
