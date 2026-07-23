using SpriteRigStudio.Domain.Animations;
using SpriteRigStudio.Domain.Common;
using SpriteRigStudio.Domain.Geometry;

namespace SpriteRigStudio.Domain.Common;

/// <summary>
/// Transient editor session state that is separate from persistent project data.
/// Not serialized in project files.
/// </summary>
public class EditorSessionState
{
    public string? ActiveProjectPath { get; set; }
    public string ActiveWorkspace { get; set; } = "Project";
    public SkeletonId? SelectedSkeletonId { get; set; }
    public CharacterRigId? SelectedCharacterRigId { get; set; }
    public AnimationId? SelectedAnimationId { get; set; }
    public HashSet<BoneId> SelectedBoneIds { get; init; } = new();
    public HashSet<SpritePartId> SelectedPartIds { get; init; } = new();
    public double CurrentTimeSeconds { get; set; }
    public PlaybackState PlaybackState { get; set; } = PlaybackState.Stopped;
    public string ActiveTool { get; set; } = "Select";
    public double CanvasZoom { get; set; } = 1.0;
    public Vector2D CanvasOffset { get; set; }
    public OnionSkinSettings OnionSkinSettings { get; set; } = new();
    public bool ShowGuides { get; set; } = true;
    public bool IsDirty { get; set; }
    public ExportProfileId? LastExportProfileId { get; set; }
    public bool IsEditCorrectionMode { get; set; }
}

public enum PlaybackState
{
    Stopped,
    Playing,
    Paused
}

public class OnionSkinSettings
{
    public bool Enabled { get; set; }
    public int FramesBefore { get; set; } = 1;
    public int FramesAfter { get; set; }
    public double Opacity { get; set; } = 0.3;
}
