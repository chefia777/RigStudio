using System;
using Microsoft.Extensions.Logging;
using SpriteRigStudio.Domain.Common;
using SpriteRigStudio.Domain.Geometry;
using SpriteRigStudio.Domain.Masks;
using SpriteRigStudio.Domain.Parts;
using SpriteRigStudio.Domain.Projects;
using SpriteRigStudio.Domain.Rigs;
using SpriteRigStudio.Domain.Skeletons;
using SpriteRigStudio.Domain.Transforms;

namespace SpriteRigStudio.Application.Rigs;

/// <summary>
/// Application service for character rig management use cases.
/// </summary>
public class RigService
{
    private readonly ILogger<RigService> _logger;

    public RigService(ILogger<RigService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Creates a new character rig based on a skeleton.
    /// </summary>
    public CharacterRigDefinition CreateCharacterRig(string name, SkeletonId skeletonId, string? sourceArtwork = null)
    {
        var rig = new CharacterRigDefinition
        {
            CharacterRigId = CharacterRigId.New(),
            Name = name,
            SkeletonId = skeletonId,
            SourceArtwork = sourceArtwork
        };

        // Set default ground anchor at bottom center
        rig.GroundAnchor = new Vector2D(0, 0);

        return rig;
    }

    /// <summary>
    /// Updates the character setup transform (move, rotate, scale, flip).
    /// </summary>
    public void UpdateSetupTransform(CharacterRigDefinition rig, CharacterSetupTransform transform)
    {
        rig.SetupTransform = transform;
    }

    /// <summary>
    /// Sets the ground anchor position.
    /// </summary>
    public void SetGroundAnchor(CharacterRigDefinition rig, Vector2D anchor)
    {
        rig.GroundAnchor = anchor;
    }

    /// <summary>
    /// Updates a single bone's setup override.
    /// </summary>
    public void UpdateBoneSetup(CharacterRigDefinition rig, BoneId boneId, BoneSetupOverride setupOverride)
    {
        rig.BoneSetupOverrides[boneId.ToKeyString()] = setupOverride;
    }

    /// <summary>
    /// Creates a sprite part and binds it to a bone.
    /// </summary>
    public SpritePartDefinition CreatePart(
        CharacterRigDefinition rig,
        string name,
        BoneId boneId,
        Vector2D pivot,
        SourceType sourceType,
        string? imageReference = null)
    {
        var part = new SpritePartDefinition
        {
            SpritePartId = SpritePartId.New(),
            Name = name,
            BoundBoneId = boneId,
            Pivot = pivot,
            SourceType = sourceType,
            ImageReference = imageReference,
            RenderOrder = rig.SpriteParts.Count * 10
        };

        rig.SpriteParts[part.SpritePartId.ToKeyString()] = part;
        return part;
    }

    /// <summary>
    /// Creates a part from a polygon mask.
    /// </summary>
    public SpritePartDefinition CreatePartFromMask(
        CharacterRigDefinition rig,
        string name,
        BoneId boneId,
        PolygonMaskDefinition mask,
        string sourceImage)
    {
        var part = new SpritePartDefinition
        {
            SpritePartId = SpritePartId.New(),
            Name = name,
            BoundBoneId = boneId,
            Pivot = Vector2D.Zero,
            SourceType = SourceType.FlattenedImageMask,
            ImageReference = sourceImage,
            MaskId = mask.MaskId,
            RenderOrder = rig.SpriteParts.Count * 10
        };

        rig.SpriteParts[part.SpritePartId.ToKeyString()] = part;
        return part;
    }

    /// <summary>
    /// Binds a part to a bone.
    /// </summary>
    public Result BindPartToBone(CharacterRigDefinition rig, SpritePartId partId, BoneId boneId)
    {
        if (!rig.SpriteParts.TryGetValue(partId.ToKeyString(), out var part))
            return Result.Failure("PART_NOT_FOUND", $"Sprite part {partId} not found.");

        part.BoundBoneId = boneId;
        return Result.Success();
    }

    /// <summary>
    /// Updates a part's pivot point.
    /// </summary>
    public void UpdatePartPivot(CharacterRigDefinition rig, SpritePartId partId, Vector2D pivot)
    {
        if (rig.SpriteParts.TryGetValue(partId.ToKeyString(), out var part))
        {
            part.Pivot = pivot;
        }
    }

    /// <summary>
    /// Updates a part's render order.
    /// </summary>
    public void UpdatePartRenderOrder(CharacterRigDefinition rig, SpritePartId partId, int order)
    {
        if (rig.SpriteParts.TryGetValue(partId.ToKeyString(), out var part))
        {
            part.RenderOrder = order;
        }
    }

    /// <summary>
    /// Duplicates a character rig.
    /// </summary>
    public CharacterRigDefinition DuplicateCharacterRig(CharacterRigDefinition source)
    {
        var duplicate = new CharacterRigDefinition
        {
            CharacterRigId = CharacterRigId.New(),
            Name = source.Name + " (copy)",
            SkeletonId = source.SkeletonId,
            SourceArtwork = source.SourceArtwork,
            SetupTransform = source.SetupTransform.Clone(),
            GroundAnchor = source.GroundAnchor
        };

        foreach (var (boneKey, override_) in source.BoneSetupOverrides)
        {
            duplicate.BoneSetupOverrides[boneKey] = new BoneSetupOverride
            {
                BoneId = new BoneId(Guid.Parse(boneKey)),
                LocalPosition = override_.LocalPosition,
                LocalRotationDegrees = override_.LocalRotationDegrees,
                LocalScale = override_.LocalScale,
                Length = override_.Length,
                Enabled = override_.Enabled,
                LockState = override_.LockState
            };
        }

        return duplicate;
    }
}
