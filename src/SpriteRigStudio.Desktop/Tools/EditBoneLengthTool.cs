using Avalonia.Input;
using SpriteRigStudio.Desktop.Viewport;

namespace SpriteRigStudio.Desktop.Tools;

public class EditBoneLengthTool : ITool
{
    public string Name => "Edit Bone Length";
    public Cursor Cursor => new(StandardCursorType.SizeNorthSouth);

    public void OnPointerDown(ViewportControl viewport, PointerPressedEventArgs e) { }
    public void OnPointerMove(ViewportControl viewport, PointerEventArgs e) { }
    public void OnPointerUp(ViewportControl viewport, PointerReleasedEventArgs e) { }
    public void OnKeyDown(ViewportControl viewport, KeyEventArgs e) { }
    public void Cancel() { }
}
