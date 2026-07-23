using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using SpriteRigStudio.Domain.Geometry;

namespace SpriteRigStudio.Desktop.Controls;

/// <summary>
/// Custom control for drawing and editing polygon masks.
/// </summary>
public class MaskCanvas : Control
{
    private readonly List<Point> _vertices = new();
    private int _draggingIndex = -1;
    private int _hoverIndex = -1;
    private Bitmap? _backgroundImage;
    private bool _polygonClosed;

    public IReadOnlyList<Point> Vertices => _vertices.AsReadOnly();
    public bool PolygonClosed => _polygonClosed;

    public void LoadBackground(string? imagePath)
    {
        _backgroundImage?.Dispose();
        _backgroundImage = null;
        if (imagePath != null && File.Exists(imagePath))
            _backgroundImage = new Bitmap(imagePath);
        InvalidateVisual();
    }

    public void AddVertex(Point p)
    {
        if (!_polygonClosed)
        {
            _vertices.Add(p);
            InvalidateVisual();
        }
    }

    public void RemoveVertex(Point p)
    {
        if (_vertices.Count <= 3) return;
        var idx = FindNearestVertex(p, 10);
        if (idx >= 0)
        {
            _vertices.RemoveAt(idx);
            InvalidateVisual();
        }
    }

    public void ClearVertices()
    {
        _vertices.Clear();
        _polygonClosed = false;
        InvalidateVisual();
    }

    public void ClosePolygon()
    {
        if (_vertices.Count >= 3)
            _polygonClosed = true;
        InvalidateVisual();
    }

    public List<Vector2D> GetContour() =>
        _vertices.Select(v => new Vector2D(v.X, v.Y)).ToList();

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        var pos = e.GetPosition(this);
        _draggingIndex = FindNearestVertex(pos, 8);
        if (_draggingIndex < 0 && !_polygonClosed)
        {
            _vertices.Add(pos);
            _draggingIndex = _vertices.Count - 1;
        }
        InvalidateVisual();
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        var pos = e.GetPosition(this);
        _hoverIndex = FindNearestVertex(pos, 8);
        if (_draggingIndex >= 0 && _draggingIndex < _vertices.Count)
        {
            _vertices[_draggingIndex] = pos;
            InvalidateVisual();
        }
        else
        {
            Cursor = _hoverIndex >= 0 ? new Cursor(StandardCursorType.Hand) : Cursor;
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        _draggingIndex = -1;
    }

    private int FindNearestVertex(Point p, double threshold)
    {
        for (int i = 0; i < _vertices.Count; i++)
        {
            var dx = _vertices[i].X - p.X;
            var dy = _vertices[i].Y - p.Y;
            if (Math.Sqrt(dx * dx + dy * dy) <= threshold)
                return i;
        }
        return -1;
    }

    public override void Render(DrawingContext context)
    {
        // Draw background image
        if (_backgroundImage != null)
        {
            var srcRect = new Rect(0, 0, _backgroundImage.Size.Width, _backgroundImage.Size.Height);
            context.DrawImage(_backgroundImage, srcRect, srcRect);
        }

        if (_vertices.Count == 0) return;

        // Draw edges
        var edgePen = new Pen(Brushes.Lime, 2);
        for (int i = 0; i < _vertices.Count - 1; i++)
            context.DrawLine(edgePen, _vertices[i], _vertices[i + 1]);

        // Draw closing edge if polygon is closed
        if (_polygonClosed && _vertices.Count >= 3)
            context.DrawLine(edgePen, _vertices[^1], _vertices[0]);

        // Draw vertices
        foreach (var v in _vertices)
            context.DrawEllipse(Brushes.Yellow, null, v, 4, 4);

        // Highlight hovered vertex
        if (_hoverIndex >= 0 && _hoverIndex < _vertices.Count)
            context.DrawEllipse(Brushes.Orange, null, _vertices[_hoverIndex], 6, 6);
    }
}
