# Sprite Rig Studio — Rigging Workflow

## Overview

Rigging connects a skeleton to a character's artwork. The process has 6 steps:

1. Import artwork
2. Place the ground anchor
3. Align the complete rig
4. Place individual joints
5. Define body parts
6. Preview movement

## Step 1: Import Artwork

Create a new character rig and select your source PNG. The application supports:
- A single flattened PNG of the full character
- Multiple transparent PNG files for separated body parts
- A mix of both

The source image is copied into the project's `Sources/` directory.

## Step 2: Place Ground Anchor

The ground anchor is the point where the character touches the ground. It determines export alignment.

Default position: midway between the feet at the lowest point.

The ground line, root point, centerline, and source-image bounds are displayed as guides.

## Step 3: Align the Complete Rig

Use the transform tools (Move, Rotate, Scale, Mirror) to position the entire skeleton over the artwork. These adjustments:
- Are stored per character rig, NOT in the shared skeleton
- Do NOT affect any shared animations
- Preserve bone hierarchy and proportions

Tools: Translate, Rotate, Scale (uniform), Mirror (horizontal).

## Step 4: Place Joints

Position each bone's joint to match the character's anatomy. The recommended order:

1. Root (center, ground level)
2. Pelvis
3. Torso
4. Chest
5. Neck
6. Head
7. Upper limbs (shoulder → elbow → wrist)
8. Lower limbs (hip → knee → ankle)

Joint positions define the character's setup pose and are used as the basis for animation.

## Step 5: Define Parts

For flattened artwork: draw polygon masks around each body part, then create a sprite part from each mask. Assign a pivot point and bind the part to a bone.

For separated artwork: import each body part PNG, position it, assign a pivot, and bind to a bone.

## Step 6: Preview Movement

Before creating animations, rotate bones manually to verify the rig behaves correctly. This preview mode does not create keyframes unless recording is enabled.
