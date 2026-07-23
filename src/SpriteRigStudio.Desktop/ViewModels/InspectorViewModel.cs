using ReactiveUI;

namespace SpriteRigStudio.Desktop.ViewModels;

/// <summary>
/// View model for the inspector panel (right sidebar).
/// Displays and edits properties of the selected element.
/// </summary>
public class InspectorViewModel : ReactiveObject
{
    // --- Transform section ---
    private double _positionX;
    private double _positionY;
    private double _rotation;
    private double _scaleX = 1.0;
    private double _scaleY = 1.0;

    // --- Bone section ---
    private string _selectedBoneName = string.Empty;
    private string _boneRole = string.Empty;
    private string _boneLength = string.Empty;

    // --- Part section ---
    private string _selectedPartName = string.Empty;
    private string _boundBone = string.Empty;
    private int _renderOrder;
    private string _pivotX = string.Empty;
    private string _pivotY = string.Empty;

    // --- Animation section ---
    private double _currentTime;
    private double _duration = 1.0;
    private int _fps = 12;
    private string _loopMode = "Loop";
    private string _interpolationMode = "Linear";

    // --- Transform properties ---

    public double PositionX
    {
        get => _positionX;
        set => this.RaiseAndSetIfChanged(ref _positionX, value);
    }

    public double PositionY
    {
        get => _positionY;
        set => this.RaiseAndSetIfChanged(ref _positionY, value);
    }

    public double Rotation
    {
        get => _rotation;
        set => this.RaiseAndSetIfChanged(ref _rotation, value);
    }

    public double ScaleX
    {
        get => _scaleX;
        set => this.RaiseAndSetIfChanged(ref _scaleX, value);
    }

    public double ScaleY
    {
        get => _scaleY;
        set => this.RaiseAndSetIfChanged(ref _scaleY, value);
    }

    // --- Bone properties ---

    public string SelectedBoneName
    {
        get => _selectedBoneName;
        set => this.RaiseAndSetIfChanged(ref _selectedBoneName, value);
    }

    public string BoneRole
    {
        get => _boneRole;
        set => this.RaiseAndSetIfChanged(ref _boneRole, value);
    }

    public string BoneLength
    {
        get => _boneLength;
        set => this.RaiseAndSetIfChanged(ref _boneLength, value);
    }

    // --- Part properties ---

    public string SelectedPartName
    {
        get => _selectedPartName;
        set => this.RaiseAndSetIfChanged(ref _selectedPartName, value);
    }

    public string BoundBone
    {
        get => _boundBone;
        set => this.RaiseAndSetIfChanged(ref _boundBone, value);
    }

    public int RenderOrder
    {
        get => _renderOrder;
        set => this.RaiseAndSetIfChanged(ref _renderOrder, value);
    }

    public string PivotX
    {
        get => _pivotX;
        set => this.RaiseAndSetIfChanged(ref _pivotX, value);
    }

    public string PivotY
    {
        get => _pivotY;
        set => this.RaiseAndSetIfChanged(ref _pivotY, value);
    }

    // --- Animation properties ---

    public double CurrentTime
    {
        get => _currentTime;
        set => this.RaiseAndSetIfChanged(ref _currentTime, value);
    }

    public double Duration
    {
        get => _duration;
        set => this.RaiseAndSetIfChanged(ref _duration, value);
    }

    public int FPS
    {
        get => _fps;
        set => this.RaiseAndSetIfChanged(ref _fps, value);
    }

    public string LoopMode
    {
        get => _loopMode;
        set => this.RaiseAndSetIfChanged(ref _loopMode, value);
    }

    public string InterpolationMode
    {
        get => _interpolationMode;
        set => this.RaiseAndSetIfChanged(ref _interpolationMode, value);
    }
}
