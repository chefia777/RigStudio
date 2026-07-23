using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows.Input;
using ReactiveUI;
using SpriteRigStudio.Application.Animations;
using SpriteRigStudio.Application.Exporting;
using SpriteRigStudio.Application.Projects;
using SpriteRigStudio.Application.Rigs;
using SpriteRigStudio.Application.Skeletons;
using SpriteRigStudio.Domain.Animations;
using SpriteRigStudio.Domain.Common;
using SpriteRigStudio.Domain.Exporting;
using SpriteRigStudio.Domain.Projects;
using SpriteRigStudio.Domain.Rigs;
using SpriteRigStudio.Domain.Skeletons;

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
        ExportService exportService)
    {
        _projectService = projectService;
        _skeletonService = skeletonService;
        _rigService = rigService;
        _animationService = animationService;
        _exportService = exportService;

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
            var result = await _projectService.CreateProjectAsync("Untitled Project", GetDefaultProjectDirectory());
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
            // TODO: Show folder picker dialog
            var path = PromptForProjectDirectory();
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

    private Task SaveProjectAsAsync()
    {
        // TODO: Show folder picker dialog
        return Task.CompletedTask;
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

    private Task ExportAnimationAsync()
    {
        if (_activeProject == null || _activeCharacter == null || _activeAnimation == null || _activeProfile == null)
        {
            StatusMessage = "Select a character, animation, and export profile to export.";
            return Task.CompletedTask;
        }

        try
        {
            var validation = _exportService.ValidateExport(_activeProject, _activeCharacter, _activeAnimation, _activeProfile);
            if (!validation.IsValid)
            {
                StatusMessage = $"Export validation failed: {string.Join("; ", validation.All.Select(i => i.Message))}";
                return Task.CompletedTask;
            }

            // TODO: Execute export via rendering pipeline
            StatusMessage = $"Exporting animation '{_activeAnimation.Name}'...";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export error: {ex.Message}";
        }
        return Task.CompletedTask;
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

    private static string? PromptForProjectDirectory()
    {
        // TODO: Integrate with Avalonia folder picker dialog
        return null;
    }
}
