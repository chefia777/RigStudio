using Avalonia.Input;
using SpriteRigStudio.Desktop.Viewport;

namespace SpriteRigStudio.Desktop.Tools;

public class MovePartTool : ITool
{
    public string Name => "Move Part";
    public Cursor Cursor => new(StandardCursorType.SizeAll);

    public void OnPointerDown(ViewportControl viewport, PointerPressedEventArgs e) { }
    public void OnPointerMove(ViewportControl viewport, PointerEventArgs e) { }
    public void OnPointerUp(ViewportControl viewport, PointerReleasedEventArgs e) { }
    public void OnKeyDown(ViewportControl viewport, KeyEventArgs e) { }
    public void Cancel() { }
}
