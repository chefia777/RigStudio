using System.Threading.Tasks;
using Avalonia;
using Avalonia.Input;
using SpriteRigStudio.Desktop.Viewport;
using SpriteRigStudio.Domain.Geometry;

namespace SpriteRigStudio.Desktop.Tools;

public class AnchorTool : ITool
{
    public string Name => "Anchor";
    public Cursor Cursor => new(StandardCursorType.Cross);

    private Point _position;

    public void OnPointerDown(ViewportControl viewport, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(viewport).Properties.IsLeftButtonPressed)
        {
            _position = e.GetPosition(viewport);
            e.Handled = true;
        }
    }

    public void OnPointerMove(ViewportControl viewport, PointerEventArgs e) { }

    public void OnPointerUp(ViewportControl viewport, PointerReleasedEventArgs e)
    {
        if (viewport.OnApplyGroundAnchor != null)
        {
            var world = viewport.ScreenToWorld(_position);
            _ = viewport.OnApplyGroundAnchor(new Vector2D(world.X, world.Y));
        }
        e.Handled = true;
    }
    public void OnKeyDown(ViewportControl viewport, KeyEventArgs e) { }
    public void Cancel() { }
}
