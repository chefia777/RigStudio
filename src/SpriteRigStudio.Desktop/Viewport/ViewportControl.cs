using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using SpriteRigStudio.Desktop.Tools;
using SpriteRigStudio.Domain.Common;
using SpriteRigStudio.Domain.Geometry;
using SpriteRigStudio.Domain.Retargeting;
using SpriteRigStudio.Domain.Rigs;

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

    // ── Callbacks wired by MainWindow code-behind ──────────────────────

    /// <summary>
    /// Called when a rig transform tool completes (move, rotate, scale).
    /// Receives the final <see cref="CharacterSetupTransform"/> to apply to the active character.
    /// </summary>
    public Func<CharacterSetupTransform, Task>? OnApplyRigTransform { get; set; }

    /// <summary>
    /// Called when a bone-level tool completes (move joint, rotate bone).
    /// Receives the bone ID and the final <see cref="BoneSetupOverride"/> to apply.
    /// </summary>
    public Func<BoneId, BoneSetupOverride, Task>? OnApplyBoneOverride { get; set; }

    // ── Tool-accessible state set by MainWindow code-behind ────────────

    /// <summary>The active character's current setup transform (read by move/rotate/scale tools).</summary>
    public CharacterSetupTransform? CurrentSetupTransform { get; set; }

    /// <summary>The currently selected bone (used by bone-level tools).</summary>
    public BoneId? SelectedBoneId { get; set; }

    /// <summary>Current bone override position offset before a drag begins.</summary>
    public Vector2D? CurrentBoneOverridePosition { get; set; }

    /// <summary>Current bone override rotation offset before a drag begins.</summary>
    public double? CurrentBoneOverrideRotation { get; set; }

    // ── Properties used by tool implementations (auto-invalidate) ─────

    private double _dragOffsetX;
    private double _dragOffsetY;
    private double _activeRotation;
    private double _activeScale = 1.0;
    private double _activeBoneRotation;

    /// <summary>Screen-space X offset for visual drag feedback.</summary>
    public double DragOffsetX
    {
        get => _dragOffsetX;
        set { _dragOffsetX = value; InvalidateVisual(); }
    }

    /// <summary>Screen-space Y offset for visual drag feedback.</summary>
    public double DragOffsetY
    {
        get => _dragOffsetY;
        set { _dragOffsetY = value; InvalidateVisual(); }
    }

    public Point SelectionStart { get; set; }
    public Point SelectionEnd { get; set; }
    public bool ShowSelectionRect { get; set; }

    /// <summary>Visual rotation angle in degrees applied during a rotate drag.</summary>
    public double ActiveRotation
    {
        get => _activeRotation;
        set { _activeRotation = value; InvalidateVisual(); }
    }

    /// <summary>Visual uniform scale factor applied during a scale drag.</summary>
    public double ActiveScale
    {
        get => _activeScale;
        set { _activeScale = value; InvalidateVisual(); }
    }

    /// <summary>Visual bone rotation angle in degrees during a bone rotate drag.</summary>
    public double ActiveBoneRotation
    {
        get => _activeBoneRotation;
        set { _activeBoneRotation = value; InvalidateVisual(); }
    }

    public double Zoom { get => _zoom; set { _zoom = Math.Clamp(value, 0.05, 20.0); InvalidateVisual(); } }
    public double PanX { get => _panX; set { _panX = value; InvalidateVisual(); } }
    public double PanY { get => _panY; set { _panY = value; InvalidateVisual(); } }
    public bool ShowGuides { get => _showGuides; set { _showGuides = value; InvalidateVisual(); } }
    public bool ShowSkeleton { get => _showSkeleton; set { _showSkeleton = value; InvalidateVisual(); } }
    public EvaluatedPose? EvaluatedPose { get => _evaluatedPose; set { _evaluatedPose = value; InvalidateVisual(); } }
    public string ActiveTool { get => _activeTool; set => _activeTool = value; }

    // ── Artwork rendering ─────────────────────────────────────────

    private Bitmap? _artworkBitmap;

    /// <summary>
    /// Loads a character artwork PNG and displays it centered in the viewport.
    /// Pass null or an empty string to clear the artwork.
    /// </summary>
    public void LoadArtwork(string? filePath)
    {
        _artworkBitmap?.Dispose();
        _artworkBitmap = null;

        if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            _artworkBitmap = new Bitmap(filePath);

        InvalidateVisual();
    }

    /// <summary>Gets the screen position of the currently selected joint (simplified placeholder).</summary>
    public Point? GetSelectedJointScreenPosition() => null;

    /// <summary>Commits a joint movement as a single undoable action.</summary>
    public void CommitJointMovement()
    {
        if (OnApplyBoneOverride != null && SelectedBoneId.HasValue)
        {
            var worldDx = _dragOffsetX / _zoom;
            var worldDy = -_dragOffsetY / _zoom;
            var currentPos = CurrentBoneOverridePosition ?? Vector2D.Zero;
            var currentRot = CurrentBoneOverrideRotation ?? 0;

            var override_ = new BoneSetupOverride
            {
                BoneId = SelectedBoneId.Value,
                LocalPosition = new Vector2D(currentPos.X + worldDx, currentPos.Y + worldDy),
                LocalRotationDegrees = currentRot + _activeBoneRotation,
                LocalScale = new Vector2D(1, 1),
                Enabled = true
            };
            OnApplyBoneOverride(SelectedBoneId.Value, override_);
        }
        _dragOffsetX = 0;
        _dragOffsetY = 0;
        _activeBoneRotation = 0;
        InvalidateVisual();
    }

    /// <summary>Commits a bone rotation as a single undoable action.</summary>
    public void CommitBoneRotation()
    {
        if (OnApplyBoneOverride != null && SelectedBoneId.HasValue)
        {
            var currentRot = CurrentBoneOverrideRotation ?? 0;
            var currentPos = CurrentBoneOverridePosition ?? Vector2D.Zero;

            var override_ = new BoneSetupOverride
            {
                BoneId = SelectedBoneId.Value,
                LocalPosition = currentPos,
                LocalRotationDegrees = currentRot + _activeBoneRotation,
                LocalScale = new Vector2D(1, 1),
                Enabled = true
            };
            OnApplyBoneOverride(SelectedBoneId.Value, override_);
        }
        _activeBoneRotation = 0;
        InvalidateVisual();
    }

    public ViewportControl()
    {
        _toolManager = new ToolManager();
        Focusable = true;
        ClipToBounds = true;
        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch;
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

    // --- Layout ---

    protected override Size MeasureOverride(Size availableSize)
    {
        // Take all available space — without this, a bare Control
        // measures as (0,0) and collapses star-sized grid columns.
        return availableSize;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        return finalSize;
    }

    // --- Rendering ---

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var bounds = Bounds;

        // Always draw a visible background
        context.FillRectangle(Brushes.DimGray, bounds);

        if (bounds.Width <= 0 || bounds.Height <= 0) return;

        try
        {
            using (context.PushTransform(Matrix.CreateTranslation(bounds.Width / 2 + _panX, bounds.Height / 2 + _panY)))
            using (context.PushTransform(Matrix.CreateScale(_zoom, -_zoom)))
            {
                // Draw artwork first (background layer)
                DrawArtwork(context);

                // Draw guides
                if (_showGuides) DrawGuides(context);

                // Draw skeleton
                if (_showSkeleton) DrawSkeleton(context);
            }
        }
        catch
        {
            // Silently handle render errors — background already drawn
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

    private void DrawArtwork(DrawingContext context)
    {
        if (_artworkBitmap == null) return;

        var srcRect = new Rect(0, 0, _artworkBitmap.Size.Width, _artworkBitmap.Size.Height);
        var halfW = _artworkBitmap.Size.Width / 2.0;
        var halfH = _artworkBitmap.Size.Height / 2.0;
        var destRect = new Rect(-halfW, -halfH, _artworkBitmap.Size.Width, _artworkBitmap.Size.Height);

        context.DrawImage(_artworkBitmap, srcRect, destRect);
    }
}
