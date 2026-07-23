using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows.Input;
using Avalonia.Platform.Storage;
using ReactiveUI;
using SpriteRigStudio.Application.Animations;
using SpriteRigStudio.Application.Exporting;
using SpriteRigStudio.Application.Projects;
using SpriteRigStudio.Application.Rigs;
using SpriteRigStudio.Application.Skeletons;
using SpriteRigStudio.Desktop.Platform;
using SpriteRigStudio.Domain.Animations;
using SpriteRigStudio.Domain.Common;
using SpriteRigStudio.Domain.Exporting;
using SpriteRigStudio.Domain.Geometry;
using SpriteRigStudio.Domain.Projects;
using SpriteRigStudio.Domain.Retargeting;
using SpriteRigStudio.Domain.Rigs;
using SpriteRigStudio.Domain.Skeletons;
using SpriteRigStudio.Infrastructure.Serialization;
using SpriteRigStudio.Rendering.Abstractions;
using SpriteRigStudio.Rendering.Composition;
using SpriteRigStudio.Rendering.Spritesheets;

namespace SpriteRigStudio.Desktop.ViewModels;

/// <summary>
/// Main window view model for Sprite Rig Studio.
/// Coordinates project state, editor state, and panel view models.
/// </summary>
public class MainWindowViewModel : ReactiveObject
{
    // --- Services ---
    private readonly ProjectService _projectService;
    private readonly SkeletonService _skeletonService;
    private readonly RigService _rigService;
    private readonly AnimationService _animationService;
    private readonly ExportService _exportService;
    private readonly IWindowProvider _windowProvider;
    private readonly IRigPoseEvaluator _rigPoseEvaluator;
    private readonly FrameRenderer _frameRenderer;
    private readonly SpritesheetComposer _spritesheetComposer;
    private readonly IImageEncoder _imageEncoder;
    private readonly IImageDecoder _imageDecoder;
    private readonly ProjectSerializer _projectSerializer;

    // --- Active workspace ---
    private string _activeWorkspace = "Project";

    // --- Project state ---
    private SpriteRigProject? _activeProject;
    private SkeletonDefinition? _activeSkeleton;
    private CharacterRigDefinition? _activeCharacter;
    private AnimationClipDefinition? _activeAnimation;
    private ExportProfile? _activeProfile;

    // --- Editor state ---
    private EditorSessionState _sessionState = new();
    private bool _isDirty;
    private bool _canUndo;
    private bool _canRedo;
    private string _activeTool = "Select";
    private string _statusMessage = "Ready";
    private double _currentTime;
    private bool _isPlaying;
    private IDisposable? _playbackSubscription;
    private readonly Stack<(string description, Action undo, Action redo)> _undoStack = new();
    private readonly Stack<(string description, Action undo, Action redo)> _redoStack = new();
    // --- Panel view models ---

    private readonly ProjectPanelViewModel _projectPanel;
    private readonly InspectorViewModel _inspector;
    private bool _isSyncingInspector;

