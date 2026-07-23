using SpriteRigStudio.Domain.Animations;
using SpriteRigStudio.Domain.Exporting;
using SpriteRigStudio.Domain.Masks;
using SpriteRigStudio.Domain.Projects;
using SpriteRigStudio.Domain.Rigs;
using SpriteRigStudio.Domain.Skeletons;
using SpriteRigStudio.Domain.Validation;

namespace SpriteRigStudio.Application.Validation;

/// <summary>
/// Validates a complete project, its entities, and their relationships.
/// </summary>
public class ProjectValidator
{
    /// <summary>
    /// Validates the entire project.
    /// </summary>
    public ValidationResult ValidateProject(SpriteRigProject project)
    {
        var result = new ValidationResult();

        // Project-level checks
        if (string.IsNullOrWhiteSpace(project.Name))
            result.AddError("PROJECT_NO_NAME", "Project must have a name.");

        // Validate each skeleton
        foreach (var (skeletonId, skeleton) in project.Skeletons)
        {
            ValidateSkeleton(skeleton, result);
        }

        // Validate each character rig
        foreach (var (charId, character) in project.CharacterRigs)
        {
            ValidateCharacter(project, character, result);
        }

        // Validate each animation
        foreach (var (animId, animation) in project.Animations)
        {
            ValidateAnimation(project, animation, result);
        }

        // Validate each export profile
        foreach (var (profId, profile) in project.ExportProfiles)
        {
            ValidateExportProfile(profile, result);
        }

        return result;
    }

    private void ValidateSkeleton(SkeletonDefinition skeleton, ValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(skeleton.Name))
            result.AddError("SKELETON_NO_NAME", "Skeleton must have a name.", skeleton.SkeletonId.ToString());

        if (skeleton.Bones.Count == 0)
        {
            result.AddError("SKELETON_NO_BONES", $"Skeleton '{skeleton.Name}' has no bones.", skeleton.SkeletonId.ToString());
            return;
        }

        if (!skeleton.Bones.ContainsKey(skeleton.RootBoneId))
            result.AddError("SKELETON_MISSING_ROOT", $"Skeleton '{skeleton.Name}' is missing its root bone.", skeleton.SkeletonId.ToString());

        if (skeleton.HasCycles())
            result.AddError("SKELETON_CYCLE", $"Skeleton '{skeleton.Name}' contains cycles.", skeleton.SkeletonId.ToString());

        // Check for missing parents
        foreach (var (boneId, bone) in skeleton.Bones)
        {
            if (bone.ParentBoneId.HasValue && !skeleton.Bones.ContainsKey(bone.ParentBoneId.Value))
                result.AddError("BONE_MISSING_PARENT",
                    $"Bone '{bone.Name}' references missing parent bone {bone.ParentBoneId}.",
                    boneId.ToString());

            if (bone.ReferenceLength <= 0)
                result.AddError("BONE_INVALID_LENGTH",
                    $"Bone '{bone.Name}' has invalid length {bone.ReferenceLength}.",
                    boneId.ToString());
        }
    }

    private void ValidateCharacter(SpriteRigProject project, CharacterRigDefinition character, ValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(character.Name))
            result.AddError("CHARACTER_NO_NAME", "Character rig must have a name.", character.CharacterRigId.ToString());

        if (!project.Skeletons.ContainsKey(character.SkeletonId))
            result.AddWarning("CHARACTER_MISSING_SKELETON",
                $"Character '{character.Name}' references skeleton {character.SkeletonId} that is not in the project.",
                character.CharacterRigId.ToString());

        // Check for unbound parts
        var unboundParts = character.SpriteParts.Values.Where(p => !p.BoundBoneId.HasValue).ToList();
        if (unboundParts.Any())
        {
            result.AddWarning("CHARACTER_UNBOUND_PARTS",
                $"Character '{character.Name}' has {unboundParts.Count} unbound parts.",
                character.CharacterRigId.ToString());
        }

        // Check for parts bound to missing bones
        foreach (var (partId, part) in character.SpriteParts)
        {
            if (part.BoundBoneId.HasValue)
            {
                var skeleton = project.Skeletons.GetValueOrDefault(character.SkeletonId);
                if (skeleton != null && !skeleton.Bones.ContainsKey(part.BoundBoneId.Value))
                    result.AddError("PART_BOUND_TO_MISSING_BONE",
                        $"Part '{part.Name}' is bound to bone {part.BoundBoneId} which does not exist.",
                        partId.ToString());
            }
        }
    }

    private void ValidateAnimation(SpriteRigProject project, AnimationClipDefinition animation, ValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(animation.Name))
            result.AddError("ANIMATION_NO_NAME", "Animation must have a name.", animation.AnimationId.ToString());

        if (animation.FramesPerSecond <= 0)
            result.AddError("ANIMATION_INVALID_FPS", $"Animation '{animation.Name}' has invalid FPS: {animation.FramesPerSecond}.");

        if (animation.DurationSeconds <= 0)
            result.AddError("ANIMATION_INVALID_DURATION", $"Animation '{animation.Name}' has invalid duration: {animation.DurationSeconds}.");

        if (!project.Skeletons.ContainsKey(animation.SkeletonId))
            result.AddWarning("ANIMATION_MISSING_SKELETON",
                $"Animation '{animation.Name}' references skeleton {animation.SkeletonId} that is not in the project.",
                animation.AnimationId.ToString());

        // Check keyframes
        foreach (var (boneId, track) in animation.BoneTracks)
        {
            var sorted = track.GetSortedKeyframes();
            for (int i = 0; i < sorted.Count; i++)
            {
                if (sorted[i].TimeSeconds < 0)
                    result.AddError("KEYFRAME_NEGATIVE_TIME",
                        $"Keyframe at {sorted[i].TimeSeconds}s in bone {boneId} has negative time.",
                        animation.AnimationId.ToString());

                if (i > 0 && Math.Abs(sorted[i].TimeSeconds - sorted[i - 1].TimeSeconds) < 0.0001)
                    result.AddError("KEYFRAME_DUPLICATE_TIME",
                        $"Duplicate keyframe time {sorted[i].TimeSeconds}s in bone {boneId}.",
                        animation.AnimationId.ToString());
            }
        }
    }

    private void ValidateExportProfile(ExportProfile profile, ValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(profile.Name))
            result.AddError("PROFILE_NO_NAME", "Export profile must have a name.");

        if (profile.FrameWidth <= 0)
            result.AddError("PROFILE_INVALID_WIDTH", $"Profile '{profile.Name}' has invalid frame width: {profile.FrameWidth}.");

        if (profile.FrameHeight <= 0)
            result.AddError("PROFILE_INVALID_HEIGHT", $"Profile '{profile.Name}' has invalid frame height: {profile.FrameHeight}.");

        if (profile.Columns <= 0)
            result.AddError("PROFILE_INVALID_COLUMNS", $"Profile '{profile.Name}' has invalid column count: {profile.Columns}.");

        if (profile.Rows <= 0)
            result.AddError("PROFILE_INVALID_ROWS", $"Profile '{profile.Name}' has invalid row count: {profile.Rows}.");
    }
}
