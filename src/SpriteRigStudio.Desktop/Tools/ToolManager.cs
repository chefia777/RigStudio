using System.Collections.Generic;
using Avalonia.Input;
using SpriteRigStudio.Desktop.Viewport;

namespace SpriteRigStudio.Desktop.Tools;

/// <summary>
/// Manages the active tool and dispatches viewport events to it.
/// </summary>
public class ToolManager
{
    private readonly Dictionary<string, ITool> _tools = new();
    private ITool? _activeTool;

    /// <summary>Event raised when the active tool changes.</summary>
    public event System.EventHandler<string>? ActiveToolChanged;

    /// <summary>Gets the name of the currently active tool.</summary>
    public string ActiveToolName => _activeTool?.Name ?? "Select";

    /// <summary>Gets the cursor for the active tool.</summary>
    public Cursor ActiveCursor => _activeTool?.Cursor ?? new Cursor(StandardCursorType.Arrow);

    /// <summary>
    /// Registers a tool with the manager.
    /// </summary>
    public void RegisterTool(ITool tool)
    {
        _tools[tool.Name] = tool;
    }

    /// <summary>
    /// Sets the active tool by name.
    /// </summary>
    public void SetActiveTool(string toolName)
    {
        if (_activeTool != null)
            _activeTool.Cancel();

        _activeTool = _tools.TryGetValue(toolName, out var tool) ? tool : null;
        ActiveToolChanged?.Invoke(this, toolName);
    }

    /// <summary>
    /// Gets a registered tool by name.
    /// </summary>
    public ITool? GetTool(string toolName) =>
        _tools.TryGetValue(toolName, out var tool) ? tool : null;

    /// <summary>
    /// Dispatches a pointer-down event to the active tool.
    /// </summary>
    public void DispatchPointerDown(ViewportControl viewport, PointerPressedEventArgs e)
    {
        _activeTool?.OnPointerDown(viewport, e);
    }

    /// <summary>
    /// Dispatches a pointer-move event to the active tool.
    /// </summary>
    public void DispatchPointerMove(ViewportControl viewport, PointerEventArgs e)
    {
        _activeTool?.OnPointerMove(viewport, e);
    }

    /// <summary>
    /// Dispatches a pointer-up event to the active tool.
    /// </summary>
    public void DispatchPointerUp(ViewportControl viewport, PointerReleasedEventArgs e)
    {
        _activeTool?.OnPointerUp(viewport, e);
    }

    /// <summary>
    /// Dispatches a key-down event to the active tool.
    /// </summary>
    public void DispatchKeyDown(ViewportControl viewport, KeyEventArgs e)
    {
        _activeTool?.OnKeyDown(viewport, e);
    }

    /// <summary>
    /// Cancels the active tool's current operation.
    /// </summary>
    public void CancelActiveTool()
    {
        _activeTool?.Cancel();
    }
}
