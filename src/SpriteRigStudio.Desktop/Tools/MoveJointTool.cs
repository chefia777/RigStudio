using Avalonia;
using Avalonia.Input;
using SpriteRigStudio.Desktop.Viewport;

namespace SpriteRigStudio.Desktop.Tools;

/// <summary>
/// Tool for moving an individual joint (bone position) in the viewport.
/// </summary>
public class MoveJointTool : ITool
{
    private bool _isDragging;
    private Point _lastPosition;

    public string Name => "MoveJoint";

    public Cursor Cursor => new Cursor(StandardCursorType.Hand);

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

            // Move the selected joint by delta (in screen pixels)
            // Convert to world coordinates and apply to selected bone
            viewport.DragOffsetX += delta.X;
            viewport.DragOffsetY += delta.Y;

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

            // Commit the joint movement
            viewport.CommitJointMovement();
            viewport.DragOffsetX = 0;
            viewport.DragOffsetY = 0;
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
