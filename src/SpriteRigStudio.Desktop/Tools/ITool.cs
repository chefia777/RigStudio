using Avalonia.Input;
using SpriteRigStudio.Desktop.Viewport;

namespace SpriteRigStudio.Desktop.Tools;

/// <summary>
/// Defines an interactive tool that operates on the viewport.
/// </summary>
public interface ITool
{
    /// <summary>Display name for the tool.</summary>
    string Name { get; }

    /// <summary>Cursor to show when this tool is active.</summary>
    Cursor Cursor { get; }

    /// <summary>Called when a pointer button is pressed in the viewport.</summary>
    void OnPointerDown(ViewportControl viewport, PointerPressedEventArgs e);

    /// <summary>Called when the pointer moves in the viewport.</summary>
    void OnPointerMove(ViewportControl viewport, PointerEventArgs e);

    /// <summary>Called when a pointer button is released in the viewport.</summary>
    void OnPointerUp(ViewportControl viewport, PointerReleasedEventArgs e);

    /// <summary>Called when a key is pressed while the tool is active.</summary>
    void OnKeyDown(ViewportControl viewport, KeyEventArgs e);

    /// <summary>Cancels the current tool operation.</summary>
    void Cancel();
}
