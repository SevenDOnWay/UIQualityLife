# Deploying the UIQualityLife Interactive Demo to itch.io

This guide outlines the steps to build, package, and upload the **UIQualityLife** interactive showcase demo to [itch.io](https://itch.io) using Unity WebGL.

---

## 🚀 1. Building the Demo

You can build the demo directly from inside Unity or package existing builds via the CLI.

### Option A: From Unity Editor (Recommended)
1. Open the project in **Unity 6**.
2. In the top menu, navigate to:
   **Tools** → **UI Quality of Life** → **Build itch.io WebGL Demo**.
3. Unity will automatically:
   - Synchronize all 38 presets, themes (`GameModernDark`, `UITheme_ModernDark`, `CyberNeon`), and atomic animation profiles.
   - Configure **Decompression Fallback** to enabled.
   - Set WebGL compression to **Gzip** with 1280×720 viewport defaults.
   - Build `Assets/Scenes/DemoScene.unity` into `Builds/WebGL/`.
   - Compress the build into an itch.io-compliant zip archive at `Builds/UIQualityLife-itch-webgl.zip`.

### Option B: Package with CLI
If you already built WebGL to `Builds/WebGL/`:
```bash
node Scripts/ui-cli.js package-itch
# or
npm run package:itch
```

---

## 📦 2. itch.io Project Configuration

Follow these settings when creating or updating your project on [itch.io](https://itch.io):

| Setting | Value | Notes |
|---|---|---|
| **Classification** | **Tool** / **Game Asset** / **Interactive Demo** | Categorizes your project appropriately |
| **Kind of project** | **HTML** | Must be set to HTML so itch.io can embed the browser build |
| **Uploads** | Upload `Builds/UIQualityLife-itch-webgl.zip` | Check **"This file will be played in the browser"** |
| **Viewport Dimensions** | **1280** width × **720** height | Standard 16:9 HD canvas |
| **Embed options** | Check **Fullscreen button** | Allows users to expand the demo to full screen |
| **Execution** | Check **Automatically start on page load** | Improves visitor experience |
| **Mobile Friendly** | Optional | Recommended if touch controls are verified |

---

## ⚡ 3. Why Decompression Fallback is Enabled

itch.io's web servers do not always serve `.br` or `.gz` files with the proper HTTP `Content-Encoding` response headers.

By enabling **Decompression Fallback (`webGLDecompressionFallback: 1`)**:
- Unity embeds a lightweight JavaScript decompressor inside the WebGL loader.
- If the browser or server fails to decompress Gzip/Brotli streams natively, the fallback decompressor automatically runs in JavaScript.
- This prevents `HTTP 404 / 403 / Content-Encoding mismatch` load errors on itch.io.

---

## 🎮 4. Demo Features Included

- **Preset Showcase (38 Components)**:
  - **Buttons**: Primary, Secondary, Success, Danger, Ghost, FAB, Pill, Icon, IconText, Loading.
  - **Inputs**: SliderWithInput, InputField, InputGroupInlineEnd, ProgressWithLabel.
  - **Cards**: AccordionFoldout, UniversalItemSlot, QuestionnaireMultiple.
  - **HUD**: HealthBar, XPLevelBar, AbilitySlot, ManaBar, MinimapFrame, AmmoCounter, BossHealthBar, ScoreCounter.
  - **Modals**: ToastItem, TooltipBox, AsyncBlocker, Confirmation, PauseMenu, LevelComplete, RewardPopup, SettingsTabs.
  - **Badges**: Spinner (Primary, Secondary, Outline).
- **Theme Switcher**: Instant switching between `GameModernDark`, `UITheme_ModernDark`, and `CyberNeon` with live color swatch token inspector.
- **Atomic Animation Lab**: Multi-channel motion testing for `CardFlip_Y`, `ModalSlidePop`, `PendulumSwing`, `RotateScaleDown`, `RotateScaleUp`, `ScaleUpCenter`, `BadgePulse`, `ButtonBounce`, `CardHoverLift` with interactive hover, press, click bounce physics, and loops.
- **Procedural Shape Studio**: Dynamic sliders for 4-corner radii, border widths, drop shadows, and linear gradient angles with live C# & CSS code generator.
- **Fullscreen Mode & Diagnostics**: Real-time FPS, screen resolution readout, and fullscreen toggle for itch.io iframe.
