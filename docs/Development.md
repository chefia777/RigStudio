# Sprite Rig Studio — Development Guide

## Prerequisites

- .NET 8.0 SDK (8.0.400 or later)
- Windows 10+ (for desktop app; CLI may work cross-platform)
- PowerShell 5.1+ (for build scripts)

## Quick Start

```powershell
# Build everything
.\scripts\build.ps1

# Run tests
.\scripts\test.ps1

# Run desktop app
dotnet run --project src\SpriteRigStudio.Desktop

# Run CLI
dotnet run --project src\SpriteRigStudio.Cli
```

## Solution Structure

The solution contains 12 projects in 3 groups:

### Source (src/)
| Project | Description |
|---|---|
| `SpriteRigStudio.Domain` | Core domain: entities, value objects, geometry, validation |
| `SpriteRigStudio.Application` | Use-case services, abstraction interfaces |
| `SpriteRigStudio.Rendering` | SkiaSharp-backed pose evaluation, compositing, encoding |
| `SpriteRigStudio.Infrastructure` | File system, persistence, serialization, hashing |
| `SpriteRigStudio.Desktop` | Avalonia UI, MVVM, viewport, tools |
| `SpriteRigStudio.Cli` | Command-line interface |

### Tests (tests/)
| Project | Description |
|---|---|
| `Domain.Tests` | 69 unit tests covering all domain logic |
| `Integration.Tests` | 1 full workflow test (create/save/load/export) |
| `Application.Tests` | (placeholder) |
| `Rendering.Tests` | (placeholder) |
| `Infrastructure.Tests` | (placeholder) |
| `Desktop.Tests` | (placeholder) |

## Dependency Direction

```
Domain ← Application ← Desktop
Domain ← Rendering ← Infrastructure → Desktop
                    ↗
              CLI ←───
```

No layer may reference a layer above it.

## Adding a Feature

1. Define domain types in the appropriate Domain folder
2. Add application service in Application layer
3. Add rendering logic in Rendering layer (if visual)
4. Add persistence in Infrastructure layer
5. Add tests in the matching test project
6. Wire the UI in Desktop layer

## Testing

```powershell
# Run all tests
dotnet test

# Run specific test project
dotnet test tests\SpriteRigStudio.Domain.Tests

# Run with verbose output
dotnet test -v n
```

## Publishing

```powershell
.\scripts\publish-windows.ps1
```

Output: `artifacts/publish/windows-x64/` — self-contained, no runtime required.
