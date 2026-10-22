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

- **Presets Showcase**: Interactive buttons, badges, inputs, progress bars, and questionnaire cards.
- **Theme Switcher**: Instant switching between `UITheme_ModernDark` and `CyberNeon` with live swatch inspector.
- **Procedural Shape Studio**: Dynamic sliders for 4-corner radii, borders, drop shadows, and linear gradients.
- **Animation Lab**: Visual state testing for hover, press, click bounce, and ambient loops.
- **Fullscreen Mode**: Direct fullscreen toggle for browser viewports.
