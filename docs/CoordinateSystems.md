# Sprite Rig Studio — Coordinate Systems

## Domain Coordinates (Character Space)

**Used in:** All domain geometry types (`Vector2D`, `Matrix3x2D`, `Transform2D`, `RectangleD`), skeleton definitions, character rigs, animation keyframes.

- **Origin:** Arbitrary; each character rig defines its own.
- **X axis:** Increases to the right.
- **Y axis:** Increases upward.
- **Rotation:** Positive = counterclockwise.
- **Units:** Pixels (double precision).

Source references:
- `Vector2D.cs`: `"Domain coordinates: Y increases upward."`
- `Transform2D.cs`: `"Domain coordinates: Y increases upward. Positive rotation is counterclockwise."`
- `TransformComposition.cs`: `"Domain coordinates: Y increases upward. Positive rotation is counterclockwise."`

## Source Image Pixel Space

**Used in:** Source artwork images, `SpritePartDefinition.SourceRectangle`, `SpritePartDefinition.Pivot`.

- **Origin:** Top-left corner of the image.
- **X axis:** Increases to the right (same as domain).
- **Y axis:** Increases downward (opposite to domain).
- **Units:** Pixels (integer for raster images).

## Export Frame Pixel Space

**Used in:** `ExportProfile.FrameWidth`, `ExportProfile.FrameHeight`, `ExportProfile.AnchorPixel`, rendered frame byte arrays, spritesheet pixel data.

- **Origin:** Top-left corner of the frame.
- **X axis:** Increases to the right.
- **Y axis:** Increases downward.
- **Units:** Pixels (integer).

## Transform Multiplication Order: TRS

All transforms follow **Translate * Rotate * Scale** order:

```csharp
// Transform2D.ToMatrix()
var t = Matrix3x2D.CreateTranslation(Position);
var r = Matrix3x2D.CreateRotation(RotationDegrees);
var s = Matrix3x2D.CreateScale(Scale.X, Scale.Y);
return t * r * s;
```

Scale is applied first (local to origin), then rotation (around origin), then translation (to final position).

## Coordinate Conversion

### Domain to Export Frame (Y Flip)

The export frame transform maps the character's ground anchor to the configured anchor pixel:

```csharp
offsetX = anchorPixel.X - groundAnchor.X;
offsetY = anchorPixel.Y + groundAnchor.Y;  // Y flip
```

A positive `groundAnchor.Y` (ground below origin in domain) adds a positive offset in export frame space, pushing the character up visually.

### Source Image to Domain (Pivot Handling)

Part pivot is in source-image space (Y-down). When computing the part local matrix:

```csharp
pivotTranslation = Translate(-part.Pivot);
localTransform = localSetupTransform * pivotTranslation;
```

## Transform Composition Formula

```
PartWorld = ExportFrameToWorld
          * CharacterSetupRoot
          * BoneSetupWorld
          * BoneAnimationDelta
          * CharacterCorrection
          * PartLocalTransform
```

Defined in `TransformComposition.ComputePartWorldTransform()`.

## Rounding Policies

- `CharacterSetupTransform.SnapToPixels`: snaps rig position to integer pixels when enabled.
- `PositionSnappingMode` (export): None, PartOrigin, FinalAnchor, or PixelGrid.
- `InterpolationMode`: Step, Linear, or Smooth.
- `SamplingMode`: NearestNeighbor or Bilinear.

## Summary

```
Source Image (Y-down) -> Pivot -> Domain (Y-up, CCW)
                                     |
                                     v ExportFrameToWorld (Y flip)
Export Frame (Y-down, top-left) -> PNG -> Spritesheet
```
