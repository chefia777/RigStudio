using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using SpriteRigStudio.Domain.Animations;

namespace SpriteRigStudio.Desktop.Dialogs;

/// <summary>
/// Dialog for creating a new animation clip: name, FPS, duration, and loop mode.
/// </summary>
public partial class NewAnimationDialog : Window
{
    /// <summary>Animation name entered by the user.</summary>
    public string AnimationName => NameBox.Text ?? string.Empty;

    /// <summary>Frames per second (default 12).</summary>
    public int Fps => (int)(FpsBox.Value ?? 12);

    /// <summary>Duration in seconds (default 1.0).</summary>
    public double Duration => double.TryParse(DurationBox.Text, out var d) ? d : 1.0;

    /// <summary>Loop mode selected by the user.</summary>
    public LoopMode LoopMode => (LoopMode)LoopModeCombo.SelectedIndex;

    public NewAnimationDialog()
    {
        InitializeComponent();

        LoopModeCombo.ItemsSource = new[] { "Loop", "Once", "PingPong", "Clamp" };
        LoopModeCombo.SelectedIndex = 0;
    }

    private void OnCreate(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameBox.Text))
            return;

        if (!double.TryParse(DurationBox.Text, out var duration) || duration <= 0)
            return;

        Close(DialogResult.Ok);
    }

    private void OnCancel(object? sender, RoutedEventArgs e)
    {
        Close(DialogResult.Cancel);
    }
}
