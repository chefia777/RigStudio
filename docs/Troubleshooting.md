# Sprite Rig Studio — Troubleshooting

## Application Won't Start

**Issue:** The application fails to launch or crashes immediately.

**Check:**
1. Windows 10 or later (64-bit) is required.
2. The self-contained release does not require .NET runtime.
3. If running from source: ensure .NET 8.0 SDK is installed.
4. Check `%LOCALAPPDATA%/SpriteRigStudio/Logs/` for error logs.

## Cannot Open a Project

**Issue:** "Project manifest not found" or "Invalid format version."

**Solutions:**
- Ensure the selected directory contains a valid `project.srsproject` file.
- The project may have been created by a newer version. Update Sprite Rig Studio.
- The manifest file may be corrupted. Check the `Recovery/` directory for autosaves.

## Export Fails

**Issue:** "Layout too small for frame count" or "Character overflow."

**Solutions:**
- Increase the columns or rows in the export profile.
- Reduce the animation's frame count by lowering FPS or duration.
- Change the overflow policy from "Fail" to "Scale to Fit" or "Expand Frame."

## Transparent Gaps in Rendered Parts

**Issue:** When rotating body parts, transparent gaps appear at the edges.

**Reason:** The source pixels don't extend far enough to cover the rotated area. This is expected behavior — the application does not fabricate missing pixels.

**Solutions:**
- Expand the mask for that body part.
- Add an underpaint layer (a filled shape behind the part).
- Use a replacement part image with larger bounds.

## Pixel Art Looks Blurry

**Issue:** Exported pixel art has soft edges or blurry pixels.

**Solution:** Enable pixel-art mode:
- Set sampling mode to Nearest Neighbor
- Disable antialiasing for masks
- Ensure feather pixels is 0
- Use PNG output (lossless)

## Performance Is Slow

**Issue:** Viewport is laggy or exports take too long.

**Tips:**
- Use reduced-resolution preview during heavy interactions.
- Close other applications to free memory.
- Export individual characters rather than the whole project.
