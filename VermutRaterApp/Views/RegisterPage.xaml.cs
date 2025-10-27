using CommunityToolkit.Maui.Alerts;
using VermutRaterApp.Services;

namespace VermutRaterApp.Views;

public partial class RegisterPage : ContentPage
{
    private readonly LoginService _loginService = new();

    public RegisterPage()
    {
        InitializeComponent();
    }

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        var email = EmailEntry.Text;
        var password = PasswordEntry.Text;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            await Toast.Make("Has d'omplir tots els camps.").Show();
            return;
        }

        RegisterButton.IsVisible = false;
        LoadingSpinner.IsVisible = true;
        LoadingSpinner.IsRunning = true;

        try
        {
            bool resultat = await _loginService.RegisterAsync(email, password);

            if (resultat)
            {
                await Toast.Make("Compte creat. Ja pots iniciar sessió!").Show();
                await Navigation.PopAsync(); // torna enrere a LoginPage
            }
        }
        catch (Exception ex)
        {
            //var missatge = GetMissatgeErrorAmable(ex.Message);
          //  await Toast.Make(missatge).Show();
        }
        finally
        {
            RegisterButton.IsVisible = true;
            LoadingSpinner.IsVisible = false;
            LoadingSpinner.IsRunning = false;
        }
    }
    private async void MapaButton_Clicked(object sender, EventArgs e)
    {
        try
        {
            await Launcher.Default.OpenAsync("https://maps.app.goo.gl/5HVoXcjwVF7SSjrZ7");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "No se pudo abrir el mapa: " + ex.Message, "OK");
        }
    }


    private void OnEntryFocused(object sender, FocusEventArgs e)
    {
        if (sender == EmailEntry)
            EmailFrame.BorderColor = Color.FromArgb("#FFD700");
        else if (sender == PasswordEntry)
            PasswordFrame.BorderColor = Color.FromArgb("#FFD700");
    }

    private void OnEntryUnfocused(object sender, FocusEventArgs e)
    {
        if (sender == EmailEntry)
            EmailFrame.BorderColor = Color.FromArgb("#E6C17A");
        else if (sender == PasswordEntry)
            PasswordFrame.BorderColor = Color.FromArgb("#E6C17A");
    }

}