    public MainWindowViewModel(
        ProjectService projectService,
        SkeletonService skeletonService,
        RigService rigService,
        AnimationService animationService,
        ExportService exportService,
        IWindowProvider windowProvider,
        IRigPoseEvaluator rigPoseEvaluator,
        FrameRenderer frameRenderer,
        SpritesheetComposer spritesheetComposer,
        IImageEncoder imageEncoder,
        IImageDecoder imageDecoder,
        ProjectSerializer projectSerializer)
    {
        _projectService = projectService;
        _skeletonService = skeletonService;
        _rigService = rigService;
        _animationService = animationService;
        _exportService = exportService;
        _windowProvider = windowProvider;
        _rigPoseEvaluator = rigPoseEvaluator;
        _frameRenderer = frameRenderer;
        _spritesheetComposer = spritesheetComposer;
        _imageEncoder = imageEncoder;
        _imageDecoder = imageDecoder;
        _projectSerializer = projectSerializer;

        _projectPanel = new ProjectPanelViewModel();
        _inspector = new InspectorViewModel();

        // React to inspector property changes (user edits values) → sync back to character
        _inspector.PropertyChanged += OnInspectorPropertyChanged;

        // Initialize commands
        NewProjectCommand = ReactiveCommand.CreateFromTask(NewProjectAsync);
        OpenProjectCommand = ReactiveCommand.CreateFromTask(OpenProjectAsync);
        SaveProjectCommand = ReactiveCommand.CreateFromTask(SaveProjectAsync, this.WhenAnyValue(x => x.ActiveProject).Select(p => p != null));
        SaveProjectAsCommand = ReactiveCommand.CreateFromTask(SaveProjectAsAsync);
        UndoCommand = ReactiveCommand.Create(Undo, this.WhenAnyValue(x => x.CanUndo));
        RedoCommand = ReactiveCommand.Create(Redo, this.WhenAnyValue(x => x.CanRedo));
        PlayPauseCommand = ReactiveCommand.Create(PlayPause);
        StopCommand = ReactiveCommand.Create(Stop);
        ExportAnimationCommand = ReactiveCommand.CreateFromTask(ExportAnimationAsync);
        ExportAllCommand = ReactiveCommand.CreateFromTask(ExportAllAsync);
        SwitchWorkspaceCommand = ReactiveCommand.Create<string>(SwitchWorkspace);
        SetActiveToolCommand = ReactiveCommand.Create<string>(SetActiveTool);
        PreviousFrameCommand = ReactiveCommand.Create(PreviousFrame, this.WhenAnyValue(x => x.ActiveAnimation).Select(a => a != null));
        NextFrameCommand = ReactiveCommand.Create(NextFrame, this.WhenAnyValue(x => x.ActiveAnimation).Select(a => a != null));
        GoToStartCommand = ReactiveCommand.Create(GoToStart);
        GoToEndCommand = ReactiveCommand.Create(GoToEnd, this.WhenAnyValue(x => x.ActiveAnimation).Select(a => a != null));

        // Playback timer — ticks at ~60 FPS when an animation is playing
        _playbackSubscription = Observable.Interval(TimeSpan.FromMilliseconds(1000.0 / 60.0))
            .Where(_ => _isPlaying && _activeAnimation != null)
            .Subscribe(_ =>
            {
                var delta = 1.0 / 60.0;
                CurrentTime += delta;
                if (_activeAnimation != null && CurrentTime >= _activeAnimation.DurationSeconds)
                {
                    if (_activeAnimation.LoopMode == LoopMode.Loop || _activeAnimation.LoopMode == LoopMode.PingPong)
                        CurrentTime = 0;
                    else
                    {
                        CurrentTime = _activeAnimation.DurationSeconds;
                        IsPlaying = false;
                    }
                }
                EvaluateAnimationPoseAtCurrentTime();
            });

        // React to project panel selection changes
        _projectPanel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ProjectPanelViewModel.SelectedSkeleton))
                OnSelectedSkeletonChanged();
            else if (e.PropertyName == nameof(ProjectPanelViewModel.SelectedCharacter))
                OnSelectedCharacterChanged();
            else if (e.PropertyName == nameof(ProjectPanelViewModel.SelectedAnimation))
                OnSelectedAnimationChanged();
            else if (e.PropertyName == nameof(ProjectPanelViewModel.SelectedProfile))
                OnSelectedProfileChanged();
        };

        // Load default skeleton pose so viewport shows something at startup
        var defaultSkeleton = DefaultHumanoidSkeleton.Create();
        var defaultRig = new CharacterRigDefinition
        {
            Name = "Default",
            SkeletonId = defaultSkeleton.SkeletonId,
            SetupTransform = new CharacterSetupTransform(),
            GroundAnchor = defaultSkeleton.DefaultGroundAnchor
        };
        CurrentPose = _rigPoseEvaluator.EvaluateSetupPose(defaultSkeleton, defaultRig);
    }

    private void OnSelectedSkeletonChanged()
    {
        if (_activeProject == null) return;
        var selected = _projectPanel.SelectedSkeleton;
        if (selected == null) return;

        if (_activeProject.Skeletons.TryGetValue(selected.Id.ToKeyString(), out var skeleton))
        {
            ActiveSkeleton = skeleton;
            var rig = new CharacterRigDefinition
            {
                Name = skeleton.Name,
                SkeletonId = skeleton.SkeletonId,
                SetupTransform = new CharacterSetupTransform(),
                GroundAnchor = skeleton.DefaultGroundAnchor
            };
            CurrentPose = _rigPoseEvaluator.EvaluateSetupPose(skeleton, rig);
            StatusMessage = $"Selected skeleton: {skeleton.Name}";
        }
    }

    private void OnSelectedCharacterChanged()
    {
        if (_activeProject == null) return;
        var selected = _projectPanel.SelectedCharacter;
        if (selected == null) return;

        if (_activeProject.CharacterRigs.TryGetValue(selected.Id.ToKeyString(), out var character))
        {
            ActiveCharacter = character;
            if (_activeProject.Skeletons.TryGetValue(character.SkeletonId.ToKeyString(), out var skeleton))
            {
                CurrentPose = _rigPoseEvaluator.EvaluateSetupPose(skeleton, character);
            }
            SyncInspectorFromCharacter();
            StatusMessage = $"Selected character: {character.Name}";
        }
    }

    private void OnSelectedAnimationChanged()
    {
        if (_activeProject == null) return;
        var selected = _projectPanel.SelectedAnimation;
        if (selected == null) return;

        if (_activeProject.Animations.TryGetValue(selected.Id.ToKeyString(), out var animation))
        {
            ActiveAnimation = animation;
            StatusMessage = $"Selected animation: {animation.Name}";
        }
    }

    private void OnSelectedProfileChanged()
    {
        if (_activeProject == null) return;
        var selected = _projectPanel.SelectedProfile;
        if (selected == null) return;

        if (_activeProject.ExportProfiles.TryGetValue(selected.Id.ToKeyString(), out var profile))
        {
            ActiveProfile = profile;
            StatusMessage = $"Selected profile: {profile.Name}";
        }
    }

    // --- Active workspace ---

    public string ActiveWorkspace
    {
        get => _activeWorkspace;
        set => this.RaiseAndSetIfChanged(ref _activeWorkspace, value);
    }

    // --- Project state ---

    public SpriteRigProject? ActiveProject
    {
        get => _activeProject;
        set => this.RaiseAndSetIfChanged(ref _activeProject, value);
    }

    public SkeletonDefinition? ActiveSkeleton
    {
        get => _activeSkeleton;
        set => this.RaiseAndSetIfChanged(ref _activeSkeleton, value);
    }

    public CharacterRigDefinition? ActiveCharacter
    {
        get => _activeCharacter;
        set => this.RaiseAndSetIfChanged(ref _activeCharacter, value);
    }

    public AnimationClipDefinition? ActiveAnimation
    {
        get => _activeAnimation;
        set => this.RaiseAndSetIfChanged(ref _activeAnimation, value);
    }

    public string ActiveAnimationDuration => _activeAnimation != null
        ? $"{_activeAnimation.DurationSeconds:F2}s"
        : "0.00s";

    public ExportProfile? ActiveProfile
    {
        get => _activeProfile;
        set => this.RaiseAndSetIfChanged(ref _activeProfile, value);
    }

    // --- Editor state ---

    public EditorSessionState SessionState
    {
        get => _sessionState;
        set => this.RaiseAndSetIfChanged(ref _sessionState, value);
    }

    public bool IsDirty
    {
        get => _isDirty;
        set => this.RaiseAndSetIfChanged(ref _isDirty, value);
    }

    public bool CanUndo
    {
        get => _canUndo;
        set => this.RaiseAndSetIfChanged(ref _canUndo, value);
    }

    public bool CanRedo
    {
        get => _canRedo;
        set => this.RaiseAndSetIfChanged(ref _canRedo, value);
    }

    public string ActiveTool
    {
        get => _activeTool;
        set => this.RaiseAndSetIfChanged(ref _activeTool, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }

    public double CurrentTime
    {
        get => _currentTime;
        set => this.RaiseAndSetIfChanged(ref _currentTime, value);
    }

    public bool IsPlaying
    {
        get => _isPlaying;
        set => this.RaiseAndSetIfChanged(ref _isPlaying, value);
    }

    // --- Panel view models ---

    public ProjectPanelViewModel ProjectPanel => _projectPanel;

    public InspectorViewModel Inspector => _inspector;

    // --- Evaluated pose for the viewport ---

    private EvaluatedPose? _currentPose;

    public EvaluatedPose? CurrentPose
    {
        get => _currentPose;
        set => this.RaiseAndSetIfChanged(ref _currentPose, value);
    }

    // --- Commands ---

    public ICommand NewProjectCommand { get; }
    public ICommand OpenProjectCommand { get; }
    public ICommand SaveProjectCommand { get; }
    public ICommand SaveProjectAsCommand { get; }
    public ICommand UndoCommand { get; }
    public ICommand RedoCommand { get; }
    public ICommand PlayPauseCommand { get; }
    public ICommand StopCommand { get; }
    public ICommand ExportAnimationCommand { get; }
    public ICommand ExportAllCommand { get; }
    public ICommand SwitchWorkspaceCommand { get; }
    public ICommand SetActiveToolCommand { get; }
    public ICommand PreviousFrameCommand { get; }
    public ICommand NextFrameCommand { get; }
    public ICommand GoToStartCommand { get; }
    public ICommand GoToEndCommand { get; }

    /// <summary>Title displayed in the window title bar.</summary>
    public string Title
    {
        get
        {
            var title = "Sprite Rig Studio";
            if (_activeProject != null)
                title = $"{_activeProject.Name} - {title}";
            if (_isDirty)
                title = $"*{title}";
            return title;
        }
    }

    // --- Command implementations ---

    private async Task NewProjectAsync()
    {
        try
        {
            var window = _windowProvider.GetMainWindow();
            if (window == null)
            {
                StatusMessage = "Cannot open dialog: no active window.";
                return;
            }

            var folders = await window.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = "Select Project Directory", AllowMultiple = false });
            var selectedPath = folders?.FirstOrDefault()?.Path?.LocalPath;
            if (string.IsNullOrEmpty(selectedPath))
                return;

            var result = await _projectService.CreateProjectAsync("Untitled Project", selectedPath);
            if (result.IsSuccess && result.Project != null)
            {
                ActiveProject = result.Project;
                IsDirty = false;
                StatusMessage = "New project created.";
                UpdateProjectPanel();
                EvaluateProjectPose(result.Project);
                this.RaisePropertyChanged(nameof(Title));
            }
            else
            {
                StatusMessage = $"Failed to create project: {result.ErrorMessage}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    private async Task OpenProjectAsync()
    {
        try
        {
            var window = _windowProvider.GetMainWindow();
            if (window == null)
            {
                StatusMessage = "Cannot open dialog: no active window.";
                return;
            }

            var folders = await window.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Open Project Directory",
                AllowMultiple = false
            });
            var path = folders?.FirstOrDefault()?.Path?.LocalPath;
            if (string.IsNullOrEmpty(path)) return;

            var result = await _projectService.OpenProjectAsync(path);
            if (result.IsSuccess && result.Project != null)
            {
                ActiveProject = result.Project;
                IsDirty = false;
                StatusMessage = $"Opened project: {result.Project.Name}";
                UpdateProjectPanel();
                EvaluateProjectPose(result.Project);
                this.RaisePropertyChanged(nameof(Title));
            }
            else
            {
                StatusMessage = $"Failed to open project: {result.ErrorMessage}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    private async Task SaveProjectAsync()
    {
        if (_activeProject == null) return;

        try
        {
            var result = await _projectService.SaveProjectAsync(_activeProject);
            if (result.IsSuccess)
            {
                IsDirty = false;
                StatusMessage = $"Project saved to {result.FilePath}";
                this.RaisePropertyChanged(nameof(Title));
            }
            else
            {
                StatusMessage = $"Failed to save: {result.ErrorMessage}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    private async Task SaveProjectAsAsync()
    {
        if (_activeProject == null) return;

        try
        {
            var window = _windowProvider.GetMainWindow();
            if (window == null)
            {
                StatusMessage = "Cannot open dialog: no active window.";
                return;
            }

            var folders = await window.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = "Select Save Directory", AllowMultiple = false });
            var selectedPath = folders?.FirstOrDefault()?.Path?.LocalPath;
            if (string.IsNullOrEmpty(selectedPath))
                return;

            var result = await _projectService.SaveProjectAsAsync(_activeProject, selectedPath);
            if (result.IsSuccess)
            {
                IsDirty = false;
                StatusMessage = $"Project saved to {result.FilePath}";
                this.RaisePropertyChanged(nameof(Title));
            }
            else
            {
                StatusMessage = $"Failed to save: {result.ErrorMessage}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    private void Undo()
    {
        if (_undoStack.Count > 0)
        {
            var (description, undo, redo) = _undoStack.Pop();
            undo();
            _redoStack.Push((description, undo, redo));
            CanUndo = _undoStack.Count > 0;
            CanRedo = _redoStack.Count > 0;
            StatusMessage = $"Undo: {description}";
            RefreshPose();
        }
    }

    private void Redo()
    {
        if (_redoStack.Count > 0)
        {
            var (description, undo, redo) = _redoStack.Pop();
            redo();
            _undoStack.Push((description, undo, redo));
            CanUndo = _undoStack.Count > 0;
            CanRedo = _redoStack.Count > 0;
            StatusMessage = $"Redo: {description}";
            RefreshPose();
        }
    }

    private void PlayPause()
    {
        IsPlaying = !IsPlaying;
        StatusMessage = IsPlaying ? "Playing" : "Paused";
    }

    private void Stop()
    {
        IsPlaying = false;
        CurrentTime = 0;
        EvaluateAnimationPoseAtCurrentTime();
        StatusMessage = "Stopped";
    }

    private void GoToStart()
    {
        CurrentTime = 0;
        EvaluateAnimationPoseAtCurrentTime();
    }

    private void GoToEnd()
    {
        if (_activeAnimation != null)
        {
            CurrentTime = _activeAnimation.DurationSeconds;
            EvaluateAnimationPoseAtCurrentTime();
        }
    }

    private void PreviousFrame()
    {
        if (_activeAnimation == null) return;
        var fps = _activeAnimation.FramesPerSecond;
        var frameDuration = 1.0 / fps;
        CurrentTime = Math.Max(0, _currentTime - frameDuration);
        EvaluateAnimationPoseAtCurrentTime();
    }

    private void NextFrame()
    {
        if (_activeAnimation == null) return;
        var fps = _activeAnimation.FramesPerSecond;
        var frameDuration = 1.0 / fps;
        CurrentTime = Math.Min(_activeAnimation.DurationSeconds, _currentTime + frameDuration);
        EvaluateAnimationPoseAtCurrentTime();
    }

    private void EvaluateAnimationPoseAtCurrentTime()
    {
        if (_activeProject == null || _activeCharacter == null || _activeAnimation == null) return;
        if (!_activeProject.Skeletons.TryGetValue(_activeCharacter.SkeletonId.ToKeyString(), out var skeleton)) return;
        CurrentPose = _rigPoseEvaluator.EvaluateAnimationPose(skeleton, _activeCharacter, _activeAnimation, _currentTime);
    }

    public void ExecuteAction(string description, Action undo, Action redo)
    {
        redo();
        _undoStack.Push((description, undo, redo));
        _redoStack.Clear();
        CanUndo = _undoStack.Count > 0;
        CanRedo = false;
        MarkDirty();
    }

    private void RefreshPose()
    {
        EvaluateAnimationPoseAtCurrentTime();
    }

    public void MarkDirty()
    {
        if (!_isDirty)
        {
            IsDirty = true;
            this.RaisePropertyChanged(nameof(Title));
        }
    }

    private async Task ExportAnimationAsync()
    {
        if (_activeProject == null || _activeCharacter == null || _activeAnimation == null || _activeProfile == null)
        {
            StatusMessage = "Select a character, animation, and export profile to export.";
            return;
        }

        try
        {
            var validation = _exportService.ValidateExport(_activeProject, _activeCharacter, _activeAnimation, _activeProfile);
            if (!validation.IsValid)
            {
                StatusMessage = $"Export validation failed: {string.Join("; ", validation.All.Select(i => i.Message))}";
                return;
            }

            if (string.IsNullOrEmpty(_activeProject.ProjectDirectory))
            {
                StatusMessage = "Project must be saved before exporting.";
                return;
            }

            StatusMessage = $"Exporting animation '{_activeAnimation.Name}'...";

            // 1. Get export paths
            var paths = _exportService.GetOutputPaths(
                _activeProject.ProjectDirectory,
                _activeCharacter.Name,
                _activeAnimation.Name,
                _activeProfile);

            Directory.CreateDirectory(paths.ExportsDirectory);

            // 2. Get skeleton
            if (!_activeProject.Skeletons.TryGetValue(_activeCharacter.SkeletonId.ToKeyString(), out var skeleton))
            {
                StatusMessage = $"Skeleton {_activeCharacter.SkeletonId} not found for character.";
                return;
            }

            // 3. Decode all images referenced by character parts
            var decodedImages = new Dictionary<string, DecodedImageInfo>();
            foreach (var part in _activeCharacter.SpriteParts.Values)
            {
                if (string.IsNullOrEmpty(part.ImageReference) || decodedImages.ContainsKey(part.ImageReference))
                    continue;
                var imagePath = System.IO.Path.Combine(_activeProject.ProjectDirectory, part.ImageReference);
                var decodeResult = await _imageDecoder.DecodeAsync(imagePath);
                if (decodeResult.IsSuccess && decodeResult.Value != null)
                    decodedImages[part.ImageReference] = decodeResult.Value;
            }

            // 4. Evaluate and render each frame
            var frameCount = _animationService.GetSampledFrameCount(_activeAnimation, _activeProfile.FrameRate);
            var frames = new List<byte[]>(frameCount);
            for (int i = 0; i < frameCount; i++)
            {
                var time = _animationService.GetSampleTime(_activeAnimation, i, _activeProfile.FrameRate);
                var pose = _rigPoseEvaluator.EvaluateAnimationPose(skeleton, _activeCharacter, _activeAnimation, time);
                var framePixels = _frameRenderer.RenderFrame(pose, _activeCharacter, _activeProfile, decodedImages);
                frames.Add(framePixels);
            }

            // 5. Compose spritesheet
            var sheetPixels = _spritesheetComposer.ComposeSpritesheet(
                frames,
                _activeProfile.Columns,
                _activeProfile.Rows,
                _activeProfile.FrameWidth,
                _activeProfile.FrameHeight);

            // 6. Write PNG file
            var encodeResult = await _imageEncoder.EncodePngToFileAsync(
                paths.SpritesheetPath,
                _activeProfile.SheetWidth,
                _activeProfile.SheetHeight,
                sheetPixels);
            if (encodeResult.IsFailure)
            {
                StatusMessage = $"Failed to write spritesheet: {encodeResult.ErrorMessage}";
                return;
            }

            // 7. Write metadata JSON
            var metadata = new
            {
                project = _activeProject.Name,
                character = _activeCharacter.Name,
                animation = _activeAnimation.Name,
                profile = _activeProfile.Name,
                frames = frameCount,
                frameWidth = _activeProfile.FrameWidth,
                frameHeight = _activeProfile.FrameHeight,
                sheetWidth = _activeProfile.SheetWidth,
                sheetHeight = _activeProfile.SheetHeight,
                columns = _activeProfile.Columns,
                rows = _activeProfile.Rows,
                timestamp = DateTime.UtcNow.ToString("O")
            };
            var metadataJson = _projectSerializer.Serialize(metadata);
            await File.WriteAllTextAsync(paths.MetadataPath, metadataJson);

            StatusMessage = $"Export complete: {frameCount} frames → {paths.SpritesheetPath}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export error: {ex.Message}";
        }
    }

    private async Task ExportAllAsync()
    {
        // TODO: Export all animations for all characters
        await Task.CompletedTask;
    }

    private void SwitchWorkspace(string workspace)
    {
        ActiveWorkspace = workspace;
        StatusMessage = $"Switched to {workspace} workspace";
    }

    private void SetActiveTool(string toolName)
    {
        ActiveTool = toolName;
        StatusMessage = $"Tool: {toolName}";
    }

    // --- Helpers ---

    private void UpdateProjectPanel()
    {
        if (_activeProject == null) return;

        _projectPanel.Skeletons.Clear();
        _projectPanel.Characters.Clear();
        _projectPanel.Animations.Clear();
        _projectPanel.ExportProfiles.Clear();

        foreach (var skeleton in _activeProject.Skeletons.Values)
            _projectPanel.Skeletons.Add(new SkeletonItem(skeleton.SkeletonId, skeleton.Name));

        foreach (var character in _activeProject.CharacterRigs.Values)
            _projectPanel.Characters.Add(new CharacterItem(character.CharacterRigId, character.Name));

        foreach (var animation in _activeProject.Animations.Values)
            _projectPanel.Animations.Add(new AnimationItem(animation.AnimationId, animation.Name));

        foreach (var profile in _activeProject.ExportProfiles.Values)
            _projectPanel.ExportProfiles.Add(new ExportProfileItem(profile.ExportProfileId, profile.Name));
    }

    /// <summary>
    /// Evaluates the setup pose from the first skeleton in a project and updates the viewport.
    /// </summary>
    private void EvaluateProjectPose(SpriteRigProject project)
    {
        var skeleton = project.Skeletons.Values.FirstOrDefault();
        if (skeleton == null) return;

        var rig = new CharacterRigDefinition
        {
            Name = "Character",
            SkeletonId = skeleton.SkeletonId,
            SetupTransform = new CharacterSetupTransform(),
            GroundAnchor = skeleton.DefaultGroundAnchor
        };
        CurrentPose = _rigPoseEvaluator.EvaluateSetupPose(skeleton, rig);
    }

    // ── Tool integration ──────────────────────────────────────────

    /// <summary>
    /// Re-evaluates the current character's setup pose and updates the viewport.
    /// </summary>
    public async Task RefreshPoseAsync()
    {
        if (_activeProject != null && _activeCharacter != null &&
            _activeProject.Skeletons.TryGetValue(_activeCharacter.SkeletonId.ToKeyString(), out var skeleton))
        {
            CurrentPose = _rigPoseEvaluator.EvaluateSetupPose(skeleton, _activeCharacter);
        }
        await Task.CompletedTask;
    }

    /// <summary>
    /// Applies a new setup transform to the active character (called from rig tools).
    /// </summary>
    public async Task ApplyRigTransformAsync(CharacterSetupTransform transform)
    {
        if (_activeCharacter != null)
        {
            _rigService.UpdateSetupTransform(_activeCharacter, transform);
            MarkDirty();
            await RefreshPoseAsync();
            SyncInspectorFromCharacter();
        }
    }

    /// <summary>
    /// Applies a new bone setup override to the active character (called from bone tools).
    /// </summary>
    public async Task ApplyBoneOverrideAsync(BoneId boneId, BoneSetupOverride override_)
    {
        if (_activeCharacter != null)
        {
            _rigService.UpdateBoneSetup(_activeCharacter, boneId, override_);
            MarkDirty();
            await RefreshPoseAsync();
            SyncInspectorFromCharacter();
        }
    }

    /// <summary>
    /// Syncs the inspector panel values from the active character's setup transform.
    /// </summary>
    public void SyncInspectorFromCharacter()
    {
        if (_isSyncingInspector) return;
        _isSyncingInspector = true;
        try
        {
            if (_activeCharacter != null)
            {
                var t = _activeCharacter.SetupTransform;
                _inspector.PositionX = t.Position.X;
                _inspector.PositionY = t.Position.Y;
                _inspector.Rotation = t.RotationDegrees;
                _inspector.ScaleX = t.UniformScale;
                _inspector.ScaleY = t.UniformScale;
            }
        }
        finally
        {
            _isSyncingInspector = false;
        }
    }

    /// <summary>
    /// Handles inspector property changes (user edits) by writing back
    /// to the active character and refreshing the pose.
    /// </summary>
    private void OnInspectorPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (_isSyncingInspector || _activeCharacter == null) return;
        _isSyncingInspector = true;
        try
        {
            var t = _activeCharacter.SetupTransform;
            switch (e.PropertyName)
            {
                case nameof(InspectorViewModel.PositionX):
                    t.Position = new Vector2D(_inspector.PositionX, t.Position.Y);
                    break;
                case nameof(InspectorViewModel.PositionY):
                    t.Position = new Vector2D(t.Position.X, _inspector.PositionY);
                    break;
                case nameof(InspectorViewModel.Rotation):
                    t.RotationDegrees = _inspector.Rotation;
                    break;
                case nameof(InspectorViewModel.ScaleX):
                case nameof(InspectorViewModel.ScaleY):
                    t.UniformScale = (_inspector.ScaleX + _inspector.ScaleY) / 2.0;
                    break;
            }
            _activeCharacter.SetupTransform = t;
            MarkDirty();
            _ = RefreshPoseAsync();
        }
        finally
        {
            _isSyncingInspector = false;
        }
    }

    private static string GetDefaultProjectDirectory()
    {
        var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        return System.IO.Path.Combine(documents, "SpriteRigStudio", "Projects");
    }

}

