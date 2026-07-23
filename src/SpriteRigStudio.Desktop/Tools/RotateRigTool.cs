using Avalonia;
using Avalonia.Input;
using SpriteRigStudio.Desktop.Viewport;

namespace SpriteRigStudio.Desktop.Tools;

/// <summary>
/// Tool for rotating the entire character rig in the viewport.
/// </summary>
public class RotateRigTool : ITool
{
    private bool _isDragging;
    private Point _lastPosition;
    private double _startRotation;

    public string Name => "RotateRig";

    public Cursor Cursor => new Cursor(StandardCursorType.Cross);

    public void OnPointerDown(ViewportControl viewport, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(viewport).Properties.IsLeftButtonPressed)
        {
            _isDragging = true;
            _lastPosition = e.GetPosition(viewport);
            _startRotation = viewport.ActiveRotation;
            e.Handled = true;
        }
    }

    public void OnPointerMove(ViewportControl viewport, PointerEventArgs e)
    {
        if (_isDragging)
        {
            var current = e.GetPosition(viewport);
            var center = new Point(viewport.Bounds.Width / 2, viewport.Bounds.Height / 2);
            var startAngle = System.Math.Atan2(_lastPosition.Y - center.Y, _lastPosition.X - center.X);
            var currentAngle = System.Math.Atan2(current.Y - center.Y, current.X - center.X);
            var deltaAngle = (currentAngle - startAngle) * 180.0 / System.Math.PI;

            viewport.ActiveRotation = _startRotation + deltaAngle;
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
                newTransform.RotationDegrees = viewport.ActiveRotation;
                _ = viewport.OnApplyRigTransform.Invoke(newTransform);
            }

            viewport.ActiveRotation = 0;
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
