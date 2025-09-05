using Microsoft.Extensions.DependencyInjection;
using Plugin.Maui.DebugOverlay;

namespace Plugin.Maui.DebugOverlay.Sample;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseDebugRibbon(options =>
            {
                options.SetRibbonColor(Color.FromArgb("#FF3300"))
                    .EnableBatteryUsage()
                    .EnableGC()
                    .EnableCPU()
                    .EnableMemory()
                    .EnableFrame()
                    .EnableLoadTimePerComponents(800, 850);
            })
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddTransient<MainPage>();

        return builder.Build();
    }
}