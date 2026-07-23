using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using SpriteRigStudio.Domain.Geometry;

namespace SpriteRigStudio.Desktop.Dialogs;

public partial class MaskEditorDialog : Window
{
    public string MaskName => MaskNameBox.Text ?? "Part";
    public List<Vector2D> OuterContour => Canvas.GetContour();
    public bool IsValid => Canvas.Vertices.Count >= 3;

    public MaskEditorDialog(string? artworkPath = null)
    {
        InitializeComponent();
        Canvas.LoadBackground(artworkPath);
    }

    private void OnAddVertex(object? sender, RoutedEventArgs e)
    {
        // Vertex addition is done by clicking on the canvas
    }

    private void OnRemoveVertex(object? sender, RoutedEventArgs e)
    {
        // Vertex removal is done by clicking on a vertex with the tool active
    }

    private void OnClosePolygon(object? sender, RoutedEventArgs e)
    {
        Canvas.ClosePolygon();
    }

    private void OnClear(object? sender, RoutedEventArgs e)
    {
        Canvas.ClearVertices();
    }

    private void OnSave(object? sender, RoutedEventArgs e)
    {
        if (!IsValid)
        {
            // Can't save without enough vertices
            return;
        }
        Close(DialogResult.Ok);
    }

    private void OnCancel(object? sender, RoutedEventArgs e)
    {
        Close(DialogResult.Cancel);
    }
}
