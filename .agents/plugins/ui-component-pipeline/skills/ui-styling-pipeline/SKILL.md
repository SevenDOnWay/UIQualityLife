---
name: ui-styling-pipeline
description: Convert web design tokens, CSS variables, Tailwind color palettes, and CSS animations/keyframes into native Unity UIThemePalette and UIAnimationClass assets. Use whenever the user pastes web styling snippets, CSS transitions, color hexes, or asks to generate themes and animation classes for the UI shelf.
---

# UI Styling Pipeline Skill

This skill guides you through converting web color palettes, design tokens (CSS variables, Tailwind colors, hex lists), and CSS-like animations (hover/press transitions, `@keyframes`, springs) into native Unity assets for `UIQualityLife`.

---

## 1. When to Use This Skill

Activate this workflow whenever the user:
- Pastes CSS `:root` variables, Tailwind color objects, or hex color lists and wants a Unity Color Palette / Theme.
- Pastes CSS transition snippets (`.btn:hover { transform: scale(1.05); }`), CSS `@keyframes` (pulse, float/bobbing, shimmer, shake), or micro-interaction descriptions and wants a Unity UI Animation Class.
- Asks to bundle a theme color, corner radius, and interactive animation into a reusable `UIStyleClass`.
- Wants the newly created theme or animation to appear immediately in the **UI Theme Studio & Presets shelf** (`UIQualityLife/Theme Studio & Presets`).

---

## 2. Directory Structure Convention

All generated styling assets MUST strictly follow the self-contained per-component directory structure:
- **Themes**: `Assets/_Project/Styling/Themes/<ThemeName>/<ThemeName>.asset`
  - Thumbnail: `<ThemeName>_thumb.png` (8×8 dynamic square swatch grid up to 64 colors on `#1E2025` backdrop).
- **Animations**: `Assets/_Project/Styling/Animations/<AnimName>/<AnimName>.asset`
  - Thumbnail: `<AnimName>_thumb.png` (Waveform motion curve icon).
- **Style Classes**: `Assets/_Project/Styling/Styles/<StyleName>/<StyleName>.asset`

---

## 3. Parametric Color Architecture & Contrast Guardrails

To prevent illegible or clashing color combinations, all generated themes and style classes must adhere to these mathematical rules (detailed in `Docs/PARAMETRIC_COLOR_ARCHITECTURE.md`):

1. **60-30-10 Color Weight Distribution**:
   - **60% Dominant (Backdrops / Screen Backgrounds)**: Low saturation ($S < 15\%$), dark surface tone ($V \in [10\%, 22\%]$) or clean light tone ($V \in [90\%, 98\%]$).
   - **30% Secondary (Cards / Panels / Inactive Tabs)**: Mid-low saturation ($S \in [15\%, 35\%]$) with $+8\%\dots+12\%$ brightness step above background.
   - **10% Accent (CTA Buttons / Active Status / Highlights)**: High saturation ($S \in [70\%, 90\%]$), high brightness ($V \in [80\%, 95\%]$).
2. **Strict HSV Rules**:
   - **Ban pure extremes**: Never combine $S = 100\%$ with $V = 100\%$ (harsh neon).
   - **Ban raw black & white**: Replace `#000000` with dark surface tones ($V \in [8\%, 15\%]$) and `#FFFFFF` with off-whites ($V \in [92\%, 98\%]$).
   - **Tinted Neutrals**: Container and border tones should inherit the primary anchor hue ($S \in [4\%, 12\%]$).
3. **Contrast & Readability (WCAG)**:
   - Ensure a minimum contrast ratio $\ge 4.5:1$ between text/icons and their background surfaces ($L = 0.2126R + 0.7152G + 0.0722B$).
   - Interactive states must be calculated deterministically:
     - $\text{Hover} = (H, \; \max(0, S - 0.05), \; \min(1, V + 0.10))$
     - $\text{Pressed} = (H, \; \min(1, S + 0.05), \; \max(0, V - 0.12))$
     - $\text{Disabled} = (H, \; S \times 0.25, \; V \times 0.60)$ with $\alpha = 0.50$.

---

## 4. How to Create Theme Palettes

### Option A: Using MCP Tool `build_ui_theme` (Preferred)
```json
{
  "name": "CyberNeon",
  "description": "High-contrast cyberpunk palette",
  "code": ":root { --primary: #00f0ff; --surface: #0a0a12; --accent: #ff007f; --text-main: #ffffff; --border: #1f2030; }"
}
```
Or with comma/space-separated color assignments:
```json
{
  "name": "OceanBreeze",
  "colors": "primary=#0284c7,secondary=#38bdf8,surface=#0f172a,accent=#34d399,text-main=#f8fafc"
}
```

### Option B: Using the CLI
```bash
node Scripts/ui-cli.js theme --name "CyberNeon" --code ":root { --primary: #00f0ff; --surface: #0a0a12; --accent: #ff007f; }"
# Or
node Scripts/ui-cli.js theme --name "OceanBreeze" --colors "primary=#0284c7,surface=#0f172a,accent=#34d399"
```

---

## 4. How to Create UI Animation Classes

### Option A: Using MCP Tool `build_ui_animation` (Preferred)
From CSS hover/active rules:
```json
{
  "name": "CardHoverBounce",
  "description": "Elevates and scales card on hover with quick press reaction",
  "code": ".card:hover { transform: scale(1.05) translateY(-4px); transition: all 0.2s cubic-bezier(0.34, 1.56, 0.64, 1); } .card:active { transform: scale(0.96); }"
}
```
From continuous `@keyframes` or loop types:
```json
{
  "name": "PulseAttention",
  "description": "Smooth breathing scale loop for attention badges",
  "loopType": "Pulse",
  "loopSpeed": 1.8,
  "loopMagnitude": 0.08
}
```
Supported loop types: `None`, `Pulse`, `Float`, `Shimmer`, `Shake`.

### Option B: Using the CLI
```bash
# Hover & Press micro-interaction
node Scripts/ui-cli.js animation --name "CardHoverBounce" --code ".card:hover { transform: scale(1.05) translateY(-4px); } .card:active { transform: scale(0.96); }"

# Continuous pulsing badge
node Scripts/ui-cli.js animation --name "PulseAttention" --loop Pulse --speed 1.8 --magnitude 0.08
```

---

## 5. How to Create Compound Style Classes

### Option A: Using MCP Tool `build_ui_style_class`
```json
{
  "name": "Btn_Primary_Style",
  "themeToken": "primary",
  "borderColorToken": "border",
  "radiusToken": "radius-md",
  "animationClassName": "CardHoverBounce",
  "description": "Interactive primary button styling"
}
```

### Option B: Using the CLI
```bash
node Scripts/ui-cli.js style --name "Btn_Primary_Style" --theme-token primary --anim-class CardHoverBounce
```

---

## 6. How the Pipeline Processes Requests

1. **Official Unity CLI**: Tries executing via `unity.exe command eval` if the Unity session is hooked.
2. **Fast Local HTTP Bridge**: If the Unity Editor is open, sends a `POST` request to `http://127.0.0.1:8765/api/build-theme`, `/api/build-animation`, or `/api/build-style`.
3. **Zero-Dependency File Queue**: If Unity is compiling or busy, safely drops a timestamped file into `Assets/_Project/Pipeline/Queue/` which is picked up and processed automatically by `UIEditorBridgeServer.cs`.
4. Assets and thumbnails are written directly into `Assets/_Project/Styling/` and immediately indexed into the UI shelf.
