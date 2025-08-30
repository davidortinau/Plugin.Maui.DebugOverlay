using Microsoft.Maui;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.LifecycleEvents;
using System.Diagnostics;

namespace Plugin.Maui.DebugOverlay;
public static class MauiProgramExtensions
{
    public static MauiAppBuilder UseDebugRibbon(this MauiAppBuilder builder, Action<DebugRibbonOptions>? configure = null)
    {
        var options = new DebugRibbonOptions();
        configure?.Invoke(options);

        builder.ConfigureMauiHandlers(handlers =>
        {

#if DEBUG
            WindowHandler.Mapper.AppendToMapping("AddDebugOverlay", (handler, view) =>
            {
                Debug.WriteLine("Adding DebugOverlay");
                var overlay = new DebugOverlay(handler.VirtualView, options);
                handler.VirtualView.AddOverlay(overlay);

            });
#endif

        });


        return builder;
    }
}