# Sprite Rig Studio — Animation Workflow

## Reusable Skeletal Animations

Animations are produced by transforming bones through a shared skeleton. A single animation can drive any character rig that uses the same skeleton.

## Creating an Animation

1. Select or create a skeleton
2. Create a new animation clip (set name, FPS, duration, loop mode)
3. Add keyframes to bone tracks
4. Adjust interpolation between keyframes
5. Preview the animation

## Keyframe Properties

Each keyframe stores:
- **Position** (X, Y in pixels)
- **Rotation** (degrees, counterclockwise)
- **Scale** (X, Y multiplier)
- **Interpolation** (Step, Linear, or Smooth)

## Rotation Handling

Rotation interpolation uses the shortest path by default (350 degrees to 10 degrees goes through 0, not around the long way). For intentional multi-turn rotations, enable explicit rotation direction.

## Loop Modes

| Mode | Behavior |
|---|---|
| **Loop** | Repeats from beginning. Samples [0, duration). |
| **Once** | Plays once and stops. Includes endpoint frame. |
| **PingPong** | Plays forward, then backward, repeating. |
| **Clamp** | Plays once, holds last frame. |

## Sampling Formula

```
sampleTime = frameIndex / exportFramesPerSecond
```

For looping animations, the range is `[0, duration)` — no duplicate endpoint frame. For non-looping, the range is `[0, duration]`.

## Retargeting

Animations created for one skeleton can be applied to any character rig using the same skeleton, even with different proportions. The retargeting system:
- Computes rotation deltas from the canonical rest pose
- Scales translations by bone length or character height ratios
- Applies character-specific correction offsets on top
