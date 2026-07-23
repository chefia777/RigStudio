using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using SpriteRigStudio.Desktop.Tools;
using SpriteRigStudio.Domain.Geometry;
using SpriteRigStudio.Domain.Retargeting;

namespace SpriteRigStudio.Desktop.Viewport;

/// <summary>
/// Interactive viewport for rendering character rigs and animations.
/// Uses Avalonia DrawingContext for rendering. SkiaSharp is used only in the
/// Rendering layer for export and mask rasterization.
/// </summary>
public class ViewportControl : Control
{
    private readonly ToolManager _toolManager;
    private bool _isPanning;
    private Point _panStart;
    private double _panStartX;
    private double _panStartY;

    private double _zoom = 1.0;
    private double _panX;
    private double _panY;
    private bool _showGuides = true;
    private bool _showSkeleton = true;
    private EvaluatedPose? _evaluatedPose;
    private string _activeTool = "Select";

    // Properties used by tool implementations
    public double DragOffsetX { get; set; }
    public double DragOffsetY { get; set; }
    public Point SelectionStart { get; set; }
    public Point SelectionEnd { get; set; }
    public bool ShowSelectionRect { get; set; }
    public double ActiveRotation { get; set; }
    public double ActiveScale { get; set; } = 1.0;
    public double ActiveBoneRotation { get; set; }

    public double Zoom { get => _zoom; set { _zoom = Math.Clamp(value, 0.05, 20.0); InvalidateVisual(); } }
    public double PanX { get => _panX; set { _panX = value; InvalidateVisual(); } }
    public double PanY { get => _panY; set { _panY = value; InvalidateVisual(); } }
    public bool ShowGuides { get => _showGuides; set { _showGuides = value; InvalidateVisual(); } }
    public bool ShowSkeleton { get => _showSkeleton; set { _showSkeleton = value; InvalidateVisual(); } }
    public EvaluatedPose? EvaluatedPose { get => _evaluatedPose; set { _evaluatedPose = value; InvalidateVisual(); } }
    public string ActiveTool { get => _activeTool; set => _activeTool = value; }

    /// <summary>Gets the screen position of the currently selected joint (simplified placeholder).</summary>
    public Point? GetSelectedJointScreenPosition() => null;

    /// <summary>Commits a joint movement as a single undoable action.</summary>
    public void CommitJointMovement() { }
    /// <summary>Commits a bone rotation as a single undoable action.</summary>
    public void CommitBoneRotation() { }

    public ViewportControl()
    {
        _toolManager = new ToolManager();
        Focusable = true;
        ClipToBounds = true;
    }

    public Point ScreenToWorld(Point screen)
    {
        var bounds = Bounds;
        var worldX = (screen.X - bounds.Width / 2 - _panX) / _zoom;
        var worldY = -(screen.Y - bounds.Height / 2 - _panY) / _zoom;
        return new Point(worldX, worldY);
    }

    public Point WorldToScreen(Point world)
    {
        var bounds = Bounds;
        var screenX = world.X * _zoom + bounds.Width / 2 + _panX;
        var screenY = -world.Y * _zoom + bounds.Height / 2 + _panY;
        return new Point(screenX, screenY);
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        var delta = e.Delta.Y;
        var oldZoom = _zoom;
        var mousePos = e.GetPosition(this);

        // Zoom around cursor
        var worldBefore = ScreenToWorld(mousePos);
        _zoom = Math.Clamp(_zoom * (delta > 0 ? 1.15 : 1.0 / 1.15), 0.05, 20.0);
        var worldAfter = ScreenToWorld(mousePos);

        // Adjust pan to keep world point under cursor
        _panX += (worldAfter.X - worldBefore.X) * _zoom;
        _panY -= (worldAfter.Y - worldBefore.Y) * _zoom;

        InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        var point = e.GetPosition(this);
        if (e.GetCurrentPoint(this).Properties.IsMiddleButtonPressed)
        {
            _isPanning = true;
            _panStart = point;
            _panStartX = _panX;
            _panStartY = _panY;
            e.Handled = true;
            return;
        }

        _toolManager.DispatchPointerDown(this, e);
        Focus();
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        var point = e.GetPosition(this);
        if (_isPanning)
        {
            _panX = _panStartX + (point.X - _panStart.X);
            _panY = _panStartY + (point.Y - _panStart.Y);
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        _toolManager.DispatchPointerMove(this, e);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        if (_isPanning)
        {
            _isPanning = false;
            e.Handled = true;
            return;
        }

        _toolManager.DispatchPointerUp(this, e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        _toolManager.DispatchKeyDown(this, e);
    }

    // --- Rendering ---

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var bounds = Bounds;
        if (bounds.Width <= 0 || bounds.Height <= 0) return;

        context.FillRectangle(Brushes.DimGray, bounds);

        using (context.PushTransform(Matrix.CreateTranslation(bounds.Width / 2 + _panX, bounds.Height / 2 + _panY)))
        using (context.PushTransform(Matrix.CreateScale(_zoom, -_zoom)))
        {
            if (_showGuides) DrawGuides(context);
            if (_showSkeleton) DrawSkeleton(context);
        }
    }

    private void DrawGuides(DrawingContext context)
    {
        var guidePen = new Pen(Brushes.Gray, 1.0 / _zoom);

        // Ground line
        context.DrawLine(guidePen, new Point(-5000, 0), new Point(5000, 0));

        // Center line
        context.DrawLine(guidePen, new Point(0, -5000), new Point(0, 5000));
    }

    private void DrawSkeleton(DrawingContext context)
    {
        if (_evaluatedPose == null) return;

        var bonePen = new Pen(Brushes.LimeGreen, 2.0 / _zoom);
        var jointSize = 4.0 / _zoom;

        foreach (var kvp in _evaluatedPose.BoneWorldTransforms)
        {
            var x = kvp.Value.M31;
            var y = kvp.Value.M32;

            // Draw joint
            context.DrawEllipse(Brushes.Yellow, null, new Point(x, y), jointSize, jointSize);

            // Draw bone line to parent
            if (_evaluatedPose.BoneLocalTransforms.TryGetValue(kvp.Key, out var local))
            {
                var parentX = x - local.Position.X;
                var parentY = y - local.Position.Y;
                context.DrawLine(bonePen, new Point(parentX, parentY), new Point(x, y));
            }
        }
    }
}
