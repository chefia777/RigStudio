# Sprite Rig Studio — Export Workflow

## Frame Sampling Formula

Each frame samples the animation at a specific time:

```
sampleTime = frameIndex / exportFps
```

- `frameIndex` starts at 0.
- `exportFps` is from `ExportProfile.FrameRate` (if 0, uses animation's `FramesPerSecond`).

### Frame Count

```csharp
// From AnimationService.GetSampledFrameCount()
var frameCount = (int)Math.Ceiling(duration * fps);

// Loop/PingPong: sample [0, duration) — no duplicate endpoint
// Once/Clamp: sample [0, duration] — includes endpoint
```

| LoopMode | Range | Count |
|---|---|---|
| Loop | `[0, duration)` | `ceil(d * fps)` |
| PingPong | `[0, duration)` | `ceil(d * fps)` |
| Once | `[0, duration]` | `ceil(d * fps) + 1` |
| Clamp | `[0, duration]` | `ceil(d * fps) + 1` |

## Ground-Anchor Enforcement

Every exported frame aligns the character's ground anchor to the configured export anchor pixel:

```csharp
offsetX = anchorPixel.X - groundAnchor.X;
offsetY = anchorPixel.Y + groundAnchor.Y;  // Y flip
```

This matrix is `ExportFrameToWorld` in the transform composition chain. The anchor remains fixed across all frames unless root motion is explicitly enabled.

## Spritesheet Composition

Frames are arranged row-major, left-to-right, top-to-bottom:

```
SheetWidth  = FrameWidth  * Columns
SheetHeight = FrameHeight * Rows
MaxFrames   = Columns     * Rows
```

No borders, dividers, labels, or grid lines are added.

## Overflow Policies

| Policy | Behavior |
|---|---|
| `FailExport` | Validation error if animation exceeds sheet capacity. |
| `WarnAndClip` | Warning; parts clipped at frame bounds. |
| `ScaleToFit` | Character uniformly scaled to fit frame. |
| `ExpandFrame` | Frame dimensions expanded dynamically. |

## Metadata

When enabled, a JSON metadata file is written alongside each spritesheet containing format version, character/animation/profile IDs, frame dimensions, frame count, FPS, loop mode, anchor, and per-frame source rectangles.

## Batch Export

The batch export processes all character-animation pairs:

```
For each character in project:
    For each animation matched by SkeletonId:
        Validate -> Sample -> Render -> Compose -> Encode -> Write metadata
```

Content hashing (SHA-256) enables change detection and cache invalidation. The hash combines source image hashes, skeleton data, rig data, animation data, correction data, and profile data.

## Output Structure

```
Exports/{CharacterName}/
    {character}_{animation}.png            (spritesheet)
    {character}_{animation}.metadata.json  (metadata)
    Frames/{animation}/
        frame_0000.png                     (individual frames, optional)
        frame_0001.png
        ...
```

## Invariants

1. Ground anchor is identical in every frame (unless root motion enabled).
2. Preview and export use the same pose-evaluation pipeline.
3. Source artwork is never destructively modified.
4. No separators, labels, or grid lines in spritesheets.
5. Deterministic output for same inputs and application version.
