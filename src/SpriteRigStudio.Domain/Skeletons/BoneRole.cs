namespace SpriteRigStudio.Domain.Skeletons;

/// <summary>
/// Standard bone roles for workflow assistance and retargeting.
/// The skeleton is not required to be humanoid; arbitrary hierarchies are supported.
/// </summary>
public enum BoneRole
{
    /// <summary>Generic or undefined role.</summary>
    Custom = 0,

    /// <summary>Root bone of the entire skeleton.</summary>
    Root,

    /// <summary>Pelvis / hip region.</summary>
    Pelvis,

    /// <summary>Torso / lower trunk.</summary>
    Torso,

    /// <summary>Chest / upper trunk.</summary>
    Chest,

    /// <summary>Neck connecting chest to head.</summary>
    Neck,

    /// <summary>Head.</summary>
    Head,

    /// <summary>Left shoulder (clavicle area).</summary>
    LeftShoulder,

    /// <summary>Left upper arm.</summary>
    LeftUpperArm,

    /// <summary>Left forearm.</summary>
    LeftForearm,

    /// <summary>Left hand.</summary>
    LeftHand,

    /// <summary>Right shoulder (clavicle area).</summary>
    RightShoulder,

    /// <summary>Right upper arm.</summary>
    RightUpperArm,

    /// <summary>Right forearm.</summary>
    RightForearm,

    /// <summary>Right hand.</summary>
    RightHand,

    /// <summary>Left hip / upper thigh joint.</summary>
    LeftHip,

    /// <summary>Left thigh.</summary>
    LeftThigh,

    /// <summary>Left lower leg.</summary>
    LeftLowerLeg,

    /// <summary>Left foot.</summary>
    LeftFoot,

    /// <summary>Right hip / upper thigh joint.</summary>
    RightHip,

    /// <summary>Right thigh.</summary>
    RightThigh,

    /// <summary>Right lower leg.</summary>
    RightLowerLeg,

    /// <summary>Right foot.</summary>
    RightFoot,

    /// <summary>Weapon attachment.</summary>
    Weapon,

    /// <summary>Shield attachment.</summary>
    Shield,

    /// <summary>Cape / tail attachment.</summary>
    Cape,

    /// <summary>Hair attachment.</summary>
    Hair,

    /// <summary>Accessory attachment.</summary>
    Accessory
}
