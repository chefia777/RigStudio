using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using SpriteRigStudio.Domain.Animations;
using SpriteRigStudio.Domain.Geometry;

namespace SpriteRigStudio.Desktop.Dialogs;

public partial class KeyframeEditorDialog : Window
{
    public double? TimeSeconds => double.TryParse(TimeBox.Text, out var t) ? t : null;
    public double PositionX => double.TryParse(PosXBox.Text, out var x) ? x : 0;
    public double PositionY => double.TryParse(PosYBox.Text, out var y) ? y : 0;
    public double RotationDegrees => double.TryParse(RotBox.Text, out var r) ? r : 0;
    public InterpolationMode Interpolation => (InterpolationMode)InterpCombo.SelectedIndex;

    public KeyframeEditorDialog() : this(0)
    {
    }

    public KeyframeEditorDialog(double initialTime)
    {
        InitializeComponent();
        InterpCombo.ItemsSource = new[] { "Step", "Linear", "Smooth" };
        InterpCombo.SelectedIndex = 1; // Linear
        TimeBox.Text = initialTime.ToString("F3");
    }

    private void OnAdd(object? sender, RoutedEventArgs e)
    {
        if (!double.TryParse(TimeBox.Text, out _))
            return;
        Close(DialogResult.Ok);
    }

    private void OnCancel(object? sender, RoutedEventArgs e)
    {
        Close(DialogResult.Cancel);
    }
}
