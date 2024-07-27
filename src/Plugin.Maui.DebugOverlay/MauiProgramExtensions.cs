using Microsoft.Maui.Handlers;
using Microsoft.Maui.LifecycleEvents;
using System.Diagnostics;

namespace Plugin.Maui.DebugOverlay;
public static class MauiProgramExtensions
{
	public static MauiAppBuilder UseDebugRibbon(this MauiAppBuilder builder, Color ribbonColor = null)
	{
		builder.ConfigureMauiHandlers(handlers =>
		{
            #if DEBUG
			WindowHandler.Mapper.AppendToMapping("AddDebugOverlay", (handler, view) =>
            {
                Debug.WriteLine("Adding DebugOverlay");
                var overlay = new DebugOverlay(handler.VirtualView, ribbonColor);
                handler.VirtualView.AddOverlay(overlay);
                    
            });
			#endif
			
		});
        

		return builder;
	}
}