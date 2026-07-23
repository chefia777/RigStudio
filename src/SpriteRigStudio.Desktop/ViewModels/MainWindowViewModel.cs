using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows.Input;
using Avalonia.Platform.Storage;
using ReactiveUI;
using SpriteRigStudio.Application.Abstractions;
using SpriteRigStudio.Application.Animations;
using SpriteRigStudio.Application.Masks;
using SpriteRigStudio.Application.Exporting;
using SpriteRigStudio.Application.Projects;
using SpriteRigStudio.Application.Rigs;
using SpriteRigStudio.Application.Skeletons;
using SpriteRigStudio.Desktop.Dialogs;
using SpriteRigStudio.Desktop.Platform;
using SpriteRigStudio.Domain.Animations;
using SpriteRigStudio.Domain.Common;
using SpriteRigStudio.Domain.Exporting;
using SpriteRigStudio.Domain.Geometry;
using SpriteRigStudio.Domain.Transforms;
using SpriteRigStudio.Domain.Parts;
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
    private readonly IProjectRepository _projectRepository;
    private readonly MaskService _maskService;

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
    private bool _isCorrectionMode;
    private bool _showSkeleton = true;
    private bool _showArtwork = true;
    private bool _showMasks;
    private bool _showGuides = true;
    private bool _isProjectPanelVisible = true;
    private bool _isInspectorPanelVisible = true;
    private bool _isTimelineVisible = true;
    private IDisposable? _playbackSubscription;
    private IDisposable? _autosaveSubscription;
    private readonly Stack<(string description, Action undo, Action redo)> _undoStack = new();
    private readonly Stack<(string description, Action undo, Action redo)> _redoStack = new();
    // --- Export state ---
    private double _exportProgress;
    private string _exportStatus = "";
    private bool _isExporting;
    private CancellationTokenSource? _exportCancellation;
    private readonly Dictionary<CharacterRigId, string> _pendingArtworkSources = new();

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
        ProjectSerializer projectSerializer,
        IProjectRepository projectRepository,
        MaskService maskService)
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
        _projectRepository = projectRepository;
        _maskService = maskService;

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
        NewCharacterCommand = ReactiveCommand.CreateFromTask(NewCharacterAsync);
        CreateMaskCommand = ReactiveCommand.CreateFromTask(CreateMaskAsync);
        AddKeyframeCommand = ReactiveCommand.CreateFromTask(AddKeyframeAsync);
        MirrorRigCommand = ReactiveCommand.Create(MirrorRig);
        DeleteBoneCommand = ReactiveCommand.Create(DeleteSelectedBone);
        PreviousFrameCommand = ReactiveCommand.Create(PreviousFrame, this.WhenAnyValue(x => x.ActiveAnimation).Select(a => a != null));
        NextFrameCommand = ReactiveCommand.Create(NextFrame, this.WhenAnyValue(x => x.ActiveAnimation).Select(a => a != null));
        GoToStartCommand = ReactiveCommand.Create(GoToStart);
        GoToEndCommand = ReactiveCommand.Create(GoToEnd, this.WhenAnyValue(x => x.ActiveAnimation).Select(a => a != null));
        NewAnimationCommand = ReactiveCommand.CreateFromTask(NewAnimationAsync);
        AddBoneCommand = ReactiveCommand.CreateFromTask(AddBoneAsync);
        ExportDialogCommand = ReactiveCommand.CreateFromTask(ExportDialogAsync);
        EditPivotCommand = ReactiveCommand.CreateFromTask(EditPivotAsync);
        ImportSeparatePartCommand = ReactiveCommand.CreateFromTask(ImportSeparatePartAsync);
        ToggleCorrectionModeCommand = ReactiveCommand.Create(ToggleCorrectionMode);
        CancelToolCommand = ReactiveCommand.Create(CancelTool);
        FitViewportCommand = ReactiveCommand.Create(FitViewport);
        ExitCommand = ReactiveCommand.Create(ExitApp);
        AboutCommand = ReactiveCommand.Create(ShowAbout);
        ImportArtworkCommand = ReactiveCommand.CreateFromTask(NewCharacterAsync);
        PreferencesCommand = ReactiveCommand.Create(ShowPreferences);
        ProjectSettingsCommand = ReactiveCommand.Create(ShowProjectSettings);
        NewPartCommand = ReactiveCommand.CreateFromTask(CreateMaskAsync);
        BakeAnimationCommand = ReactiveCommand.Create(BakeAnimation);
        NewExportProfileCommand = ReactiveCommand.Create(NewExportProfile);
        ToggleProjectPanelCommand = ReactiveCommand.Create(ToggleProjectPanel);
        ToggleInspectorPanelCommand = ReactiveCommand.Create(ToggleInspectorPanel);
        ToggleTimelineCommand = ReactiveCommand.Create(ToggleTimeline);
        ToggleShowSkeletonCommand = ReactiveCommand.Create(ToggleShowSkeleton);
        ToggleShowArtworkCommand = ReactiveCommand.Create(ToggleShowArtwork);
        ToggleShowMasksCommand = ReactiveCommand.Create(ToggleShowMasks);
        ToggleShowGuidesCommand = ReactiveCommand.Create(ToggleShowGuides);
        CancelExportCommand = ReactiveCommand.Create(CancelExport, this.WhenAnyValue(x => x.IsExporting));
        AddAnimationEventCommand = ReactiveCommand.CreateFromTask(AddAnimationEventAsync);
        // Autosave timer — ticks every 60 seconds when the project is dirty
        _autosaveSubscription = Observable.Interval(TimeSpan.FromSeconds(60))
            .Where(_ => _isDirty && _activeProject != null && !string.IsNullOrEmpty(_activeProject.ProjectDirectory))
            .Subscribe(async _ =>
            {
                var result = await _projectRepository.AutosaveAsync(_activeProject!);
                if (result.IsSuccess)
                    StatusMessage = "Autosaved.";
            });

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

        // Create an in-memory project on startup so users can immediately
        // create characters, animations, and use the tools without saving first.
        var startupProject = new SpriteRigProject
        {
            Name = "Untitled Project",
            FormatVersion = 1,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        var startupSkeleton = DefaultHumanoidSkeleton.Create();
        startupProject.Skeletons[startupSkeleton.SkeletonId.ToKeyString()] = startupSkeleton;
        ActiveProject = startupProject;
        ActiveSkeleton = startupSkeleton;
        IsDirty = true;

        // Build a default setup pose from the skeleton
        var defaultRig = new CharacterRigDefinition
        {
            Name = "Default View",
            SkeletonId = startupSkeleton.SkeletonId,
            SetupTransform = new CharacterSetupTransform(),
            GroundAnchor = startupSkeleton.DefaultGroundAnchor
        };
        CurrentPose = _rigPoseEvaluator.EvaluateSetupPose(startupSkeleton, defaultRig);
        UpdateProjectPanel();

        // Check for recovery sessions
        CheckRecoveryAsync().FireAndForget();
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
            if (_activeProject.Skeletons.TryGetValue(animation.SkeletonId.ToKeyString(), out var skeleton))
                ActiveSkeleton = skeleton;
            CurrentTime = Math.Min(CurrentTime, animation.DurationSeconds);
            EvaluateAnimationPoseAtCurrentTime();
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
        set
        {
            var next = _activeAnimation == null
                ? Math.Max(0, value)
                : Math.Clamp(value, 0, _activeAnimation.DurationSeconds);
            if (Math.Abs(_currentTime - next) < 0.000001)
                return;
            this.RaiseAndSetIfChanged(ref _currentTime, next);
            _sessionState.CurrentTimeSeconds = next;
        }
    }

    public bool IsPlaying
    {
        get => _isPlaying;
        set => this.RaiseAndSetIfChanged(ref _isPlaying, value);
    }

    public bool IsCorrectionMode
    {
        get => _isCorrectionMode;
        set => this.RaiseAndSetIfChanged(ref _isCorrectionMode, value);
    }

    public bool ShowSkeleton
    {
        get => _showSkeleton;
        private set => this.RaiseAndSetIfChanged(ref _showSkeleton, value);
    }

    public bool ShowArtwork
    {
        get => _showArtwork;
        private set => this.RaiseAndSetIfChanged(ref _showArtwork, value);
    }

    public bool ShowMasks
    {
        get => _showMasks;
        private set => this.RaiseAndSetIfChanged(ref _showMasks, value);
    }

    public bool ShowGuides
    {
        get => _showGuides;
        private set => this.RaiseAndSetIfChanged(ref _showGuides, value);
    }

    public bool IsProjectPanelVisible
    {
        get => _isProjectPanelVisible;
        private set => this.RaiseAndSetIfChanged(ref _isProjectPanelVisible, value);
    }

    public bool IsInspectorPanelVisible
    {
        get => _isInspectorPanelVisible;
        private set => this.RaiseAndSetIfChanged(ref _isInspectorPanelVisible, value);
    }

    public bool IsTimelineVisible
    {
        get => _isTimelineVisible;
        private set => this.RaiseAndSetIfChanged(ref _isTimelineVisible, value);
    }

    /// <summary>Raised by the Fit Viewport command so the window can reset the viewport camera.</summary>
    public event Action? FitViewportRequested;

    // --- Export state ---

    public double ExportProgress
    {
        get => _exportProgress;
        set => this.RaiseAndSetIfChanged(ref _exportProgress, value);
    }

    public string ExportStatus
    {
        get => _exportStatus;
        set => this.RaiseAndSetIfChanged(ref _exportStatus, value);
    }

    public bool IsExporting
    {
        get => _isExporting;
        set => this.RaiseAndSetIfChanged(ref _isExporting, value);
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
    public ICommand NewCharacterCommand { get; }
    public ICommand CreateMaskCommand { get; }
    public ICommand AddKeyframeCommand { get; }
    public ICommand MirrorRigCommand { get; }
    public ICommand DeleteBoneCommand { get; }
    public ICommand PreviousFrameCommand { get; }
    public ICommand NextFrameCommand { get; }
    public ICommand GoToStartCommand { get; }
    public ICommand GoToEndCommand { get; }
    public ICommand NewAnimationCommand { get; }
    public ICommand AddBoneCommand { get; }
    public ICommand ExportDialogCommand { get; }
    public ICommand EditPivotCommand { get; }
    public ICommand ImportSeparatePartCommand { get; }
    public ICommand ToggleCorrectionModeCommand { get; }
    public ICommand CancelToolCommand { get; }
    public ICommand FitViewportCommand { get; }
    public ICommand CancelExportCommand { get; }
    public ICommand AddAnimationEventCommand { get; }
    public ICommand ExitCommand { get; }
    public ICommand AboutCommand { get; }
    public ICommand ImportArtworkCommand { get; }
    public ICommand PreferencesCommand { get; }
    public ICommand ProjectSettingsCommand { get; }
    public ICommand NewPartCommand { get; }
    public ICommand BakeAnimationCommand { get; }
    public ICommand NewExportProfileCommand { get; }
    public ICommand ToggleProjectPanelCommand { get; }
    public ICommand ToggleInspectorPanelCommand { get; }
    public ICommand ToggleTimelineCommand { get; }
    public ICommand ToggleShowSkeletonCommand { get; }
    public ICommand ToggleShowArtworkCommand { get; }
    public ICommand ToggleShowMasksCommand { get; }
    public ICommand ToggleShowGuidesCommand { get; }

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

    private Task NewProjectAsync()
    {
        try
        {
            _pendingArtworkSources.Clear();
            // Create a new in-memory project immediately — no folder picker needed.
            // User can save later via Save/SaveAs which will prompt for a directory.
            var project = new SpriteRigProject
            {
                Name = "Untitled Project",
                FormatVersion = 1,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
            var skeleton = DefaultHumanoidSkeleton.Create();
            project.Skeletons[skeleton.SkeletonId.ToKeyString()] = skeleton;

            ActiveProject = project;
            ActiveSkeleton = skeleton;
            IsDirty = true;

            // Clear undo/redo stacks for fresh project
            _undoStack.Clear();
            _redoStack.Clear();
            CanUndo = false;
            CanRedo = false;

            UpdateProjectPanel();
            EvaluateProjectPose(project);
            this.RaisePropertyChanged(nameof(Title));
            StatusMessage = "New project created. Use Save to save to disk.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        return Task.CompletedTask;
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
                _pendingArtworkSources.Clear();
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
            if (string.IsNullOrEmpty(_activeProject.ProjectDirectory))
            {
                StatusMessage = "Project has not been saved yet. Use Save As.";
                return;
            }

            PrepareProjectAssetsForSave(_activeProject, _activeProject.ProjectDirectory);
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

            PrepareProjectAssetsForSave(_activeProject, selectedPath);
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

    /// <summary>Moves the playhead from the timeline and refreshes the evaluated pose.</summary>
    public void SetCurrentTimeFromTimeline(double time)
    {
        CurrentTime = time;
        EvaluateAnimationPoseAtCurrentTime();
    }

    private void ToggleCorrectionMode()
    {
        IsCorrectionMode = !IsCorrectionMode;
        StatusMessage = IsCorrectionMode ? "Editing character corrections" : "Editing shared animation";
    }

    private async Task CheckRecoveryAsync()
    {
        try
        {
            var sessions = await _projectRepository.DetectRecoverySessionsAsync();
            if (sessions.Count > 0)
            {
                StatusMessage = $"Found {sessions.Count} recoverable session(s). Use Edit → Recover to restore.";
            }
        }
        catch { /* silently ignore recovery check failures */ }
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
            IsExporting = true;
            _exportCancellation = new CancellationTokenSource();
            ExportProgress = 0;
            ExportStatus = "Starting export...";

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
            ExportStatus = "Decoding images...";
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
            ExportStatus = $"Rendering {frameCount} frames...";
            var frames = new List<byte[]>(frameCount);
            for (int i = 0; i < frameCount; i++)
            {
                if (_exportCancellation.IsCancellationRequested)
                {
                    StatusMessage = "Export cancelled.";
                    return;
                }

                var time = _animationService.GetSampleTime(_activeAnimation, i, _activeProfile.FrameRate);
                var pose = _rigPoseEvaluator.EvaluateAnimationPose(skeleton, _activeCharacter, _activeAnimation, time);
                var framePixels = _frameRenderer.RenderFrame(pose, _activeCharacter, _activeProfile, decodedImages);
                frames.Add(framePixels);

                ExportProgress = (double)(i + 1) / frameCount;
                ExportStatus = $"Rendered frame {i + 1}/{frameCount}";
            }

            // 5. Compose spritesheet
            ExportStatus = "Composing spritesheet...";
            var sheetPixels = _spritesheetComposer.ComposeSpritesheet(
                frames,
                _activeProfile.Columns,
                _activeProfile.Rows,
                _activeProfile.FrameWidth,
                _activeProfile.FrameHeight);

            // 6. Write PNG file
            ExportStatus = "Writing files...";
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

            // 6b. Export individual frames if requested
            if (_activeProfile.IncludeIndividualFrames)
            {
                Directory.CreateDirectory(paths.FramesDirectory);
                for (int i = 0; i < frames.Count; i++)
                {
                    var framePath = System.IO.Path.Combine(paths.FramesDirectory, $"{paths.BaseName}_{i:D3}.png");
                    await _imageEncoder.EncodePngToFileAsync(framePath,
                        _activeProfile.FrameWidth, _activeProfile.FrameHeight, frames[i]);
                }
            }

            // 7. Write metadata JSON matching the spec from section 30
            var metadataObj = new
            {
                formatVersion = 1,
                characterId = _activeCharacter.CharacterRigId.ToKeyString(),
                characterName = _activeCharacter.Name,
                animationId = _activeAnimation.AnimationId.ToKeyString(),
                animationName = _activeAnimation.Name,
                framesPerSecond = _activeProfile.FrameRate > 0 ? _activeProfile.FrameRate : _activeAnimation.FramesPerSecond,
                frameCount = frameCount,
                frameWidth = _activeProfile.FrameWidth,
                frameHeight = _activeProfile.FrameHeight,
                columns = _activeProfile.Columns,
                rows = _activeProfile.Rows,
                loopMode = _activeAnimation.LoopMode.ToString(),
                anchor = new { x = _activeProfile.AnchorPixel.X, y = _activeProfile.AnchorPixel.Y },
                frames = Enumerable.Range(0, frameCount).Select(i => new
                {
                    index = i,
                    x = (i % _activeProfile.Columns) * _activeProfile.FrameWidth,
                    y = (i / _activeProfile.Columns) * _activeProfile.FrameHeight,
                    width = _activeProfile.FrameWidth,
                    height = _activeProfile.FrameHeight,
                    durationSeconds = 1.0 / (_activeProfile.FrameRate > 0 ? _activeProfile.FrameRate : _activeAnimation.FramesPerSecond)
                }).ToList()
            };
            var metadataJson = _projectSerializer.Serialize(metadataObj);
            await File.WriteAllTextAsync(paths.MetadataPath, metadataJson);

            ExportStatus = "Export complete.";
            StatusMessage = $"Export complete: {frameCount} frames → {paths.SpritesheetPath}";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Export cancelled.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export error: {ex.Message}";
        }
        finally
        {
            IsExporting = false;
            _exportCancellation?.Dispose();
            _exportCancellation = null;
        }
    }

    private async Task ExportAllAsync()
    {
        if (_activeProject == null || string.IsNullOrEmpty(_activeProject.ProjectDirectory))
        {
            StatusMessage = "Save the project before exporting.";
            return;
        }

        var profile = _activeProfile ?? _activeProject.ExportProfiles.Values.FirstOrDefault();
        if (profile == null)
        {
            StatusMessage = "Create an export profile first.";
            return;
        }

        var jobs = from character in _activeProject.CharacterRigs.Values
                   join animation in _activeProject.Animations.Values
                       on character.SkeletonId equals animation.SkeletonId
                   select (character, animation);
        var jobList = jobs.ToList();
        if (jobList.Count == 0)
        {
            StatusMessage = "No compatible character and animation pairs to export.";
            return;
        }

        var exported = 0;
        foreach (var (character, animation) in jobList)
        {
            ActiveCharacter = character;
            ActiveAnimation = animation;
            ActiveProfile = profile;
            await ExportAnimationAsync();
            if (ExportStatus == "Export complete.")
                exported++;
        }

        StatusMessage = $"Exported {exported} of {jobList.Count} animation(s).";
    }

    private void SwitchWorkspace(string workspace)
    {
        ActiveWorkspace = workspace;
        StatusMessage = $"Switched to {workspace} workspace";
    }

    private void CancelTool() => StatusMessage = "Operation cancelled.";

    private void CancelExport()
    {
        _exportCancellation?.Cancel();
        ExportStatus = "Cancelling...";
    }

    private void FitViewport()
    {
        FitViewportRequested?.Invoke();
        StatusMessage = "Fit viewport.";
    }

    private void ExitApp()
    {
        var window = _windowProvider.GetMainWindow();
        window?.Close();
    }

    private void ShowAbout() => StatusMessage = "Sprite Rig Studio v0.1.0";

    private void ShowPreferences() => StatusMessage = "Preferences (coming soon).";

    private void ShowProjectSettings() => StatusMessage = "Project settings (coming soon).";

    private void BakeAnimation() => StatusMessage = "Bake animation (coming soon).";

    private void NewExportProfile()
    {
        if (_activeProject == null) { StatusMessage = "Open a project first."; return; }
        var profile = new ExportProfile
        {
            ExportProfileId = ExportProfileId.New(),
            Name = "Default Profile",
            FrameWidth = 300,
            FrameHeight = 300,
            Columns = 4,
            Rows = 4,
            FrameRate = 12,
            BackgroundMode = BackgroundMode.Transparent,
            SamplingMode = SamplingMode.NearestNeighbor,
            AnchorPixel = new Vector2D(150, 260),
            OverflowPolicy = OverflowPolicy.FailExport
        };
        _activeProject.ExportProfiles[profile.ExportProfileId.ToKeyString()] = profile;
        MarkDirty();
        UpdateProjectPanel();
        StatusMessage = $"Created export profile: {profile.Name}";
    }

    private void ToggleProjectPanel()
    {
        IsProjectPanelVisible = !IsProjectPanelVisible;
        StatusMessage = IsProjectPanelVisible ? "Project panel visible" : "Project panel hidden";
    }

    private void ToggleInspectorPanel()
    {
        IsInspectorPanelVisible = !IsInspectorPanelVisible;
        StatusMessage = IsInspectorPanelVisible ? "Inspector panel visible" : "Inspector panel hidden";
    }

    private void ToggleTimeline()
    {
        IsTimelineVisible = !IsTimelineVisible;
        StatusMessage = IsTimelineVisible ? "Timeline visible" : "Timeline hidden";
    }

    private void ToggleShowSkeleton()
    {
        ShowSkeleton = !ShowSkeleton;
        if (_activeCharacter != null) _activeCharacter.PreviewSettings.ShowSkeleton = ShowSkeleton;
        StatusMessage = ShowSkeleton ? "Skeleton visible" : "Skeleton hidden";
    }

    private void ToggleShowArtwork()
    {
        ShowArtwork = !ShowArtwork;
        StatusMessage = ShowArtwork ? "Artwork visible" : "Artwork hidden";
    }

    private void ToggleShowMasks()
    {
        ShowMasks = !ShowMasks;
        if (_activeCharacter != null)
            _activeCharacter.PreviewSettings.ShowMasks = ShowMasks;
        StatusMessage = ShowMasks ? "Masks visible" : "Masks hidden";
    }

    private void ToggleShowGuides()
    {
        ShowGuides = !ShowGuides;
        _sessionState.ShowGuides = ShowGuides;
        StatusMessage = ShowGuides ? "Guides visible" : "Guides hidden";
    }

    private async Task AddAnimationEventAsync()
    {
        if (_activeAnimation == null) { StatusMessage = "Select an animation."; return; }
        // For MVP, just add an event at current time
        var evt = new AnimationEvent
        {
            TimeSeconds = _currentTime,
            Name = "Event",
            Parameter = null
        };
        _activeAnimation.Events.Add(evt);
        MarkDirty();
        StatusMessage = $"Added event at {_currentTime:F2}s";
        await Task.CompletedTask;
    }

    private void SetActiveTool(string toolName)
    {
        ActiveTool = toolName;
        SessionState.ActiveTool = toolName;
        StatusMessage = $"Tool: {toolName}";
    }

    private async Task NewCharacterAsync()
    {
        if (_activeProject == null) return;
        var window = _windowProvider.GetMainWindow();
        if (window == null) return;

        var skeletons = _projectPanel.Skeletons.ToList();
        if (skeletons.Count == 0)
        {
            StatusMessage = "No skeletons available. Create a skeleton first.";
            return;
        }

        var dialog = new NewCharacterDialog(skeletons) { DataContext = this };
        var result = await dialog.ShowDialog<DialogResult>(window);
        if (result != DialogResult.Ok) return;

        var name = dialog.CharacterName!;
        var skeletonId = dialog.SelectedSkeletonId!.Value;
        var artworkPath = dialog.ArtworkPath;

        // Copy artwork into project if selected
        string? artworkRef = null;
        if (!string.IsNullOrEmpty(artworkPath) && File.Exists(artworkPath))
        {
            var fileName = $"{name.ToLowerInvariant().Replace(' ', '_')}.png";
            if (_activeProject.ProjectDirectory != null)
            {
                var sourcesDir = System.IO.Path.Combine(_activeProject.ProjectDirectory, "Sources");
                Directory.CreateDirectory(sourcesDir);
                var destPath = System.IO.Path.Combine(sourcesDir, fileName);
                File.Copy(artworkPath, destPath, overwrite: true);
                artworkRef = $"Sources/{fileName}";
            }
            else
            {
                // Keep the source path temporarily; Save As will copy it into Sources/.
                artworkRef = artworkPath;
            }
        }

        var character = _rigService.CreateCharacterRig(name, skeletonId, artworkRef);

        var charId = character.CharacterRigId;
        if (!string.IsNullOrEmpty(artworkPath) && _activeProject.ProjectDirectory == null)
            _pendingArtworkSources[charId] = artworkPath;
        ExecuteAction($"Create character '{name}'",
            undo: () => { _activeProject.CharacterRigs.Remove(charId.ToKeyString()); UpdateProjectPanel(); },
            redo: () => { _activeProject.CharacterRigs[charId.ToKeyString()] = character; UpdateProjectPanel(); });

        // Select the newly created character in the panel
        var newItem = _projectPanel.Characters.FirstOrDefault(c => c.Id == character.CharacterRigId);
        if (newItem != null)
            _projectPanel.SelectedCharacter = newItem;

        OnSelectedCharacterChanged();
        StatusMessage = $"Created character: {name}";
    }

    private async Task CreateMaskAsync()
    {
        if (_activeProject == null || _activeCharacter == null)
        {
            StatusMessage = "Select a character first.";
            return;
        }
        var window = _windowProvider.GetMainWindow();
        if (window == null) return;

        var artworkPath = _activeCharacter.SourceArtwork;
        if (!string.IsNullOrEmpty(artworkPath) && !Path.IsPathRooted(artworkPath))
            artworkPath = _activeProject.ProjectDirectory != null
                ? System.IO.Path.Combine(_activeProject.ProjectDirectory, artworkPath)
                : null;

        if (artworkPath == null || !File.Exists(artworkPath))
        {
            StatusMessage = "Character has no source artwork to mask. Import artwork first.";
            return;
        }

        var dialog = new MaskEditorDialog(artworkPath);
        var result = await dialog.ShowDialog<DialogResult>(window);
        if (result == DialogResult.Ok && dialog.IsValid)
        {
            var mask = _maskService.CreateMask(dialog.MaskName);
            foreach (var v in dialog.OuterContour)
                _maskService.AddVertex(mask, v);
            if (!_maskService.ValidateMask(mask).IsValid)
            {
                StatusMessage = "Mask is invalid. Check for self-intersections and try again.";
                return;
            }

            if (!_activeProject.Skeletons.TryGetValue(_activeCharacter.SkeletonId.ToKeyString(), out var skeleton))
                return;
            var boneId = _sessionState.SelectedBoneIds.FirstOrDefault();
            if (boneId == BoneId.Empty)
                boneId = skeleton.RootBoneId;
            var part = _rigService.CreatePartFromMask(
                _activeCharacter, mask.Name, boneId, mask, _activeCharacter.SourceArtwork!);
            var maskId = mask.MaskId;
            var partId = part.SpritePartId;
            ExecuteAction($"Create mask '{mask.Name}'",
                undo: () =>
                {
                    _activeCharacter.Masks.Remove(maskId.ToString("N"));
                    _activeCharacter.SpriteParts.Remove(partId.ToKeyString());
                },
                redo: () =>
                {
                    _activeCharacter.Masks[maskId.ToString("N")] = mask;
                    _activeCharacter.SpriteParts[partId.ToKeyString()] = part;
                });
            StatusMessage = $"Created mask '{mask.Name}' with {mask.VertexCount} vertices.";
        }
    }

    private async Task AddKeyframeAsync()
    {
        if (_activeProject == null || _activeCharacter == null || _activeAnimation == null)
        {
            StatusMessage = "Select a character and animation first.";
            return;
        }
        var selectedBoneId = _sessionState.SelectedBoneIds.FirstOrDefault();
        if (selectedBoneId == BoneId.Empty)
        {
            StatusMessage = "Select a bone first.";
            return;
        }
        var window = _windowProvider.GetMainWindow();
        if (window == null) return;

        var dialog = new KeyframeEditorDialog(_currentTime);
        var dlgResult = await dialog.ShowDialog<DialogResult>(window);
        if (dlgResult == DialogResult.Ok && dialog.TimeSeconds.HasValue)
        {
            var keyframe = new TransformKeyframe
            {
                TimeSeconds = dialog.TimeSeconds.Value,
                Position = new Vector2D(dialog.PositionX, dialog.PositionY),
                RotationDegrees = dialog.RotationDegrees,
                Scale = new Vector2D(1, 1),
                Interpolation = dialog.Interpolation
            };
            var animSnapshot = _activeAnimation.AnimationId;
            var boneId = selectedBoneId;
            var time = keyframe.TimeSeconds;
            ExecuteAction($"Add keyframe at {time:F2}s",
                undo: () => { _animationService.DeleteKeyframe(_activeAnimation, boneId, time); },
                redo: () => { _animationService.AddKeyframe(_activeAnimation, boneId, keyframe.Clone()); });
            StatusMessage = $"Added keyframe at {keyframe.TimeSeconds:F2}s.";
        }
    }

    // --- New dialog command implementations ---

    private async Task NewAnimationAsync()
    {
        if (_activeProject == null) { StatusMessage = "Open or create a project first."; return; }
        if (ActiveSkeleton == null) { StatusMessage = "Select a skeleton first."; return; }
        var window = _windowProvider.GetMainWindow();
        if (window == null) return;
        var dialog = new NewAnimationDialog();
        var result = await dialog.ShowDialog<DialogResult>(window);
        if (result != DialogResult.Ok) return;
        var anim = _animationService.CreateAnimation(
            dialog.AnimationName, ActiveSkeleton.SkeletonId, dialog.Fps, dialog.Duration);
        anim.LoopMode = dialog.LoopMode;
        var animId = anim.AnimationId;
        ExecuteAction($"Create animation '{anim.Name}'",
            undo: () => { _activeProject.Animations.Remove(animId.ToKeyString()); UpdateProjectPanel(); },
            redo: () => { _activeProject.Animations[animId.ToKeyString()] = anim; UpdateProjectPanel(); });
        StatusMessage = $"Created animation: {anim.Name}";
    }

    private void MirrorRig()
    {
        if (_activeCharacter == null) { StatusMessage = "Select a character."; return; }
        ExecuteAction("Mirror rig",
            undo: () => { _activeCharacter.SetupTransform.FlipHorizontal = !_activeCharacter.SetupTransform.FlipHorizontal; RefreshPose(); },
            redo: () => { _activeCharacter.SetupTransform.FlipHorizontal = !_activeCharacter.SetupTransform.FlipHorizontal; RefreshPose(); });
    }

    private void DeleteSelectedBone()
    {
        if (ActiveSkeleton == null) { StatusMessage = "Select a skeleton."; return; }
        var selectedId = _sessionState.SelectedBoneIds.FirstOrDefault();
        if (selectedId == BoneId.Empty) { StatusMessage = "Select a bone to delete."; return; }
        var bone = ActiveSkeleton.Bones.GetValueOrDefault(selectedId.ToKeyString());
        if (bone == null) return;
        var boneName = bone.Name;
        ExecuteAction($"Delete bone '{boneName}'",
            undo: () => { ActiveSkeleton.Bones[selectedId.ToKeyString()] = new BoneDefinition { BoneId = selectedId, Name = boneName, ParentBoneId = bone.ParentBoneId }; RefreshPose(); },
            redo: () => { _skeletonService.RemoveBone(ActiveSkeleton, selectedId); RefreshPose(); });
        StatusMessage = $"Deleted bone: {boneName}";
    }

    private async Task AddBoneAsync()
    {
        if (ActiveSkeleton == null) { StatusMessage = "Select a skeleton first."; return; }
        var window = _windowProvider.GetMainWindow();
        if (window == null) return;
        var dialog = new AddBoneDialog(ActiveSkeleton);
        var result = await dialog.ShowDialog<DialogResult>(window);
        if (result != DialogResult.Ok) return;
        var addResult = _skeletonService.AddBone(
            ActiveSkeleton, dialog.BoneName, dialog.Role,
            dialog.ParentBoneId, Transform2D.Identity);
        if (addResult.IsSuccess)
        {
            MarkDirty();
            UpdateProjectPanel();
            StatusMessage = $"Added bone: {dialog.BoneName}";
        }
        else
        {
            StatusMessage = $"Failed: {addResult.ErrorMessage}";
        }
    }

    private async Task ExportDialogAsync()
    {
        if (_activeProject == null || _activeCharacter == null || _activeAnimation == null)
        { StatusMessage = "Select a character and animation first."; return; }
        var window = _windowProvider.GetMainWindow();
        if (window == null) return;
        var dialog = new ExportDialog(_activeProject.ExportProfiles.Values,
            _activeCharacter.Name, _activeAnimation.Name);
        var result = await dialog.ShowDialog<DialogResult>(window);
        if (result != DialogResult.Ok) return;
        // Set selected profile and trigger export
        if (_activeProject.ExportProfiles.TryGetValue(
            dialog.SelectedProfileId.ToKeyString(), out var profile))
        {
            ActiveProfile = profile;
            profile.IncludeIndividualFrames = dialog.ExportIndividualFrames;
            await ExportAnimationAsync();
        }
    }

    private async Task EditPivotAsync()
    {
        if (_activeCharacter == null) { StatusMessage = "Select a character."; return; }
        var selectedPartId = _sessionState.SelectedPartIds.FirstOrDefault();
        if (selectedPartId == SpritePartId.Empty) { StatusMessage = "Select a part first."; return; }
        if (!_activeCharacter.SpriteParts.TryGetValue(selectedPartId.ToKeyString(), out var part)) return;

        var window = _windowProvider.GetMainWindow();
        if (window == null) return;

        var dialog = new PivotEditorDialog(part.Pivot);
        var result = await dialog.ShowDialog<DialogResult>(window);
        if (result == DialogResult.Ok)
        {
            _rigService.UpdatePartPivot(_activeCharacter, selectedPartId, dialog.Pivot);
            MarkDirty();
            RefreshPose();
            StatusMessage = $"Pivot updated to ({dialog.Pivot.X:F1}, {dialog.Pivot.Y:F1})";
        }
    }

    private async Task ImportSeparatePartAsync()
    {
        if (_activeProject == null || _activeCharacter == null) { StatusMessage = "Select a character."; return; }
        if (!_activeProject.Skeletons.TryGetValue(_activeCharacter.SkeletonId.ToKeyString(), out var skeleton)) return;

        var window = _windowProvider.GetMainWindow();
        if (window == null) return;

        var dialog = new ImportPartDialog(skeleton);
        var result = await dialog.ShowDialog<DialogResult>(window);
        if (result != DialogResult.Ok) return;

        // Copy image into project
        var partsDir = System.IO.Path.Combine(_activeProject.ProjectDirectory!, "Parts", _activeCharacter.Name);
        Directory.CreateDirectory(partsDir);
        var fileName = $"{dialog.PartName.ToLowerInvariant().Replace(' ', '_')}.png";
        var destPath = System.IO.Path.Combine(partsDir, fileName);
        System.IO.File.Copy(dialog.ImagePath!, destPath, overwrite: true);
        var imageRef = $"Parts/{_activeCharacter.Name}/{fileName}";

        var part = _rigService.CreatePart(_activeCharacter!, dialog.PartName, dialog.BoundBoneId,
            dialog.PositionOffset, SourceType.SeparateImage, imageRef);
        var partId = part.SpritePartId;
        ExecuteAction($"Import part '{part.Name}'",
            undo: () => { _activeCharacter.SpriteParts.Remove(partId.ToKeyString()); UpdateProjectPanel(); },
            redo: () => { _activeCharacter.SpriteParts[partId.ToKeyString()] = part; UpdateProjectPanel(); });
        StatusMessage = $"Imported part: {part.Name}";
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

    /// <summary>Applies a new ground anchor selected in the viewport.</summary>
    public async Task ApplyGroundAnchorAsync(Vector2D anchor)
    {
        if (_activeCharacter != null)
        {
            _rigService.SetGroundAnchor(_activeCharacter, anchor);
            MarkDirty();
            await RefreshPoseAsync();
        }
    }

    /// <summary>Updates the editor selection after a viewport bone click.</summary>
    public void SelectBone(BoneId? boneId)
    {
        _sessionState.SelectedBoneIds.Clear();
        if (boneId is { } selected)
            _sessionState.SelectedBoneIds.Add(selected);
        SyncInspectorFromCharacter();
    }

    /// <summary>
    /// Syncs the inspector panel values from the active character's setup transform
    /// and selected part properties.
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

                var selectedBoneId = _sessionState.SelectedBoneIds.FirstOrDefault();
                if (selectedBoneId != BoneId.Empty &&
                    _activeProject?.Skeletons.TryGetValue(_activeCharacter.SkeletonId.ToKeyString(), out var selectedSkeleton) == true &&
                    selectedSkeleton.Bones.TryGetValue(selectedBoneId.ToKeyString(), out var selectedBone))
                {
                    _inspector.SelectedBoneName = selectedBone.Name;
                    _inspector.BoneRole = selectedBone.Role.ToString();
                    _inspector.BoneLength = selectedBone.ReferenceLength.ToString("F1");
                }
                else
                {
                    _inspector.SelectedBoneName = string.Empty;
                    _inspector.BoneRole = string.Empty;
                    _inspector.BoneLength = string.Empty;
                }

                // Sync selected part properties
                var selectedPartId = _sessionState.SelectedPartIds.FirstOrDefault();
                if (selectedPartId != SpritePartId.Empty &&
                    _activeCharacter.SpriteParts.TryGetValue(selectedPartId.ToKeyString(), out var part))
                {
                    _inspector.SelectedPartName = part.Name;
                    _inspector.RenderOrder = part.RenderOrder;
                    _inspector.PivotX = part.Pivot.X.ToString("F1");
                    _inspector.PivotY = part.Pivot.Y.ToString("F1");

                    if (part.BoundBoneId.HasValue && _activeProject != null &&
                        _activeProject.Skeletons.TryGetValue(_activeCharacter.SkeletonId.ToKeyString(), out var skeleton) &&
                        skeleton.Bones.TryGetValue(part.BoundBoneId.Value.ToKeyString(), out var bone))
                    {
                        _inspector.BoundBone = bone.Name;
                    }
                    else
                    {
                        _inspector.BoundBone = string.Empty;
                    }
                }
                else
                {
                    _inspector.SelectedPartName = string.Empty;
                    _inspector.RenderOrder = 0;
                    _inspector.PivotX = string.Empty;
                    _inspector.PivotY = string.Empty;
                    _inspector.BoundBone = string.Empty;
                }
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
            if (e.PropertyName == nameof(InspectorViewModel.RenderOrder))
            {
                var selectedPartId = _sessionState.SelectedPartIds.FirstOrDefault();
                if (selectedPartId != SpritePartId.Empty)
                {
                    _rigService.UpdatePartRenderOrder(_activeCharacter, selectedPartId, _inspector.RenderOrder);
                    MarkDirty();
                }
                return;
            }

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

    private void PrepareProjectAssetsForSave(SpriteRigProject project, string targetDirectory)
    {
        Directory.CreateDirectory(targetDirectory);
        var pathMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var character in project.CharacterRigs.Values)
        {
            if (!string.IsNullOrWhiteSpace(character.SourceArtwork))
            {
                var source = character.SourceArtwork!;
                if (_pendingArtworkSources.TryGetValue(character.CharacterRigId, out var pending))
                    source = pending;

                if (Path.IsPathRooted(source) && File.Exists(source))
                {
                    var relative = $"Sources/{SanitizeAssetName(Path.GetFileName(source))}";
                    character.SourceArtwork = CopyAsset(source, relative, targetDirectory, pathMap, project);
                }
                else if (!Path.IsPathRooted(source))
                {
                    character.SourceArtwork = source.Replace('\\', '/');
                }
            }

            foreach (var part in character.SpriteParts.Values)
            {
                if (string.IsNullOrWhiteSpace(part.ImageReference))
                    continue;

                var source = part.ImageReference!;
                if (Path.IsPathRooted(source) && File.Exists(source))
                {
                    var relative = $"Parts/{SanitizeAssetName(character.Name)}/{SanitizeAssetName(Path.GetFileName(source))}";
                    part.ImageReference = CopyAsset(source, relative, targetDirectory, pathMap, project);
                }
                else if (!Path.IsPathRooted(source))
                {
                    part.ImageReference = source.Replace('\\', '/');
                }
            }
        }
    }

    private static string CopyAsset(
        string source,
        string relativePath,
        string targetDirectory,
        Dictionary<string, string> pathMap,
        SpriteRigProject project)
    {
        if (pathMap.TryGetValue(source, out var existingRelative))
            return existingRelative;

        var normalized = relativePath.Replace('\\', '/');
        var destination = Path.Combine(targetDirectory, normalized.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        File.Copy(source, destination, overwrite: true);
        var info = new FileInfo(source);
        project.AssetReferences[normalized] = new AssetReference
        {
            RelativePath = normalized,
            OriginalFileName = Path.GetFileName(source),
            SizeBytes = info.Length,
            Category = Path.GetExtension(source).TrimStart('.').ToLowerInvariant()
        };
        pathMap[source] = normalized;
        return normalized;
    }

    private static string SanitizeAssetName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(value.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
        return sanitized.Trim().ToLowerInvariant().Replace(' ', '_');
    }

}



