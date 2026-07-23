# Sprite Rig Studio — Project Format

## Directory Structure

Sprite Rig Studio uses a directory-based project format. Each project is a folder with a structured layout:

```
MyProject/
├── project.srsproject          # Project manifest (JSON)
├── Sources/                     # Imported source images
│   └── character.png
├── Parts/                       # Separated body part images
│   └── character/
│       ├── head.png
│       ├── torso.png
│       └── sword.png
├── Skeletons/                   # Skeleton definitions
│   └── humanoid_<id>.json
├── Characters/                  # Character rig definitions
│   └── frost-knight_<id>.json
├── Animations/                  # Animation clips
│   └── idle_<id>.json
├── ExportProfiles/              # Export configuration
│   └── 4x4-300_<id>.json
├── Thumbnails/                  # Cached previews
├── Recovery/                    # Autosave recovery files
└── Exports/                     # Generated spritesheets
    └── FrostKnight/
        ├── frost-knight_idle.png
        └── frost-knight_idle.metadata.json
```

## Manifest (project.srsproject)

The manifest is a JSON file that references all entity files by relative path:

```json
{
  "formatVersion": 1,
  "projectId": "8d7158b82f234313b7eb171a52ad0b31",
  "name": "My Project",
  "createdAtUtc": "2026-07-23T00:00:00.000Z",
  "updatedAtUtc": "2026-07-23T12:00:00.000Z",
  "skeletonFiles": ["Skeletons/humanoid_<id>.json"],
  "characterFiles": ["Characters/frost-knight_<id>.json"],
  "animationFiles": ["Animations/idle_<id>.json"],
  "exportProfileFiles": ["ExportProfiles/4x4-300_<id>.json"]
}
```

## Entity Files

Each entity (skeleton, character, animation, profile) is stored in its own JSON file. The files use:
- UTF-8 encoding
- Stable property names in camelCase
- Strongly typed IDs as 32-character hex strings
- Relative paths for asset references
- ISO-8601 UTC timestamps

## ID Format

All entity IDs are 32-character lowercase hex strings (GUID without hyphens):

```
8d7158b82f234313b7eb171a52ad0b31
```

IDs are stable — renaming a display name does not change the ID.
