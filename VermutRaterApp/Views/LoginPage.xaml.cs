using VermutRaterApp.Services;
using VermutRaterApp.Views;

namespace VermutRaterApp.Views;

public partial class LoginPage : ContentPage
{
    private readonly LoginService _loginService = new();

    public LoginPage()
    {
        InitializeComponent();
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        var email = EmailEntry.Text;
        var password = PasswordEntry.Text;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            await DisplayAlert("Camp buit", "Has d'omplir tots els camps.", "OK");
            return;
        }

        LoginButton.IsVisible = false;
        LoadingSpinner.IsVisible = true;
        LoadingSpinner.IsRunning = true;

        bool resultat = await _loginService.LoginComplet(email, password);

        LoginButton.IsVisible = true;
        LoadingSpinner.IsVisible = false;
        LoadingSpinner.IsRunning = false;

        if (resultat)
        {
            await Navigation.PushAsync(new MainPage());
        }
    }

    private async void OnRegisterButtonClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new RegisterPage());
    }
}
