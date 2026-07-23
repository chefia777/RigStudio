# Sprite Rig Studio

A standalone desktop application for creating skeletal sprite rigs, authoring reusable animations, and exporting consistently aligned spritesheets.

## Overview

Sprite Rig Studio lets you:

- Import character sprites (flattened PNG or separated body parts)
- Position a reusable skeletal rig over any character
- Move, rotate, scale, and mirror the complete rig
- Place and adjust joints (head, torso, arms, legs, and more)
- Add custom bones and attachment points
- Define body parts and bind them to bones
- Create reusable skeletal animations
- Apply animations to multiple character rigs with different proportions
- Apply character-specific correction offsets
- Preview animations in real time
- Export one spritesheet per animation (or batch-export all)
- Export individual frames and metadata (frame, anchors, pivots, FPS, looping)

## Requirements

- Windows 10 or later (64-bit)
- No game engine, runtime, or development tools required

## Quick Start

1. Download the latest release from the Releases page.
2. Extract the archive to a folder of your choice.
3. Run `SpriteRigStudio.exe`.
4. Create a new project and start rigging!

## Documentation

See the [docs](./docs) folder for user and developer documentation.

## Building from Source

### Prerequisites

- .NET 8.0 SDK (8.0.400 or later)

### Build

```powershell
.\scripts\build.ps1
```

### Test

```powershell
.\scripts\test.ps1
```

### Publish

```powershell
.\scripts\publish-windows.ps1
```

## License

See [LICENSE](./LICENSE).
