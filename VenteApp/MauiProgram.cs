using Microcharts.Maui;
using Microsoft.Maui.LifecycleEvents;

namespace VenteApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>().ConfigureLifecycleEvents(events =>
            {
#if ANDROID
                events.AddAndroid(android => android
                    .OnCreate((activity, bundle) => CartService.Instance.LoadCart())
                    .OnStop(activity => CartService.Instance.SaveCart())
                    .OnDestroy(activity => CartService.Instance.SaveCart())
                );
#endif

#if IOS
            events.AddiOS(ios => ios
                .WillTerminate(application => CartService.Instance.SaveCart())
                .DidEnterBackground(application => CartService.Instance.SaveCart())
                .WillEnterForeground(application => CartService.Instance.LoadCart())
            );
#endif
            })
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("FluentSystemIcons-Regular.ttf", "FluentIcons");
            });

        // Initialize Microcharts
        builder.UseMicrocharts();

        return builder.Build();
    }
}
