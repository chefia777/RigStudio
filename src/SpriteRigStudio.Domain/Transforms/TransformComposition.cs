using SpriteRigStudio.Domain.Geometry;

namespace SpriteRigStudio.Domain.Transforms;

/// <summary>
/// Documents and implements the single authoritative transform-composition convention.
///
/// Final part world transform (for rendering/export):
///   SpritePart.WorldTransform =
///         ExportFrameToWorld
///       × CharacterSetupRoot
///       × BoneSetupWorld
///       × BoneAnimationDelta
///       × CharacterCorrection
///       × PartLocalTransform
///
/// Where:
///   ExportFrameToWorld = translation that maps export-anchor to frame center
///   CharacterSetupRoot = the rig's setup transform (position, rotation, scale, flip)
///   BoneSetupWorld    = bone's world transform from character setup pose
///   BoneAnimationDelta = animation transform applied on top of setup pose
///   CharacterCorrection = per-character animation correction offset
///   PartLocalTransform = sprite part's local offset relative to its bound bone
///
/// Domain coordinates: Y increases upward.
/// Positive rotation is counterclockwise.
/// </summary>
public static class TransformComposition
{
    /// <summary>
    /// Computes the part-to-world transform for a single sprite part at a given evaluated pose.
    /// </summary>
    public static Matrix3x2D ComputePartWorldTransform(
        Matrix3x2D exportFrameToWorld,
        Matrix3x2D characterSetupRoot,
        Matrix3x2D boneSetupWorld,
        Matrix3x2D boneAnimationDelta,
        Matrix3x2D characterCorrection,
        Matrix3x2D partLocalTransform)
    {
        // Compose: exportFrameToWorld * characterSetupRoot * boneSetupWorld *
        //          boneAnimationDelta * characterCorrection * partLocalTransform
        return exportFrameToWorld
             * characterSetupRoot
             * boneSetupWorld
             * boneAnimationDelta
             * characterCorrection
             * partLocalTransform;
    }
}
