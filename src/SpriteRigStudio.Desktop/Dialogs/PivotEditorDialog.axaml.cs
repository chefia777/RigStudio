using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using SpriteRigStudio.Domain.Geometry;

namespace SpriteRigStudio.Desktop.Dialogs;

public partial class PivotEditorDialog : Window
{
    public Vector2D Pivot => new(
        double.Parse(PivotXBox.Text ?? "0"),
        double.Parse(PivotYBox.Text ?? "0"));

    public PivotEditorDialog(Vector2D currentPivot)
    {
        InitializeComponent();
        PivotXBox.Text = currentPivot.X.ToString("F1");
        PivotYBox.Text = currentPivot.Y.ToString("F1");
    }

    private void OnOk(object? sender, RoutedEventArgs e) => Close(DialogResult.Ok);
    private void OnCancel(object? sender, RoutedEventArgs e) => Close(DialogResult.Cancel);
}
