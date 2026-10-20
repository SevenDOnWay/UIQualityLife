---
name: ui-component-builder
description: Convert web UI code (HTML/Tailwind/CSS), design specs, or visual descriptions into native Unity UGUI prefabs with automated 256x256 thumbnails and shelf presets. Use whenever the user asks to add UI components, import web UI elements, generate buttons/cards/dialogs, or automate UI creation.
---

# Unity UI Component Builder Skill

This skill guides you through converting web designs, Tailwind CSS/HTML snippets, and declarative UI specifications into production-ready Unity UGUI prefabs with automated 256×256 auto-framed thumbnails and shelf presets.

## Architecture & Available Tooling

The project includes an automated UI Pipeline comprising:
1. **MCP Server** (`ui-pipeline`): Exposes stdio tools (`build_ui_component`, `list_ui_presets`, `spawn_ui_component`).
2. **Unity CLI** (`node Scripts/ui-cli.js`): Cross-platform CLI for building, spawning, listing, and querying bridge health.
3. **Spec Parser** (`Scripts/spec-parser.js`): Translates HTML & Tailwind CSS classes into `UIComponentSpec` JSON.
4. **Unity Live Bridge Server** (`Assets/_Project/Editor/Pipeline/UIEditorBridgeServer.cs`):
   - Fast HTTP API on `http://127.0.0.1:8765/` (active when Unity Editor is running).
   - Zero-dependency File Queue fallback (`Assets/_Project/Pipeline/Queue/*.json` -> `Finished/`).
5. **Unity Builder Engine** (`Assets/_Project/Editor/Pipeline/UIComponentBuilder.cs`):
   - Builds UGUI hierarchy (`CanvasRenderer`, `RectTransform`, `Image`, `TextMeshProUGUI`, `Button`, `LayoutGroup`).
   - Saves prefab to `Assets/_Project/Prefabs/UI/<Category>/<Name>.prefab`.
   - Creates/updates `UIPresetItem` ScriptableObject in `Assets/_Project/Presets/<Category>/<Name>.asset`.
   - Renders 256×256 auto-framed thumbnail with `#1E2025` contrast background and 12% margin padding to `Assets/_Project/Thumbnails/<Name>_thumb.png`.

---

## When to Use This Skill

Activate this workflow when the user:
- Gives you web code (HTML, Tailwind CSS, SVG/CSS) and wants it in Unity.
- Shares a UI screenshot or description and asks to create a Unity UI component.
- Asks to populate the UI Component Library shelf with common UI elements (cards, buttons, modal dialogs, HUD elements).
- Wants to spawn a preset into the active scene Canvas.

---

## Usage Methods

### Method 1: Using MCP Tools (Preferred in Agent Environment)

If the `ui-pipeline` MCP server is loaded, you can call its tools directly:

1. **`build_ui_component`**:
   ```json
   {
     "name": "EmeraldBadge",
     "category": "Badges",
     "code": "<div class=\"flex items-center px-4 py-1.5 bg-emerald-600 rounded-full text-white font-bold text-sm shadow-md\">Online</div>"
   }
   ```
   Or pass a detailed `spec` JSON object for pixel-perfect layout hierarchy.

2. **`list_ui_presets`**:
   Lists all registered presets in the project categorized by tags.

3. **`spawn_ui_component`**:
   ```json
   {
     "name": "EmeraldBadge"
   }
   ```
   Spawns an instance into the open Canvas in Unity.

---

### Method 2: Using the CLI via `run_command`

You can execute the CLI directly via `run_command`:

#### Build from HTML / Tailwind Code
```bash
node Scripts/ui-cli.js build --code "<button class=\"bg-indigo-600 text-white font-bold py-2 px-6 rounded-lg shadow-lg hover:bg-indigo-700\">Confirm Order</button>" --name "Btn_ConfirmOrder" --category "Buttons"
```

#### Build from a Spec JSON File
```bash
node Scripts/ui-cli.js build --spec path/to/spec.json
```

#### Spawn a Preset into the Scene
```bash
node Scripts/ui-cli.js spawn "Btn_ConfirmOrder"
```

#### List Available Presets
```bash
node Scripts/ui-cli.js list
```

