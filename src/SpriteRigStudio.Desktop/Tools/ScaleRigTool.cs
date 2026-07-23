using System;
using Avalonia;
using Avalonia.Input;
using SpriteRigStudio.Desktop.Viewport;

namespace SpriteRigStudio.Desktop.Tools;

/// <summary>
/// Tool for scaling the entire character rig in the viewport.
/// </summary>
public class ScaleRigTool : ITool
{
    private bool _isDragging;
    private Point _lastPosition;
    private double _startScale;
    private double _baseDistance;

    public string Name => "ScaleRig";

    public Cursor Cursor => new Cursor(StandardCursorType.SizeAll);

    public void OnPointerDown(ViewportControl viewport, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(viewport).Properties.IsLeftButtonPressed)
        {
            _isDragging = true;
            _lastPosition = e.GetPosition(viewport);
            _startScale = viewport.ActiveScale;
            var center = new Point(viewport.Bounds.Width / 2, viewport.Bounds.Height / 2);
            _baseDistance = Math.Sqrt(
                Math.Pow(_lastPosition.X - center.X, 2) +
                Math.Pow(_lastPosition.Y - center.Y, 2));
            if (_baseDistance < 1) _baseDistance = 1;
            e.Handled = true;
        }
    }

    public void OnPointerMove(ViewportControl viewport, PointerEventArgs e)
    {
        if (_isDragging)
        {
            var current = e.GetPosition(viewport);
            var center = new Point(viewport.Bounds.Width / 2, viewport.Bounds.Height / 2);
            var currentDistance = Math.Sqrt(
                Math.Pow(current.X - center.X, 2) +
                Math.Pow(current.Y - center.Y, 2));

            var scaleFactor = currentDistance / _baseDistance;
            viewport.ActiveScale = Math.Clamp(_startScale * scaleFactor, 0.1, 10.0);
            viewport.InvalidateVisual();
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
                var newTransform = currentTransform.Clone();
                newTransform.UniformScale = viewport.ActiveScale;
                _ = viewport.OnApplyRigTransform.Invoke(newTransform);
            }

            viewport.ActiveScale = 1.0;
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
