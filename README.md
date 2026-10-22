# UIQualityLife: Modern UI Pipeline & Styling Toolkit for Unity 6

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Unity 6 Ready](https://img.shields.io/badge/Unity-6000.0%2B-black?logo=unity)](https://unity.com/)
[![CI](https://github.com/SevenDOnWay/UIQualityLife/actions/workflows/ci.yml/badge.svg)](.github/workflows/ci.yml)
[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg)](CONTRIBUTING.md)

**UIQualityLife** is a rapid UI prototyping, preset management, and CSS-like styling and animation pipeline built specifically for **Unity 6**. It bridges the gap between modern web UI design workflows (Tailwind CSS, design tokens, CSS transitions) and native Unity UGUI / UI Toolkit workflows with first-class AI and CLI automation support.

---

## 💡 Why UIQualityLife?

Building and maintaining game UI in Unity is traditionally slow, repetitive, and fragmented across disparate prefabs and hardcoded colors. UIQualityLife transforms your workflow:

- ⏱️ **Slash Prototyping Time from Hours to Seconds**: Instantly mock up clean, polished game interfaces without getting bogged down in manual hierarchy assembly and RectTransform alignment.
- 📦 **Ready-to-Use Component Shelf**: Access a growing library of production-ready UI widgets, interactive animations, and responsive color palettes with a single click or drag-and-drop into your active scene Canvas.
- 🎒 **Build Your Portable UI Library**: Save and catalog your custom UI prefabs into an organized, self-contained library. Carry your entire design system to your next game project effortlessly via local package sharing.
- 🎯 **CSS-like Centralized Styling — "Change Once, Update Everywhere"**: Decouple styling from prefab hierarchies. Adjust a color token, corner radius, or easing curve in a single theme asset, and watch changes cascade across every screen in your project.
- 🤖 **Agentic AI & MCP Automation**: Features a native Model Context Protocol (MCP) server and CLI. AI coding agents (such as Codex or Claude) can directly read web HTML/CSS/Tailwind snippets or React designs and convert them into native, fully configured Unity UI prefabs.

---

## ✨ Key Features

- **🎨 CSS-like Theme Palettes & Design Tokens**: Define color palettes, typography, and styling variables using familiar CSS/Tailwind-inspired token systems, complete with auto-generated 8×8 visual swatch thumbnails.
- **✨ Stateful UI Animations**: Visual State Machine (`UIVisualState`) with built-in zero-dependency easing functions matching standard CSS Penner curves. Supports hover, press, focus, disabled states, click bounce, and ambient loops.
- **⚡ AI & CLI Automation (MCP Ready)**: Ships with a command-line interface (`ui-cli.js`) and Model Context Protocol (MCP) server plugins, enabling AI coding agents (such as Codex) to translate UI code, web snippets, or visual specs directly into Unity prefabs and ScriptableObjects.
- **🧩 Self-Contained Preset Architecture**: Organized modular preset library for buttons, cards, modals, badges, and inputs, featuring isolated component folders with automated 256×256 thumbnail bakes.
- **🌐 Interactive WebGL Demo Ready**: Pre-configured DemoScene and automated builder to export itch.io-ready WebGL packages with Decompression Fallback enabled.
- **🔄 Dual-Mode Editor Bridge**: Seamless communication via a local HTTP bridge server (`http://127.0.0.1:8765/`) or an asynchronous file-based queue (`Assets/_Project/Pipeline/Queue/`), allowing headless and background UI creation without locking the editor.

---

## 🎮 Interactive itch.io WebGL Demo

To build and package the interactive WebGL demo for itch.io:
1. Open the project in **Unity 6** and navigate to **Tools > UI Quality of Life > Build itch.io WebGL Demo**.
2. Upload Builds/UIQualityLife-itch-webgl.zip directly to itch.io (with *This file will be played in the browser* enabled).

See the full [itch.io Deployment Guide](DOCS/ITCH_DEPLOYMENT.md) for detailed configuration.

---

## 🚀 Getting Started

### Prerequisites

- **Unity 6 (6000.0.x or later)**
- **TextMeshPro** / UGUI
- **Node.js (v18+)** *(optional, for running CLI scripts and MCP tooling)*

### Installation

#### As a Standalone Repository
1. Clone the repository:
   ```bash
   git clone https://github.com/SevenDOnWay/UIQualityLife.git
   ```
2. Open the project folder in Unity Hub using **Unity 6**.

#### As a Local Shared Package
For multi-project workflows, consume this library in your game project's `Packages/manifest.json`:
```json
{
  "dependencies": {
    "com.sevendonway.uiqualitylife": "file:../../UIQualityLife"
  }
}
```

---

## 🛠️ CLI & Automation Workflow

The pipeline includes a Node-based CLI that can be invoked manually or driven by AI agents:

```bash
# Check status of the Unity Editor Bridge
node Scripts/ui-cli.js status

# List available presets and tags in the library
node Scripts/ui-cli.js list

# Build a UI component from a design spec or code snippet
node Scripts/ui-cli.js build --name "ActionButton" --category "Buttons" --code "<button class='bg-blue-500 rounded-lg'>Click</button>"

# Create a theme palette from CSS variables or Tailwind tokens
node Scripts/ui-cli.js theme --name "CyberNeon" --code ":root { --primary: #00ffcc; --bg: #0d1117; }"

# Create an animation profile from CSS transition rules
node Scripts/ui-cli.js animation --name "ButtonBounce" --code "transform: scale(0.95); transition: transform 0.15s ease-out;"

# Spawn a component directly into the active scene Canvas
node Scripts/ui-cli.js spawn "ActionButton"
```

---

## 📁 Project Structure

```
UIQualityLife/
├── .agents/plugins/ui-component-pipeline/   # MCP server & agent skills for AI integration
├── Assets/
│   └── _Project/
│       ├── Editor/                          # Core pipeline engines, thumbnail bakers, bridge server
│       ├── Runtime/                         # Easing curves, state machines, styling runners
│       ├── Presets/                         # UI components (Buttons, Cards, Inputs, Badges)
│       └── Styling/                         # Themes, Animations, and Style Classes
├── Scripts/
│   └── ui-cli.js                            # Automation CLI for external bridges & AI agents
└── .github/                                 # CI/CD workflows and community templates
```

---

## 🗺️ Roadmap & Next Targets

We are actively expanding UIQualityLife to become the standard design-to-engine pipeline for Unity developers. Upcoming milestones:

- [ ] **🎨 Figma & Design Tool Synchronization**: Direct import and two-way sync with Figma frames, design tokens, and vector assets directly into Unity UI assets.
- [ ] **🧩 100+ Production-Ready UI Components**: Expanding the library to over 100 modular presets, including complex HUD bars, inventory matrices, skill trees, dialog windows, notifications, and responsive sliders.
- [ ] **📐 15+ Complete Game UI Templates**: Out-of-the-box, genre-specific interface packs (RPG, FPS/Tactical, Mobile Casual, Cyberpunk/Sci-Fi, and Cozy Fantasy).
- [ ] **🛠️ Native UI Builder & UI Toolkit Support**: Full cross-compatibility with Unity's UI Builder, generating both UXML hierarchy and USS style classes alongside standard UGUI prefabs.
- [ ] **⚡ Extended Framework Importers**: Support for directly importing React UI snippets, Tailwind config files, and Shadcn UI component primitives via MCP.

---

## 🤝 Contributing

Contributions are welcome! Please read our [Contributing Guidelines](CONTRIBUTING.md) and [Code of Conduct](CODE_OF_CONDUCT.md) before submitting pull requests.

---

## 🛡️ Security

If you discover a security vulnerability within UIQualityLife, please review our [Security Policy](SECURITY.md) for disclosure instructions.

---

## 📄 License

This project is open-source software licensed under the [MIT License](LICENSE).
