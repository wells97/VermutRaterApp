using VermutRaterApp.Models;
using VermutRaterApp.Services;

namespace VermutRaterApp.Views;

public partial class LoadingPage : ContentPage
{
    private readonly INotificationManagerService _notificacions;

    public LoadingPage(INotificationManagerService notificacions)
    {
        InitializeComponent();
        _notificacions = notificacions;

    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            // Carregar vermuts des de Firebase
            var vermuts = await FirebaseService.CargarVermutsConDatosLocalesAsync();

            //  Anar a la MainPage
            //await Task.Delay(6000);
            Application.Current.MainPage = new NavigationPage(new MainPage(_notificacions));
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No s'han pogut carregar els vermuts: {ex.Message}", "OK");
        }
    }
}
