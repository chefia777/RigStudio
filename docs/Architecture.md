# Sprite Rig Studio — Architecture

## Dependency Direction

The solution follows a strict layered architecture. All dependency arrows point inward toward the Domain layer.

```
Domain
  ^
  ├── Application
  ├── Rendering
  ^
  ├── Infrastructure  (references Domain, Application, Rendering)
  ^
  ├── Desktop         (references Domain, Application, Rendering, Infrastructure)
  └── CLI             (references Application, Rendering, Infrastructure)
```

### Layer Responsibilities

| Layer | Project | Role |
|---|---|---|
| Domain | `SpriteRigStudio.Domain` | Pure business logic, entities, value objects, geometry, transforms, enums. Zero external dependencies beyond .NET runtime. |
| Application | `SpriteRigStudio.Application` | Use-case orchestration, services, abstraction interfaces. Depends only on Domain. |
| Rendering | `SpriteRigStudio.Rendering` | SkiaSharp-based frame rendering, pose evaluation, mask rasterization, spritesheet composition. Depends only on Domain. |
| Infrastructure | `SpriteRigStudio.Infrastructure` | File system, persistence, serialization, hashing, logging, migrations. Depends on Domain, Application, and Rendering. |
| Desktop | `SpriteRigStudio.Desktop` | Avalonia UI (MVVM), composition root, view models, tools, viewport. Depends on all lower layers. |
| CLI | `SpriteRigStudio.Cli` | Command-line interface for batch export and validation. Depends on Application, Rendering, and Infrastructure. |

## Transform Composition Convention

The single authoritative formula for computing a sprite part's final world transform:

```
PartWorld = ExportFrameToWorld
          * CharacterSetupRoot
          * BoneSetupWorld
          * BoneAnimationDelta
          * CharacterCorrection
          * PartLocalTransform
```

All transforms follow **Translate * Rotate * Scale (TRS)** order. See `docs/CoordinateSystems.md` for details.

## MVVM Pattern (Desktop)

The Desktop layer uses Model-View-ViewModel with Avalonia UI and ReactiveUI.

- **Views** (XAML + code-behind) bind to ViewModel properties and commands.
- **ViewModels** expose observable state and coordinate application services.
- **Application Services** execute use cases and remain independent of Avalonia.

The composition root is `DesktopBootstrapper.BuildServiceProvider()` which registers all dependencies via `Microsoft.Extensions.DependencyInjection`.

## Retargeting Model

Retargeting allows one reusable animation to drive multiple characters with different proportions:

1. Rotation delta: `delta = animatedCanonicalRotation - canonicalRestRotation`
2. Applied: `characterLocalRotation = characterSetupRotation + delta + correction`
3. Translation scaling: values scaled by bone length or height ratios per `TranslationMode`.

## Export Pipeline

See `docs/ExportWorkflow.md` for the complete export pipeline documentation.
