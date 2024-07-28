
using System.Diagnostics;
using System.Reflection;
using Microsoft.Maui.Graphics;

namespace Plugin.Maui.DebugOverlay;

public class DebugOverlay : WindowOverlay
{
    private DebugRibbonElement _debugRibbonElement;

    private Color _ribbonColor;
    private string _mauiVersion;
    
    public DebugOverlay(IWindow window, Color ribbonColor = null ) : base(window)
    {
        _ribbonColor = ribbonColor;
        _debugRibbonElement = new DebugRibbonElement(this, ribbonColor: ribbonColor);
        this.AddWindowElement(_debugRibbonElement);
        Debug.WriteLine("DebugOverlay created AND element added");
        this.Tapped += DebugOverlay_Tapped;

        var version = typeof(MauiApp).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        _mauiVersion = $"{version?[..version.IndexOf('+')]}";
    }

    private void DebugOverlay_Tapped(object? sender, WindowOverlayTappedEventArgs e)
    {
        Debug.WriteLine("Tapped");
        if (_debugRibbonElement.Contains(e.Point))
        {
            bool debugMode = _debugRibbonElement.LabelText.Contains("DEBUG");
            // The tap is on the _debugRibbonElement
            Debug.WriteLine($"Tapped on _debugRibbonElement {debugMode}");
            this.RemoveWindowElement(_debugRibbonElement);
            _debugRibbonElement = new DebugRibbonElement(this, 
                labelText: (debugMode) ? _mauiVersion : "DEBUG",
                ribbonColor:_ribbonColor);
            this.AddWindowElement(_debugRibbonElement);
        }
        else
        {
            // The tap is not on the _debugRibbonElement
            Debug.WriteLine("Tapped outside of _debugRibbonElement");
        }

    }

    public override void HandleUIChange()
    {
        base.HandleUIChange();
    }


}
