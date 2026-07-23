using Avalonia;
using Avalonia.Input;
using SpriteRigStudio.Desktop.Viewport;

namespace SpriteRigStudio.Desktop.Tools;

/// <summary>
/// Select tool for picking bones, parts, and other viewport elements.
/// Supports click-to-select and drag-to-select rectangle.
/// </summary>
public class SelectTool : ITool
{
    private bool _isDragging;
    private Point _dragStart;
    private Point _dragEnd;

    public string Name => "Select";

    public Cursor Cursor => new Cursor(StandardCursorType.Arrow);

    public void OnPointerDown(ViewportControl viewport, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(viewport).Properties.IsLeftButtonPressed)
        {
            _isDragging = true;
            _dragStart = e.GetPosition(viewport);
            _dragEnd = _dragStart;
            viewport.SelectionStart = _dragStart;
            viewport.SelectionEnd = _dragEnd;
            viewport.ShowSelectionRect = true;
            e.Handled = true;
        }
        else if (e.GetCurrentPoint(viewport).Properties.IsMiddleButtonPressed)
        {
            // Middle-button initiates panning - handled by viewport
            e.Handled = false;
        }
    }

    public void OnPointerMove(ViewportControl viewport, PointerEventArgs e)
    {
        if (_isDragging)
        {
            _dragEnd = e.GetPosition(viewport);
            viewport.SelectionEnd = _dragEnd;
            e.Handled = true;
        }
    }

    public void OnPointerUp(ViewportControl viewport, PointerReleasedEventArgs e)
    {
        if (_isDragging)
        {
            _isDragging = false;
            viewport.ShowSelectionRect = false;
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
