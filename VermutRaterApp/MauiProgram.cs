using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.LifecycleEvents;
using Plugin.LocalNotification;
using VermutRaterApp.Services;
using VermutRaterApp.Views;

namespace VermutRaterApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .UseLocalNotification()
                .UseMauiCommunityToolkitMediaElement()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("PlayfairDisplay-Regular.ttf", "PlayfairDisplay");
                    fonts.AddFont("DancingScript-Regular.ttf", "DancingScript");
                });
            // 🔧 Forzar color de CheckBox en Android
            CheckBoxHandler.Mapper.AppendToMapping("ColorFix", (handler, view) =>
            {
#if ANDROID
                handler.PlatformView.ButtonTintList = Android.Content.Res.ColorStateList.ValueOf(
                    Android.Graphics.Color.ParseColor("#D4AF37")
                );
        
#endif
            });
            builder.ConfigureLifecycleEvents(events =>
            {
#if ANDROID
                events.AddAndroid(android => android.OnCreate((activity, bundle) =>
                {
                    activity.Window?.SetStatusBarColor(Android.Graphics.Color.Black); // o el color que quieras
                    activity.Window?.SetNavigationBarColor(Android.Graphics.Color.Black);
                }));
#endif
            });
            // 🔐 Registre de serveis
            builder.Services.AddSingleton<INotificationManagerService, NotificationManagerService>();
            builder.Services.AddSingleton<MainPage>();
            builder.Services.AddSingleton<LoadingPage>();
            builder.Services.AddTransient<LoginPage>();



#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
