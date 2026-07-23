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
            vm.PropertyChanged += (s, args) =>
            {
                if (args.PropertyName == nameof(MainWindowViewModel.CurrentPose))
                    Viewport.EvaluatedPose = vm.CurrentPose;
            };
            // Initial sync
            Viewport.EvaluatedPose = vm.CurrentPose;
        }
    }
}