#### Check Unity Bridge Status
```bash
node Scripts/ui-cli.js status
```

On Windows, `Scripts\ui-cli.cmd` and `Scripts/ui-cli.ps1` are also available.

---

## Declarative Spec Reference (`UIComponentSpec`)

When maximum visual fidelity or complex layout hierarchies (such as cards with nested icons, badges, titles, descriptions, and button bars) are needed, you can compose a `UIComponentSpec` JSON object:

```json
{
  "name": "ModernCard",
  "category": "Cards",
  "rootNode": {
    "name": "CardRoot",
    "width": 320,
    "height": 200,
    "colorHex": "#2D3748",
    "cornerRadius": 12,
    "layoutType": "Vertical",
    "padding": { "top": 16, "bottom": 16, "left": 16, "right": 16 },
    "spacing": 12,
    "childAlignment": "UpperLeft",
    "children": [
      {
        "name": "HeaderRow",
        "height": 30,
        "layoutType": "Horizontal",
        "spacing": 8,
        "childAlignment": "MiddleLeft",
        "children": [
          {
            "name": "Icon",
            "width": 24,
            "height": 24,
            "colorHex": "#4FD1C5"
          },
          {
            "name": "Title",
            "text": "Card Header",
            "fontSize": 18,
            "fontStyle": "Bold",
            "textColorHex": "#FFFFFF"
          }
        ]
      },
      {
        "name": "BodyText",
        "height": 80,
        "text": "This is a clean, modern card component automatically baked with 256x256 thumbnail preview.",
        "fontSize": 14,
        "textColorHex": "#CBD5E0"
      },
      {
        "name": "ActionButton",
        "height": 40,
        "colorHex": "#3182CE",
        "cornerRadius": 6,
        "isButton": true,
        "buttonTransition": "ColorTint",
        "children": [
          {
            "name": "Label",
            "text": "Action Button",
            "fontSize": 15,
            "textColorHex": "#FFFFFF",
            "alignment": "Center"
          }
        ]
      }
    ]
  }
}
```

### Supported Node Properties:
- **Dimensions & Anchors**: `width`, `height`, `anchorMin` `[x, y]`, `anchorMax` `[x, y]`, `pivot` `[x, y]`, `anchoredPosition` `[x, y]`.
- **Background & Styling**: `colorHex` (e.g. `#4F46E5`), `cornerRadius` (sets raycast and sprite slicing if applicable).
- **Layout Groups**: `layoutType` (`"None"`, `"Horizontal"`, `"Vertical"`, `"Grid"`), `padding` (`top`, `bottom`, `left`, `right`), `spacing`, `childAlignment` (`"UpperLeft"`, `"MiddleCenter"`, etc.), `childControlWidth`, `childControlHeight`, `childForceExpandWidth`, `childForceExpandHeight`.
- **Typography (`TextMeshProUGUI`)**: `text`, `fontSize`, `fontStyle` (`"Normal"`, `"Bold"`, `"Italic"`), `textColorHex`, `alignment` (`"Left"`, `"Center"`, `"Right"`).
- **Interactivity**:
  - `isButton`: Adds UGUI `Button` component with hover/pressed states.
  - `isInputField`: Adds TMP Input Field with placeholder and caret.
  - `isSlider`: Adds UGUI Slider hierarchy (Background, Fill Area, Handle).
  - `isToggle`: Adds UGUI Toggle hierarchy with checkmark graphic.

---

## Best Practices for Adding Components

1. **Category Organization**:
   - Use standard categories: `"Buttons"`, `"Cards"`, `"Inputs"`, `"Badges"`, `"Modals"`, `"HUD"`, `"Nav"`.
   - Categories automatically become search tags on the preset shelf.
2. **Fixed-Square Thumbnails**:
   - The thumbnail renderer frames the bounding box of the root RectTransform plus 12% padding onto a 256×256 canvas with dark neutral `#1E2025` backdrop.
   - For wide components (like horizontal navbars or wide buttons), ensure root `width` is set appropriately so the auto-framing camera fits the full component without clipping.
3. **Responsive Sizing**:
   - For components with child layout groups, ensure `childControlWidth: true` and `childControlHeight: false` or appropriate fit modes are specified if variable child contents are used.
