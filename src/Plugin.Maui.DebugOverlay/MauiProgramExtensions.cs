using Microsoft.Maui.Handlers;
using Plugin.Maui.DebugOverlay.Utils;
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
            LoadTimeMetricsStore loadTimeMetricsStore = new LoadTimeMetricsStore();

            WindowHandler.Mapper.AppendToMapping("AddDebugOverlay", (handler, view) =>
            {
                Debug.WriteLine("Adding DebugOverlay");
                var overlay = new DebugOverlay(handler.VirtualView, options, loadTimeMetricsStore);
                handler.VirtualView.AddOverlay(overlay);
            });

            if (options.ShowLoadTime)
            {
                PageHandler.Mapper.AppendToMapping("ClearLoadTimeMetrics", (handler, view) =>
                {
                    // Clear metrics as soon as the page is created
                    loadTimeMetricsStore.Clear();
                });

                // Add metrics for load VisualElement (including layouts, controls)
                ViewHandler.ViewMapper.AppendToMapping("MeasureComponentLoad", (handler, view) =>
                {
                    if (view is VisualElement ve)
                    {
                        #region track loading
                        var swLoaded = Stopwatch.StartNew();

                         ve.HandlerChanged += (_, __) =>
                        {
                            if (swLoaded.IsRunning)
                            {
                                swLoaded.Stop();
                                loadTimeMetricsStore.Add(ve.Id, swLoaded.Elapsed.TotalMilliseconds);
                            }
                        };
                        #endregion
                    }
                });
            }
#endif

        });


        return builder;
    }
}