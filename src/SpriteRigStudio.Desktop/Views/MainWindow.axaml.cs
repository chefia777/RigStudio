using System;
using System.Linq;
using Avalonia.Controls;
using SpriteRigStudio.Desktop.ViewModels;

namespace SpriteRigStudio.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is MainWindowViewModel vm)
        {
            // ── Pose sync ──────────────────────────────────────────
            vm.PropertyChanged += (s, args) =>
            {
                if (args.PropertyName == nameof(MainWindowViewModel.CurrentPose))
                    Viewport.EvaluatedPose = vm.CurrentPose;
            };
            Viewport.EvaluatedPose = vm.CurrentPose;

            // ── Tool callbacks ─────────────────────────────────────
            Viewport.OnApplyRigTransform = async (transform) =>
            {
                await vm.ApplyRigTransformAsync(transform);
            };

            Viewport.OnApplyBoneOverride = async (boneId, override_) =>
            {
                await vm.ApplyBoneOverrideAsync(boneId, override_);
            };

            Viewport.OnApplyGroundAnchor = async anchor =>
            {
                await vm.ApplyGroundAnchorAsync(anchor);
            };

            Viewport.OnBoneSelected = boneId =>
            {
                vm.SelectBone(boneId);
                SyncViewportFromViewModel(vm);
            };

            // ── Sync viewport state when active character changes ──
            vm.PropertyChanged += (s, args) =>
            {
                if (args.PropertyName == nameof(MainWindowViewModel.ActiveCharacter))
                {
                    SyncViewportFromViewModel(vm);
                    LoadArtworkForViewport(vm);
                }
                else if (args.PropertyName == nameof(MainWindowViewModel.ActiveTool) ||
                         args.PropertyName == nameof(MainWindowViewModel.ShowSkeleton) ||
                         args.PropertyName == nameof(MainWindowViewModel.ShowArtwork) ||
                         args.PropertyName == nameof(MainWindowViewModel.ShowGuides))
                {
                    SyncViewportFromViewModel(vm);
                }
            };

            // Initial sync
            SyncViewportFromViewModel(vm);
            LoadArtworkForViewport(vm);
        }
    }

    /// <summary>
    /// Pushes the active character's state down to the ViewportControl
    /// so tools can read the current transform, bone overrides, etc.
    /// </summary>
    private void SyncViewportFromViewModel(MainWindowViewModel vm)
    {
        if (vm.ActiveCharacter != null)
        {
            Viewport.CurrentSetupTransform = vm.ActiveCharacter.SetupTransform;
            Viewport.SelectedBoneId = vm.SessionState.SelectedBoneIds.FirstOrDefault();
            if (Viewport.SelectedBoneId is { } selectedBoneId &&
                vm.ActiveCharacter.BoneSetupOverrides.TryGetValue(selectedBoneId.ToKeyString(), out var boneOverride))
            {
                Viewport.CurrentBoneOverridePosition = boneOverride.LocalPosition;
                Viewport.CurrentBoneOverrideRotation = boneOverride.LocalRotationDegrees;
            }
            else
            {
                Viewport.CurrentBoneOverridePosition = null;
                Viewport.CurrentBoneOverrideRotation = null;
            }
            Viewport.ShowSkeleton = vm.ShowSkeleton;
        }
        else
        {
            Viewport.CurrentSetupTransform = null;
            Viewport.SelectedBoneId = null;
        }

        Viewport.ShowArtwork = vm.ShowArtwork;
        Viewport.ShowGuides = vm.ShowGuides;
        Viewport.SetActiveTool(vm.ActiveTool);
    }

    /// <summary>
    /// Loads the active character's source artwork into the viewport, or clears it if none.
    /// </summary>
    private void LoadArtworkForViewport(MainWindowViewModel vm)
    {
        var character = vm.ActiveCharacter;
        var project = vm.ActiveProject;

        if (character?.SourceArtwork != null)
        {
            var path = System.IO.Path.IsPathRooted(character.SourceArtwork)
                ? character.SourceArtwork
                : project?.ProjectDirectory != null
                    ? System.IO.Path.Combine(project.ProjectDirectory, character.SourceArtwork)
                    : null;
            Viewport.LoadArtwork(path);
        }
        else
        {
            Viewport.LoadArtwork(null);
        }
    }
}
