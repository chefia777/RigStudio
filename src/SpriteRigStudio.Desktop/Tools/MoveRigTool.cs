using System;
using Avalonia;
using Avalonia.Input;
using SpriteRigStudio.Desktop.Viewport;
using SpriteRigStudio.Domain.Geometry;
using SpriteRigStudio.Domain.Rigs;

namespace SpriteRigStudio.Desktop.Tools;

/// <summary>
/// Tool for translating the entire character rig in the viewport.
/// Provides visual feedback via DragOffsetX/Y and commits the new
/// transform through <see cref="ViewportControl.OnApplyRigTransform"/>.
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

            // Update visual offset (screen-space delta)
            viewport.DragOffsetX += delta.X;
            viewport.DragOffsetY += delta.Y;

            _lastPosition = current;
            e.Handled = true;
        }
    }

    public void OnPointerUp(ViewportControl viewport, PointerReleasedEventArgs e)
    {
        if (_isDragging)
        {
            _isDragging = false;

            var currentTransform = viewport.CurrentSetupTransform;
            if (currentTransform != null && viewport.OnApplyRigTransform != null)
            {
                // Convert screen-space drag delta to world-space delta
                double worldDx = viewport.DragOffsetX / viewport.Zoom;
                double worldDy = -viewport.DragOffsetY / viewport.Zoom;

                var newTransform = currentTransform.Clone();
                newTransform.Position = new Vector2D(
                    currentTransform.Position.X + worldDx,
                    currentTransform.Position.Y + worldDy);

                // Fire-and-forget the async callback
                _ = viewport.OnApplyRigTransform.Invoke(newTransform);
            }

            // Reset visual offset
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
