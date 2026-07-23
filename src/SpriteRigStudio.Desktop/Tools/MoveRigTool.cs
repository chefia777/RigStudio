using Avalonia;
using Avalonia.Input;
using SpriteRigStudio.Desktop.Viewport;

namespace SpriteRigStudio.Desktop.Tools;

/// <summary>
/// Tool for translating the entire character rig in the viewport.
/// </summary>
public class MoveRigTool : ITool
{
    private bool _isDragging;
    private Point _lastPosition;

    public string Name => "MoveRig";

    public Cursor Cursor => new Cursor(StandardCursorType.SizeAll);

    public void OnPointerDown(ViewportControl viewport, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(viewport).Properties.IsLeftButtonPressed)
        {
            _isDragging = true;
            _lastPosition = e.GetPosition(viewport);
            e.Handled = true;
        }
    }

    public void OnPointerMove(ViewportControl viewport, PointerEventArgs e)
    {
        if (_isDragging)
        {
            var current = e.GetPosition(viewport);
            var delta = current - _lastPosition;

            viewport.PanX += delta.X;
            viewport.PanY += delta.Y;

            _lastPosition = current;
            viewport.InvalidateVisual();
            e.Handled = true;
        }
    }

    public void OnPointerUp(ViewportControl viewport, PointerReleasedEventArgs e)
    {
        if (_isDragging)
        {
            _isDragging = false;
            e.Handled = true;
        }
    }

    public void OnKeyDown(ViewportControl viewport, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Cancel();
            e.Handled = true;
        }
    }

    public void Cancel()
    {
        _isDragging = false;
    }
}
