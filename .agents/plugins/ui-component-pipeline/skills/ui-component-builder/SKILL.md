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

## 🛑 Mandatory Closed-Loop Verification Protocol

When creating or modifying UI components, AI agents **MUST NEVER** report completion based on `build` success alone. Unity will return `{ "success": true }` even if elements are completely overlapping, invisible, or distorted.

Always execute this 3-step loop:
1. **Step 1: Build** — Call `build_ui_component` (or `node Scripts/ui-cli.js build ...`).
2. **Step 2: Inspect** — Call `inspect_ui_component` (or `node Scripts/ui-cli.js inspect --name "<Name>"`).
3. **Step 3: Validate Diagnostics**:
   - `isVerticalDistorted`: Must be `false` (no 1-character vertical text collapse).
   - `hasLowContrast`: Must be `false` (text-to-background contrast ratio $\ge 3.5:1$).
   - `rect.width` / `rect.height`: Must be positive numbers ($> 0$), never negative.
   - Sibling order: Background fills and progress bars must have a lower `siblingIndex` than text overlays so they do not occlude text.
   - If any check fails, adjust the spec JSON and re-build until all diagnostics pass.

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

4. **`inspect_ui_component`**:
   ```json
   {
     "name": "EmeraldBadge"
   }
   ```
   Inspects the live instantiated component in the scene, returning its full hierarchy tree, `RectTransform` bounds (`width`/`height`), layout groups, and text contents.

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

#### Inspect Live In-Scene Layout & Bounds
```bash
node Scripts/ui-cli.js inspect --name "Btn_ConfirmOrder"
```

The inspect tool outputs full diagnostic JSON containing:
- **Geometry & Sizing**: `rect` (`x, y, width, height`), `sizeDelta`, `offsetMin`, `offsetMax`.
- **Position & Alignment**: `anchoredPosition`, `anchorMin`, `anchorMax`, `pivot`.
- **Hierarchy Layer & Z-Order**: `siblingIndex` (determines render draw order), `depth`, `layer`, `active`.
- **Text & Diagnostics**: `text`, `fontSize`, `wrapping`, `overflow`, `preferredWidth`, `preferredHeight`, `isVerticalDistorted`.
- **Layout Constraints**: `layoutGroup` (`type`, `spacing`, `padding`, `childControlWidth`), `layoutElement` (`preferredWidth`, `flexibleWidth`).

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

## 🔍 Validation & Layout Troubleshooting Rules

When building or fixing UI components, AI agents MUST validate against these 4 golden rules of Unity UGUI:

### 1. The "Negative Width" / Vertical Text Wrapping Trap
- **Symptom**: Text wraps each character onto a new line vertically (e.g. `P \n A \n U \n S \n E \n D`) or has `rect.width < 0`.
- **Root Cause**: When a child is placed inside a parent `HorizontalLayoutGroup` or `VerticalLayoutGroup` with `childControlWidth = true`:
  - If the child does not declare a `LayoutElement` with `preferredWidth`, Unity's layout system calculates its width as 0 or minimal padding.
  - An inner stretch text (`anchorMin = 0, anchorMax = 1`) with negative offsets collapses into a negative bounding box.
- **Fix**:
  1. Always set `preferredWidth` and `preferredHeight` on child buttons and cards via `LayoutElement` (e.g. `preferredWidth: 160, preferredHeight: 44`).
  2. For single-word labels or button text, set `textWrappingMode = NoWrap` and `overflowMode = Ellipsis`.

### 2. Sibling Hierarchy & Z-Order Layering (Overlapping Text & Fills)
- **Symptom**: Text appears hidden, darkened, or covered by a background/progress bar fill.
- **Root Cause**: In Unity UGUI, **sibling index is draw order**. The last sibling is drawn on top of earlier siblings!
- **Fix**:
  - In Health/Mana bars, progress bars, and card containers: the fill image (`Fill`) must have a lower `siblingIndex` than the text label (`HPLabel`).
  - Add text with an outline (`outlineWidth = 0.25`, `outlineColor = #000000DD`) so it remains crisp and high-contrast over dynamic fills.
  - For complex bars, separate into a header row (`HP` on Left, `820/1000` on Right) and place the progress bar underneath.

