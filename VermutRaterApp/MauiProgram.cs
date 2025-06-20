using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Microsoft.Maui;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;
using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Maui.Core.Handlers;


namespace VermutRaterApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()                    // ya lo tienes
            .UseMauiCommunityToolkitMediaElement()        // ← AÑADE ESTA
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("PlayfairDisplay-Regular.ttf", "PlayfairDisplay");
            })
            .ConfigureMauiHandlers(handlers =>
             {
                 handlers.AddHandler(typeof(MediaElement), typeof(MediaElementHandler));
             }); ;

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
