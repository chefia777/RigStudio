using System;
using Avalonia;
using Avalonia.Input;
using SpriteRigStudio.Desktop.Viewport;

namespace SpriteRigStudio.Desktop.Tools;

/// <summary>
/// Tool for rotating an individual bone in the viewport.
/// </summary>
public class RotateBoneTool : ITool
{
    private bool _isDragging;
    private Point _lastPosition;
    private double _startBoneRotation;

    public string Name => "RotateBone";

    public Cursor Cursor => new Cursor(StandardCursorType.Cross);

    public void OnPointerDown(ViewportControl viewport, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(viewport).Properties.IsLeftButtonPressed)
        {
            _isDragging = true;
            _lastPosition = e.GetPosition(viewport);
            _startBoneRotation = viewport.ActiveBoneRotation;
            e.Handled = true;
        }
    }

    public void OnPointerMove(ViewportControl viewport, PointerEventArgs e)
    {
        if (_isDragging)
        {
            var current = e.GetPosition(viewport);
            var jointScreenPos = viewport.GetSelectedJointScreenPosition();
            if (jointScreenPos == null) return;

            var startAngle = Math.Atan2(_lastPosition.Y - jointScreenPos.Value.Y, _lastPosition.X - jointScreenPos.Value.X);
            var currentAngle = Math.Atan2(current.Y - jointScreenPos.Value.Y, current.X - jointScreenPos.Value.X);
            var deltaAngle = (currentAngle - startAngle) * 180.0 / Math.PI;

            viewport.ActiveBoneRotation = _startBoneRotation + deltaAngle;
            viewport.InvalidateVisual();
            e.Handled = true;
        }
    }

    public void OnPointerUp(ViewportControl viewport, PointerReleasedEventArgs e)
    {
        if (_isDragging)
        {
            _isDragging = false;

            // Commit the bone rotation
            viewport.CommitBoneRotation();
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
        _lastPosition = default;
        _startBoneRotation = 0;
    }
}