### 3. LayoutElement Flexibility in Compound Rows
- When placing buttons in a horizontal row (e.g. `Cancel` + `Confirm` in Modals):
  - Give each button a `preferredWidth` and `flexibleWidth: 1`.
  - Set the parent's `childControlWidth: true` and `childForceExpandWidth: true` so buttons evenly share available modal width.

### 4. Anchoring and Stretch Offsets
- Full-fill elements (such as button background text or modal backdrops) must use stretch anchors:
  `anchorMin: [0, 0]`, `anchorMax: [1, 1]`, with `offsetMin: [0, 0]`, `offsetMax: [0, 0]`, `sizeDelta: [0, 0]`.
  Do NOT apply large negative offsets that shrink the rect below 0.

---

## 🔤 Google Font & Material Symbols Icon Integration

The project has Google's **Material Symbols** font asset (`MaterialSymbolsRounded-Regular SDF.asset`) integrated for vector UI icons.

### 1. Dynamic SDF Font Mechanics (Why the Character Table Starts at ~16)
- **On-Demand (Lazy) Generation**: The asset is configured in **Dynamic Atlas Mode** (`Atlas Population Mode = Dynamic`). Google Material Symbols contains over 3,000+ icons; pre-rasterizing all icons into a static atlas would consume excessive memory and require a huge 4K/8K texture.
- **Initial State**: A freshly created or empty dynamic font asset only contains ~16 control characters (e.g. `Space`, `Line Break`, `Tab`, missing glyph placeholder).
- **Automatic Runtime & Editor Baking**: The moment any TextMeshPro component requests a character (e.g. `0xE88A`), TextMeshPro dynamically reads the vector outline from `MaterialSymbolsRounded-Regular.ttf`, rasterizes it into the SDF texture atlas on the fly, and appends it to the **Character Table** and **Glyph Table**.

