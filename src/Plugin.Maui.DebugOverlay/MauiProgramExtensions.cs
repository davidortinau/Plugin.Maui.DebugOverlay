using Microsoft.Maui.Handlers;
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

            if (options.ShowLoadTime)
            { 
                // Add metrics for load VisualElement (including layouts, controls)
                ViewHandler.ViewMapper.AppendToMapping("MeasureComponentLoad", (handler, view) =>
                {
                    if (view is VisualElement ve)
                    {
                        var swLoaded = Stopwatch.StartNew();
                        //var swHandlerChanged = Stopwatch.StartNew();


                        //Here the difference is subtle but important.
                        //we want to know when the element is actually loaded and ready, not just when its handler is set ?!
                        //HandlerChanged can be too early in some cases
                        //Loaded can be too late in some cases (like if the element is never shown)

                        //ve.HandlerChanged += (_, __) =>
                        //{
                        //    if (swHandlerChanged.IsRunning)
                        //    {
                        //        swHandlerChanged.Stop();
                        //        overlay?.AddMetricElementLoad(ve.Id, ve.GetType().Name, swHandlerChanged.Elapsed.TotalMilliseconds);
                        //    }
                        //};

                        //here we only track the element when it is actually loaded (which means it is part of the visual tree and has a size)
                        ve.Loaded += (_, __) =>
                        {
                            if (swLoaded.IsRunning)
                            {
                                swLoaded.Stop();
                                DebugOverlayPanel.AddMetricElementLoad(ve.Id, ve.GetType().Name, swLoaded.Elapsed.TotalMilliseconds);
                            }
                        };
                    }
                });
            }

#endif

        });


        return builder;
    } 
}