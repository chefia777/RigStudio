using System;
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

            // ── Sync viewport state when active character changes ──
            vm.PropertyChanged += (s, args) =>
            {
                if (args.PropertyName == nameof(MainWindowViewModel.ActiveCharacter))
                {
                    SyncViewportFromViewModel(vm);
                    LoadArtworkForViewport(vm);
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
        }
        else
        {
            Viewport.CurrentSetupTransform = null;
        }
    }

    /// <summary>
    /// Loads the active character's source artwork into the viewport, or clears it if none.
    /// </summary>
    private void LoadArtworkForViewport(MainWindowViewModel vm)
    {
        var character = vm.ActiveCharacter;
        var project = vm.ActiveProject;

        if (character?.SourceArtwork != null && project?.ProjectDirectory != null)
        {
            var path = System.IO.Path.Combine(project.ProjectDirectory, character.SourceArtwork);
            Viewport.LoadArtwork(path);
        }
        else
        {
            Viewport.LoadArtwork(null);
        }
    }
}