### 2. OpenType Ligatures vs. Unicode Codepoints in Unity
- **The Ligature Table (1027+ records)**: Contains the web ligature substitution rules embedded by Google (e.g. rule #0 is the word `x_circle` $\rightarrow$ target glyph `1144`). In Unity's inspector, these appear as raw hex component IDs because the target glyphs are not yet baked into the atlas texture.
- **Why Typing Words (e.g. "home") Fails**: In web browsers, HTML/CSS text shaping engines automatically replace ligature words with icon glyphs. **Unity TextMeshPro does NOT evaluate multi-glyph word ligatures on dynamic fonts**. Typing `"home"` into a TMP text field will simply render the individual English letters `h`, `o`, `m`, `e`.
- **Golden Rule**: **Always reference icons via their Unicode Codepoint (`\uE88A`) or literal character glyph (``), never by ligature word names.**

### 3. How to Use Icons in UI Components

#### Method A: In Declarative Spec JSON (`UIComponentSpec`)
When generating icon buttons, badge icons, or tabs, specify the Unicode escape sequence in the `text` field:
```json
{
  "name": "SettingsIcon",
  "type": "Text",
  "text": "\uE8B8",
  "fontSize": 20,
  "textColorHex": "#A1A1AA",
  "alignment": "Center",
  "width": 24,
  "height": 24,
  "wrap": false
}
```

#### Method B: In Unity Inspector Text Fields
1. **Font Asset**: Set to `MaterialSymbolsRounded-Regular SDF` (or leave default `LiberationSans SDF`, as `MaterialSymbolsRounded-Regular SDF` is set as its Fallback Font).
2. **Text Input**: Copy and paste the actual character symbol directly into the Inspector (e.g. `` for Home, `` for Settings) or assign via C# (`tmpText.text = "\uE88A"`).
3. **Font Style**: Keep as **Normal** (regular). Toggle **Bold** if a thicker stroke weight is required.
4. **Wrapping**: Set wrapping mode to **Disabled** (`wrap: false`).

#### Method C: Inline Text + Icon via Fallback Font
Because `MaterialSymbolsRounded-Regular SDF` is configured in `LiberationSans SDF`'s **Fallback Font Asset Table**, any standard TextMeshProUGUI text component can render icons inline with regular text automatically without manual font switching:
```csharp
// Seamlessly renders standard text with icon glyphs inline:
myTmpText.text = "\uE88A Home  |  \uE8B8 Settings";
```

### 4. Common Material Symbols Codepoints Reference
| Icon Name | Unicode Escape | Literal Glyph (Copy-Paste) | Common UI Usage |
| :--- | :---: | :---: | :--- |
| **home** | `\uE88A` | `` | Main menu, dashboard |
| **settings** | `\uE8B8` | `` | Options dialog, preferences |
| **search** | `\uE8B6` | `` | Search inputs, filter bars |
| **close / x** | `\uE5CD` | `` | Modal close buttons, cancel |
| **check / done** | `\uE876` | `` | Confirmation, completed tasks |
| **favorite / heart** | `\uE87D` | `` | Health, wishlist, likes |
| **menu** | `\uE5D2` | `` | Mobile hamburger drawer |
| **refresh** | `\uE5D5` | `` | Reload, retry level |
| **delete / trash** | `\uE872` | `` | Discard, remove item |
| **edit** | `\uE3C9` | `` | Rename, modify |
| **notifications** | `\uE7F4` | `` | Alerts, badge counter |
| **info** | `\uE88E` | `` | Tooltips, help popups |
| **warning** | `\uE002` | `` | Caution dialogs |
| **play_arrow** | `\uE037` | `` | Start, resume game |
| **pause** | `\uE034` | `` | Pause menu |
| **volume_up** | `\uE050` | `` | Audio settings, SFX slider |
| **volume_off** | `\uE04F` | `` | Mute toggle |

---

## Declarative Spec Reference (`UIComponentSpec`)

> [!IMPORTANT]
> **Critical field names — read before writing any spec JSON:**
> - The top-level root node field is **`"root"`** (not `"rootNode"`).
> - The background color field is **`"backgroundColorHex"`** (alias `"colorHex"` also accepted).
> - Layout config is a **nested `"layout"` object** — layout props do NOT live flat on the node.
> - The `"categories"` field is an **array** (not `"category"`), e.g. `"categories": ["Inputs"]`.

When maximum visual fidelity or complex layout hierarchies are needed, compose a `UIComponentSpec` JSON object and pass it as the `spec` argument to `build_ui_component`.

### Complete Working Example — Card with header, body, button

```json
{
  "name": "ModernCard",
  "categories": ["Cards"],
  "description": "Dark card with header row, body text, and action button.",
  "root": {
    "name": "CardRoot",
    "type": "Container",
    "width": 320,
    "height": 200,
    "backgroundColorHex": "#2D3748",
    "layout": {
      "type": "Vertical",
      "paddingTop": 16,
      "paddingBottom": 16,
      "paddingLeft": 16,
      "paddingRight": 16,
      "spacing": 12,
      "childControlWidth": true,
      "childControlHeight": false,
      "childForceExpandWidth": true,
      "childForceExpandHeight": false
    },
    "children": [
      {
        "name": "HeaderRow",
        "type": "Container",
        "height": 30,
        "layout": {
          "type": "Horizontal",
          "spacing": 8,
          "childControlWidth": false,
          "childControlHeight": true
        },
        "children": [
          {
            "name": "Icon",
            "type": "Container",
            "width": 24,
            "height": 24,
            "backgroundColorHex": "#4FD1C5"
          },
          {
            "name": "Title",
            "type": "Text",
            "text": "Card Header",
            "fontSize": 18,
            "fontStyle": "Bold",
            "textColorHex": "#FFFFFF",
            "alignment": "Left"
          }
        ]
      },
      {
        "name": "BodyText",
        "type": "Text",
        "height": 80,
        "text": "Clean, modern card component automatically baked with 256x256 thumbnail.",
        "fontSize": 14,
        "textColorHex": "#CBD5E0",
        "alignment": "Left",
        "wrap": true
      },
      {
        "name": "ActionButton",
        "type": "Button",
        "height": 40,
        "text": "Action Button",
        "normalColorHex": "#3182CE",
        "textColorHex": "#FFFFFF",
        "fontSize": 15
      }
    ]
  }
}
```

### Progress Bar Example — Fully Responsive Labeled Progress Bar

This example demonstrates how to build a progress bar where the label and value adapt fluidly to the root width without any custom scripts:

```json
{
  "name": "ProgressWithLabel",
  "categories": ["Inputs"],
  "description": "Responsive labeled progress bar at 56% fill that adapts to any root width.",
  "root": {
    "name": "ProgressWithLabel",
    "type": "Container",
    "width": 320,
    "height": 52,
    "layout": {
      "type": "Vertical",
      "spacing": 8,
      "childControlWidth": true,
      "childForceExpandWidth": true,
      "childControlHeight": false,
      "childForceExpandHeight": false,
      "childAlignment": "UpperLeft"
    },
    "children": [
      {
        "name": "LabelRow",
        "type": "Container",
        "height": 20,
        "layout": {
          "type": "Horizontal",
          "spacing": 0,
          "childControlWidth": true,
          "childForceExpandWidth": true,
          "childControlHeight": true,
          "childForceExpandHeight": false,
          "childAlignment": "MiddleLeft"
        },
        "children": [
          {
            "name": "ProgressLabel",
            "type": "Text",
            "text": "Upload progress",
            "fontSize": 14,
            "fontStyle": "Normal",
            "textColorHex": "#F1F5F9",
            "alignment": "Left",
            "flexibleWidth": 1
          },
          {
            "name": "ProgressValue",
            "type": "Text",
            "text": "56%",
            "fontSize": 14,
            "fontStyle": "Bold",
            "textColorHex": "#94A3B8",
            "alignment": "Right",
            "preferredWidth": 60,
            "flexibleWidth": 0
          }
        ]
      },
      {
        "name": "ProgressBar",
        "type": "ProgressBar",
        "height": 12,
        "backgroundColorHex": "#1E293B",
        "fillColorHex": "#6366F1",
        "value": 0.56
      }
    ]
  }
}
```

---

## Responsive UI Architecture with Native UGUI Layout Groups

> [!IMPORTANT]
> **Do NOT write custom C# scripts for responsive layouts.**
> Native UGUI provides full responsive layout capabilities out of the box using `HorizontalLayoutGroup`, `VerticalLayoutGroup`, `LayoutElement`, and `RectTransform` anchor mathematics. Always use these declarative primitives.

### Core Principles for Responsive Components

#### 1. Let the Root Layout Group Drive Child Dimensions
- Set `childControlWidth: true` and `childForceExpandWidth: true` on vertical containers (cards, modals, progress bars, input groups).
- This ensures all child rows, dividers, and progress tracks automatically stretch to 100% of the root's width when resized (e.g., in different canvas resolutions, aspect ratios, or parent containers).

#### 2. Edge-to-Edge Rows (Left Label + Right Value / Action)
For headers, labeled progress bars, key-value stats, or card titles with badges:
- **Row Container**: Set `layout.type: "Horizontal"`, `childControlWidth: true`, and `childForceExpandWidth: true`.
- **Left Element (Label / Title)**: Set `alignment: "Left"`, and `flexibleWidth: 1`. It automatically consumes all available remaining horizontal space.
- **Right Element (Value / Badge / Icon)**: Set `alignment: "Right"`, and `preferredWidth: <fixed>` with `flexibleWidth: 0`. It stays anchored to the far right edge regardless of the root width.

#### 3. Proportional Fills with Anchors (`ProgressBar`)
- The track background expands with the parent layout group.
- The inner `Fill` child uses relative anchors (`anchorMin: [0, 0]`, `anchorMax: [value, 1]`, with zero offsets). As the parent width expands from 200px to 800px, the fill bar always represents the exact percentage without any script calculation.

#### 4. Never Hardcode Static Widths on Responsive Children
- **Anti-pattern**: Giving children fixed static widths (e.g. `width: 200` and `width: 120`) inside a row with `childControlWidth: false`. If the parent expands to 600px, the children will remain stuck at 320px on the left side, leaving an awkward blank gap.
- **Best Practice**: Use `childControlWidth: true` and `flexibleWidth` so the layout group distributes space dynamically.

#### 5. Compound Interactive Rows (Toggles, Checkboxes, Radio Buttons)
- **Do not place `type: "Toggle"` on the row container itself** if the row has children (e.g. label text, badges, or shortcut icons). In UGUI, a Toggle on the container will stretch its checkmark graphic over the whole container background.
- **Proper Hierarchy**:
  - Container: `type: "Container"`, `layout: { type: "Horizontal", childControlWidth: true, childForceExpandWidth: true }`
  - Text Label: `type: "Text"`, `flexibleWidth: 1`, `alignment: "Left"`
  - Checkbox: `type: "Toggle"`, `width: 20`, `height: 20`, `flexibleWidth: 0`
  This guarantees the checkbox stays neatly pinned to the right edge at fixed 20×20 dimensions while the label expands smoothly across all remaining row space.

#### 6. Modern Framework Code (React / JSX / Vue) vs Raw HTML
- The `code` mode uses a lightweight HTML regex parser (`spec-parser.js`) intended for standard HTML/Tailwind tags (`<div class="...">`).
- For modern frameworks (React, TSX/JSX, Vue, Svelte) with custom component tags (`<Dialog>`, `<QuestionnaireItem>`, custom props), **always use the declarative `spec` path**. Translate the compound elements directly into `UIComponentSpec` JSON for pixel-perfect fidelity.

---

### Supported Node Properties

#### Dimensions & Transform
| Property | Type | Description |
|---|---|---|
| `width` | float | Width in pixels |
| `height` | float | Height in pixels |
| `anchorMin` | `[x, y]` | Anchor min (0–1) |
| `anchorMax` | `[x, y]` | Anchor max (0–1) |
| `pivot` | `[x, y]` | Pivot point |
| `anchoredPosition` | `[x, y]` | Position offset from anchor |

#### Background & Styling
| Property | Type | Description |
|---|---|---|
| `backgroundColorHex` | string | Background color. **Supports alpha**: `"#RRGGBBAA"`. Use `"#00000000"` for fully transparent/layout-only containers. |
| `colorHex` | string | Alias for `backgroundColorHex`. Both accepted. |
| `spriteResource` | string | Unity built-in sprite name (e.g. `"UISprite"`, `"Background"`) |

> [!NOTE]
> Transparent color `#00000000` (or omitting `backgroundColorHex`) creates a layout-only container with no Image component — useful for rows/columns.

#### Layout Group (nested `layout` object)
| Property | Type | Default | Description |
|---|---|---|---|
| `type` | string | `"None"` | `"None"`, `"Horizontal"`, `"Vertical"`, `"Grid"` |
| `spacing` | float | `8` | Space between children |
| `paddingTop/Bottom/Left/Right` | int | `0` | Padding in pixels |
| `childControlWidth` | bool | `true` | Layout controls child width (**essential for responsive rows/cards**) |
| `childControlHeight` | bool | `true` | Layout controls child height |
| `childForceExpandWidth` | bool | `false` | Force children to expand horizontally |
| `childForceExpandHeight` | bool | `false` | Force children to expand vertically |
| `childAlignment` | string | `"MiddleCenter"` | `"UpperLeft"`, `"MiddleLeft"`, `"MiddleCenter"`, `"MiddleRight"`, etc. |

#### LayoutElement Overrides (Native UGUI Flexible Layouts)
Set on any node that is a child of a layout group:
| Property | Type | Description |
|---|---|---|
| `flexibleWidth` | float | Relative weight to expand horizontally (`1` to consume remaining row space, `0` to prevent expansion) |
| `flexibleHeight` | float | Relative weight to expand vertically |
| `preferredWidth` | float | Preferred width before flexible distribution (e.g. `60` for a badge/label) |
| `preferredHeight` | float | Preferred height before flexible distribution |
| `minWidth` | float | Minimum allowed width |
| `minHeight` | float | Minimum allowed height |
| `ignoreLayout` | bool | If `true`, the layout group ignores this RectTransform entirely |

#### Typography (`type: "Text"`)
| Property | Type | Description |
|---|---|---|
| `text` | string | Text content |
| `fontSize` | float | Font size |
| `fontStyle` | string | `"Normal"`, `"Bold"`, `"Italic"`, `"BoldItalic"` |
| `textColorHex` | string | Text color hex |
| `alignment` | string | `"Left"`, `"Center"`, `"Right"`, `"TopLeft"`, etc. |
| `wrap` | bool | Enable text wrapping (default `true`) |

#### Node Types & Interactivity
| `type` value | Description |
|---|---|
| `"Container"` | Layout-only or background rectangle |
| `"Text"` | TextMeshPro text label |
| `"Button"` | UGUI Button with Image + ColorTint states. Uses `normalColorHex`, `highlightColorHex`, `pressedColorHex`, `text` |
| `"InputField"` | TMP InputField with placeholder. Uses `backgroundColorHex`, `placeholderText`, `placeholderColorHex` |
| `"Slider"` | UGUI Slider. Uses `sliderValue`, `sliderMinValue`, `sliderMaxValue`, `fillColorHex`, `backgroundColorHex` |
| `"Toggle"` | UGUI Toggle with checkmark. Uses `toggleState`, `checkmarkColorHex`, `backgroundColorHex` |
| `"ProgressBar"` | **New.** Auto-builds Track + Fill hierarchy. Set `value` (0–1) for fill fraction, `fillColorHex`, `backgroundColorHex` for track. No children needed. |

Shorthand boolean flags are also accepted on any node and set the type implicitly:
`isButton`, `isInputField`, `isSlider`, `isToggle`, `isProgressBar`

---

## Best Practices for Adding Components

1. **Always use `"root"` (not `"rootNode"`)** — the C# spec class field is named `root`. The builder now accepts both, but `"root"` is canonical.

2. **Always use `"categories": [...]` (array)** — not a string. Omitting it defaults to `"General"`.

3. **Use `type` explicitly on every node** — `"Container"`, `"Text"`, `"Button"`, etc. Omitting defaults to `"Container"`.

4. **Layout lives in a nested `layout` object** — do not put `spacing`, `paddingTop`, `childControlWidth`, etc. directly on the node. They must be inside `"layout": { ... }`.

5. **Transparent containers** — use no `backgroundColorHex` (omit entirely) or `"backgroundColorHex": "#00000000"` for layout rows/columns that need no visible background.

6. **Progress/fill values** — for nodes with a fill fraction (ProgressBar, Gauge), use `"value": 0.56` (range 0–1) NOT a pixel width. The builder maps this to `anchorMax.x`.

7. **Category Organization**:
   - `"Buttons"` — action buttons, icon buttons, toggle pills
   - `"Cards"` — product cards, profile cards, info containers
   - `"Inputs"` — text fields, progress bars, sliders, toggles
   - `"Badges"` — tags, status badges, price pills, notification counters
   - `"Modals"` — dialogs, confirmation popups, overlays
   - `"HUD"` — health bars, stamina gauges, player frames

8. **Spawning** — always use the CLI for spawning (more reliable timeout than MCP):
   ```bash
   node Scripts/ui-cli.js spawn --name "ComponentName"
   ```
   The MCP `spawn_ui_component` tool has a short 3s connection timeout that may fail on slow Unity responses.

9. **Fixed-Square Thumbnails**:
   - Auto-framing pads root bounds by 12% onto a 256×256 `#1E2025` backdrop.
   - For wide components, set explicit `width` on the root so the camera fits the full component.

10. **Responsive Layouts (No Custom Scripts)**:
    - Never write custom C# scripts for layout responsiveness. Use native UGUI components (`HorizontalLayoutGroup`, `VerticalLayoutGroup`, `LayoutElement`, RectTransform anchors).
    - Always ensure child rows adapt to root resizing: set `childControlWidth: true` and `childForceExpandWidth: true` on the parent layout group.
    - For edge-to-edge layouts (e.g. progress bar with label left and percentage right), set `flexibleWidth: 1` on the left element and `flexibleWidth: 0` (or fixed `preferredWidth`) on the right element. Text alignments (`Left` and `Right`) ensure content stays pinned to the outer edges.

---

## Cross-Project Library Architecture (Side-by-Side Local Package)

When reusing and expanding this UI component library across multiple Unity projects, use the **Side-by-Side Local Package workflow** to allow real-time component sharing without Git submodule complexity.

### 1. Directory Structure

Place the central UI Library repository side-by-side with your game projects:

```
D:/Git/
├── GameProjectA/              <-- Own Git repo (Game A)
│   └── Packages/manifest.json
├── GameProjectB/              <-- Own Git repo (Game B)
│   └── Packages/manifest.json
└── ui-quality-life/           <-- Central UI Library Git repo
    ├── package.json           <-- Package definition
    ├── Presets/               <-- Shared library prefabs, assets, thumbnails
    ├── Editor/                <-- Shelf window, builder engine, bridge server
    └── .agents/               <-- MCP server, skills, and pipeline rules
```

### 2. Linking via `Packages/manifest.json`

In any consumer game project, add a relative local link inside `Packages/manifest.json`:

```json
"dependencies": {
  "com.ngoda.ui-quality-life": "file:../../ui-quality-life"
}
```

* **Writable by default:** Unlike Git URL packages which Unity locks into read-only `Library/PackageCache/`, local `file:` packages remain fully editable.
* **Instant propagation:** Any new components created via the pipeline while working in `GameProjectA` are saved directly to `ui-quality-life/Presets/`. `GameProjectB` immediately sees them on the UI Shelf on next focus or asset refresh.

### 3. Git Operations & Remote Isolation

Because the repositories are side-by-side rather than nested:

* **Game Project (`GameProjectA/`):**
  ```bash
  cd D:/Git/GameProjectA
  git remote -v       # Points to GameProjectA remote
  git add .           # Stages ONLY GameProjectA files (ignores the package)
  git commit -m "Update gameplay features"
  git push origin main
  ```
* **UI Component Library (`ui-quality-life/`):**
  ```bash
  cd D:/Git/ui-quality-life
  git remote -v       # Points to ui-quality-life remote
  git add .           # Stages ONLY new presets, thumbnails, and pipeline improvements
  git commit -m "Add new BadgeSpinner variants"
  git push origin main
  ```

### 4. Hybrid Library Workflow (Global vs Game-Specific)

The UI Quality of Life Shelf discovers presets using:
```csharp
AssetDatabase.FindAssets("t:UIPresetItem");
```
This automatically scans **both `Packages/` AND `Assets/`** simultaneously:
* **Global Components:** Saved to `ui-quality-life/Presets/` (Buttons, Modals, Badges, Input fields, Spinners) — accessible to every project using the package.
* **Game-Specific Components:** Saved to the game's local `Assets/` folder (e.g. specialized HUDs, inventory grids) — kept private to that specific game.

