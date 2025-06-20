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
            await DisplayAlert("Camp buit", "Has d'omplir tots els camps.", "OK");
            return;
        }

        RegisterButton.IsVisible = false;
        LoadingSpinner.IsVisible = true;
        LoadingSpinner.IsRunning = true;

        bool resultat = await _loginService.RegisterAsync(email, password);

        RegisterButton.IsVisible = true;
        LoadingSpinner.IsVisible = false;
        LoadingSpinner.IsRunning = false;

        if (resultat)
        {
            await DisplayAlert("Compte creat", "Ja pots iniciar sessió!", "OK");
            await Navigation.PopAsync(); // torna enrere a LoginPage
        }
    }
}
