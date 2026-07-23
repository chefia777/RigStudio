using Microsoft.Extensions.Logging;
using SpriteRigStudio.Domain.Animations;
using SpriteRigStudio.Domain.Common;
using SpriteRigStudio.Domain.Exporting;
using SpriteRigStudio.Domain.Geometry;
using SpriteRigStudio.Domain.Projects;
using SpriteRigStudio.Domain.Rigs;
using SpriteRigStudio.Domain.Skeletons;
using DomainValidation = SpriteRigStudio.Domain.Validation;

namespace SpriteRigStudio.Application.Exporting;

/// <summary>
/// Application service for export use cases.
/// Coordinates between the domain model and rendering/infrastructure layers.
/// </summary>
public class ExportService
{
    private readonly ILogger<ExportService> _logger;

    public ExportService(ILogger<ExportService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Validates that an export can proceed.
    /// </summary>
    public DomainValidation.ValidationResult ValidateExport(
        SpriteRigProject project,
        CharacterRigDefinition character,
        AnimationClipDefinition animation,
        ExportProfile profile)
    {
        var result = new DomainValidation.ValidationResult();

        // Validate frame dimensions
        if (profile.FrameWidth <= 0 || profile.FrameHeight <= 0)
            result.AddError("EXPORT_INVALID_DIMENSIONS", "Frame width and height must be positive.");

        // Validate spritesheet capacity
        var frameCount = animation.FrameCount;
        if (frameCount > profile.MaxFrames && profile.OverflowPolicy == OverflowPolicy.FailExport)
            result.AddError("EXPORT_SHEET_TOO_SMALL",
                $"Animation '{animation.Name}' has {frameCount} frames but the spritesheet " +
                $"({profile.Columns}x{profile.Rows}) only holds {profile.MaxFrames} frames. " +
                $"Increase columns/rows or change the overflow policy.");

        // Validate character has skeleton
        if (!project.Skeletons.ContainsKey(character.SkeletonId.ToKeyString()))
            result.AddError("EXPORT_MISSING_SKELETON",
                $"Character '{character.Name}' references skeleton {character.SkeletonId} which was not found.");

        // Validate animation skeleton matches
        if (animation.SkeletonId != character.SkeletonId)
            result.AddError("EXPORT_SKELETON_MISMATCH",
                $"Animation '{animation.Name}' is for skeleton {animation.SkeletonId} " +
                $"but character '{character.Name}' uses skeleton {character.SkeletonId}.");

        // Validate character has parts bound
        var unboundParts = character.SpriteParts.Values.Where(p => !p.BoundBoneId.HasValue).ToList();
        if (unboundParts.Any())
            result.AddWarning("EXPORT_UNBOUND_PARTS",
                $"Character '{character.Name}' has {unboundParts.Count} unbound sprite parts: " +
                $"{string.Join(", ", unboundParts.Select(p => p.Name))}");

        return result;
    }

    /// <summary>
    /// Gets the export output paths for a character and animation.
    /// </summary>
    public ExportOutputPaths GetOutputPaths(
        string projectDirectory,
        string characterName,
        string animationName,
        ExportProfile profile)
    {
        var sanitizedName = SanitizeFileName(characterName);
        var sanitizedAnim = SanitizeFileName(animationName);

        var exportsDir = Path.Combine(projectDirectory, "Exports", sanitizedName);
        var baseName = $"{sanitizedName}_{sanitizedAnim}";

        return new ExportOutputPaths
        {
            ExportsDirectory = exportsDir,
            BaseName = baseName,
            SpritesheetPath = Path.Combine(exportsDir, $"{baseName}.png"),
            MetadataPath = Path.Combine(exportsDir, $"{baseName}.metadata.json"),
            FramesDirectory = Path.Combine(exportsDir, "Frames", sanitizedAnim)
        };
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
        return sanitized.ToLowerInvariant().Replace(' ', '_');
    }

    /// <summary>
    /// Computes the export frame-to-world transform that maps the character's
    /// ground anchor to the configured export anchor pixel.
    /// </summary>
    public Matrix3x2D ComputeExportFrameTransform(
        ExportProfile profile,
        Vector2D groundAnchor)
    {
        // The anchor pixel is in frame space where Y increases downward.
        // We need to map the character's ground anchor (domain space, Y up)
        // to the export frame (pixel space, Y down).
        //
        // ExportFrameToWorld = translate such that groundAnchor maps to AnchorPixel
        var anchorX = profile.AnchorPixel.X;
        var anchorY = profile.AnchorPixel.Y;

        // In export frame space: origin is top-left, Y increases downward
        // Ground anchor in character space needs to align to (anchorX, anchorY)
        // in the export frame.
        var offsetX = anchorX - groundAnchor.X;
        var offsetY = anchorY + groundAnchor.Y; // Y flip: character Y-up to image Y-down

        return Matrix3x2D.CreateTranslation(new Vector2D(offsetX, offsetY));
    }
}

/// <summary>
/// Output paths for an export operation.
/// </summary>
public class ExportOutputPaths
{
    public string ExportsDirectory { get; init; } = string.Empty;
    public string BaseName { get; init; } = string.Empty;
    public string SpritesheetPath { get; init; } = string.Empty;
    public string MetadataPath { get; init; } = string.Empty;
    public string FramesDirectory { get; init; } = string.Empty;
}
