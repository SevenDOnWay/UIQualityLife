# Contributing to UIQualityLife

Thank you for your interest in contributing to **UIQualityLife**! We welcome community contributions to help improve the UI prototyping workflows, theming engines, and automation tools for Unity 6.

---

## 🌟 Code of Conduct

By participating in this project, you agree to abide by our [Code of Conduct](CODE_OF_CONDUCT.md). Please treat all community members with respect and courtesy.

---

## 🚀 How to Contribute

### 1. Reporting Bugs
- Check the [Issues](https://github.com/SevenDOnWay/UIQualityLife/issues) tab to ensure the bug hasn't already been reported.
- Open a new issue with a clear title and description.
- Include your Unity version (e.g., Unity 6000.0.x), operating system, steps to reproduce, and relevant console log excerpts.

### 2. Suggesting Features & Enhancements
- Open a feature request issue explaining the motivation, use case, and proposed API or workflow.
- If you'd like to implement it yourself, please comment on the issue first so we can coordinate design direction.

### 3. Submitting Pull Requests
1. **Fork** the repository and create your branch from `main`:
   ```bash
   git checkout -b feature/my-new-feature
   ```
2. **Follow Code Conventions**:
   - Adhere to standard Unity C# naming conventions (PascalCase for classes/methods, camelCase for local variables, `_camelCase` or `m_` for private fields).
   - Follow SOLID & OOP principles; ensure presets remain self-contained.
   - For scripting utilities (`Scripts/`), adhere to clean modern JavaScript (ES6+ / Node.js).
   - Ensure you do not commit unintended `.meta` file modifications or personal user settings (`UserSettings/`, `Library/`).
3. **Commit Messages**:
   - Use [Conventional Commits](https://www.conventionalcommits.org/) (e.g., `feat:`, `fix:`, `docs:`, `refactor:`, `chore:`).
4. **Test Your Changes**:
   - Ensure the Unity project opens cleanly with no compilation errors or broken references.
   - Test CLI scripts (`node Scripts/ui-cli.js status`) if modifying pipeline bridges.
5. **Push and Open a PR**:
   - Push your branch to GitHub and submit a Pull Request targeting `main`.
   - Provide a clear summary of changes and reference any linked issues.

---

## 📁 Repository Structure & Guidelines

- **Presets & Components**: Must reside in self-contained directories under `Assets/_Project/Presets/<Category>/<ComponentName>/`.
- **Styling**: Themes, animation states, and style classes reside under `Assets/_Project/Styling/`.
- **Thumbnails**: Prefabs and theme palettes must generate self-contained previews without triggering full-library batch bakes.
