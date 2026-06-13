# Tech Stack

## Engine & Version

- **Unity 6** (Editor version `6000.4.10f1`)
- **Render Pipeline**: Universal Render Pipeline (URP) 17.4.0 with 2D Renderer
- **Scripting**: C# (.NET / IL2CPP backend for Android)
- **Template**: `com.unity.template.universal-2d@6.1.3`

## Key Packages

| Package | Version | Purpose |
|---------|---------|---------|
| Input System | 1.19.0 | Player input handling (new system) |
| 2D Animation | 14.0.4 | Skeletal animation for sprites |
| 2D Aseprite | 4.0.2 | Import Aseprite files directly |
| 2D Sprite Shape | 14.0.1 | Freeform 2D geometry |
| 2D Tilemap + Extras | 1.0.0 / 7.0.1 | Tile-based level design |
| 2D PSD Importer | 13.0.3 | Import layered PSD/PSB files |
| Timeline | 1.8.12 | Cutscenes and sequenced events |
| uGUI | 2.0.0 | UI system |
| Visual Scripting | 1.9.11 | Node-based logic (if used) |
| Unity MCP (CoplayDev) | git | MCP integration for AI tooling |

## IDE Support

- JetBrains Rider (`com.unity.ide.rider`)
- Visual Studio (`com.unity.ide.visualstudio`)

## Build & Commands

There are no CLI build scripts configured. All builds are done through the Unity Editor:

- **Open project**: Open in Unity Hub or Unity Editor 6000.4.10f1
- **Build**: File → Build Settings → Build
- **Play**: Press Play in the Unity Editor or Ctrl+P
- **Tests**: Window → General → Test Runner (uses `com.unity.test-framework` 1.6.0)

## Scripting Conventions

- Active Input Handler: New Input System only (`activeInputHandler: 1`)
- Allow Unsafe Code: disabled
- Deterministic Compilation: enabled
- Incremental GC: enabled
- Color Space: Linear (`m_ActiveColorSpace: 1`)
