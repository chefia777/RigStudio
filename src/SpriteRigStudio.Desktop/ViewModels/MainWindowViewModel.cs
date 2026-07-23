using System;
using System.Collections.ObjectModel;
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
using SpriteRigStudio.Domain.Projects;
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

    // --- Panel view models ---
    private readonly ProjectPanelViewModel _projectPanel;
    private readonly InspectorViewModel _inspector;

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

        // Initialize commands
        NewProjectCommand = ReactiveCommand.CreateFromTask(NewProjectAsync);
        OpenProjectCommand = ReactiveCommand.CreateFromTask(OpenProjectAsync);
        SaveProjectCommand = ReactiveCommand.CreateFromTask(SaveProjectAsync, this.WhenAnyValue(x => x.IsDirty));
        SaveProjectAsCommand = ReactiveCommand.CreateFromTask(SaveProjectAsAsync);
        UndoCommand = ReactiveCommand.Create(Undo, this.WhenAnyValue(x => x.CanUndo));
        RedoCommand = ReactiveCommand.Create(Redo, this.WhenAnyValue(x => x.CanRedo));
        PlayPauseCommand = ReactiveCommand.Create(PlayPause);
        StopCommand = ReactiveCommand.Create(Stop);
        ExportAnimationCommand = ReactiveCommand.CreateFromTask(ExportAnimationAsync);
        ExportAllCommand = ReactiveCommand.CreateFromTask(ExportAllAsync);
        SwitchWorkspaceCommand = ReactiveCommand.Create<string>(SwitchWorkspace);
        SetActiveToolCommand = ReactiveCommand.Create<string>(SetActiveTool);
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

var folders = await  window.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = "Select Project Directory",  AllowMultiple = false });
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

var folders = await  window.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = "Select Save Directory",  AllowMultiple = false });
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
        // TODO: Implement undo stack
        StatusMessage = "Undo";
    }

    private void Redo()
    {
        // TODO: Implement redo stack
        StatusMessage = "Redo";
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
        StatusMessage = "Stopped";
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

    private static string GetDefaultProjectDirectory()
    {
        var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        return System.IO.Path.Combine(documents, "SpriteRigStudio", "Projects");
    }

}

