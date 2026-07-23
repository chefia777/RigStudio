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
                    SyncViewportFromViewModel(vm);
            };

            // Initial sync
            SyncViewportFromViewModel(vm);
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
}
