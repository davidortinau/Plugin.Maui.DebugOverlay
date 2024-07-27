
using System.Diagnostics;
using Microsoft.Maui.Graphics;

namespace Plugin.Maui.DebugOverlay;

public class DebugOverlay : WindowOverlay
{
    private readonly DebugRibbonElement _debugRibbonElement;
    
    public DebugOverlay(IWindow window, Color ribbonColor = null ) : base(window)
    {
        _debugRibbonElement = new DebugRibbonElement(ribbonColor);
        this.AddWindowElement(_debugRibbonElement);
        Debug.WriteLine("DebugOverlay created AND element added");
    }
}
