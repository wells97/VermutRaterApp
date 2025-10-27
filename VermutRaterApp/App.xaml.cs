using Microsoft.Maui.Controls;
using VermutRaterApp.Services;
using VermutRaterApp.Views;

namespace VermutRaterApp;

public partial class App : Application
{

    public App(INotificationManagerService notificacions)
    {
        InitializeComponent();
        MainPage = new AppShell();


    }
}


