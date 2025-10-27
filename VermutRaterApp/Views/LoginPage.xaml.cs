using CommunityToolkit.Maui.Alerts;
using FirebaseAdmin.Auth;
using System.Diagnostics;
using System.Globalization;
using VermutRaterApp.Helpers;
using VermutRaterApp.Models;
using VermutRaterApp.Services;

namespace VermutRaterApp.Views;

public partial class LoginPage : ContentPage
{
    private readonly LoginService _loginService = new();
    private static readonly Color HighlightBg = Color.FromArgb("#1AE6C17A"); // 10%
    private static readonly Color HighlightBorder = Color.FromArgb("#E6C17A");
    private static readonly Color FieldBg = Color.FromArgb("#2B0E0C");   // tu base

    public LoginPage(INotificationManagerService notificacions)
    {
        InitializeComponent();
        // Establecer idioma guardado al iniciar
        string idiomaActual = Preferences.Get("idioma", "ca");
        LocalizationResourceManager.Instance.SetCulture(new CultureInfo(idiomaActual));
        IdiomaIcono.Source = idiomaActual == "ca" ? "flag_ca.png" : "flag_es.png";
        IdiomaTexto.Text = idiomaActual == "ca" ? "CA" : "ES";

        Idioma.idioma = idiomaActual;

        NavigationPage.SetHasNavigationBar(this, false);
        NavigationPage.SetHasBackButton(this, false);
        Shell.SetNavBarIsVisible(this, false);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        NavigationPage.SetHasNavigationBar(this, false);
        NavigationPage.SetHasBackButton(this, false);
        Shell.SetNavBarIsVisible(this, false);
    }
    private void OnEntryFocused(object sender, FocusEventArgs e)
    {
        if (sender is Entry entry && entry.Parent is Grid g && g.Parent is Frame f)
        {
            g.BackgroundColor = HighlightBg;
            f.BorderColor = HighlightBorder;
        }
    }

    private void OnEntryUnfocused(object sender, FocusEventArgs e)
    {
        if (sender is Entry entry && entry.Parent is Grid g && g.Parent is Frame f)
        {
            g.BackgroundColor = FieldBg;
            f.BorderColor = HighlightBorder; // o deja tu color por defecto
        }
    }

    //TODO: Crear una classe ErrorMessageManager o algo aixi per a tirar desde alli els errors

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        var email = EmailEntry.Text?.Trim();
        var password = PasswordEntry.Text?.Trim();

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            string msg = _loginService.GetMissatgeErrorAmable("EMPTY_FIELDS");
            await Toast.Make(msg).Show();
            return;
        }

        LoginButton.IsEnabled = false;
        LoginButton.Opacity = 0.6;
        LoadingSpinner.IsVisible = true;
        LoadingSpinner.IsRunning = true;

        try
        {
            // 1) Login en Firebase (REST) → obtiene IdToken y Uid
            await FirebaseAuthHolder.SignInWithEmailPasswordAsync(email, password);
            Usuario.Email = email;
            Usuario.UID = FirebaseAuthHolder.Uid ?? Preferences.Get("Uid", null);

            // por si Uid viene correcto desde holder, persistimos
            if (!string.IsNullOrEmpty(Usuario.UID))
            {
                Preferences.Set("Uid", Usuario.UID);
                Preferences.Set("Email", Usuario.Email);
            }
            var token = await FirebaseAuthHolder.GetFreshTokenAsync();
            Debug.WriteLine($"[Login] Firebase OK. uid={FirebaseAuthHolder.Uid} tokenLen={token?.Length ?? 0}");

            // 2) (Opcional) tu lógica de login local/propia
            bool resultat = true;
            if (_loginService is not null)
            {
                resultat = await _loginService.LoginComplet(email, password);
            }

            if (resultat)
            {
                await Shell.Current.GoToAsync("//MainPage");
            }

        }
        catch (Exception ex)
        {
            // Errores de Auth REST (email incorrecto, password, cuenta deshabilitada, etc.)
            Debug.WriteLine($"[Login][ERROR] {ex.GetType().Name}: {ex.Message}");

            // Mapea a un mensaje amable si quieres (puedes inspeccionar ex.Message para códigos de Firebase)
            string msg = _loginService?.GetMissatgeErrorAmable(ex.Message)
                         ?? "No s'ha pogut iniciar sessió. Revisa l'email i la contrasenya.";
            await Toast.Make(msg).Show();
        }
        finally
        {
            LoadingSpinner.IsRunning = false;
            LoadingSpinner.IsVisible = false;
            LoginButton.IsEnabled = true;
            LoginButton.Opacity = 1;
        }
    }

    private async void OnRegisterButtonClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new RegisterPage());
    }



    private async void OnIdiomaSelectorTapped(object sender, EventArgs e)
    {
        string idiomaSeleccionado = await DisplayActionSheet("Selecciona idioma", null, null, "Castellano", "Català");

        if (idiomaSeleccionado == "Castellano")
        {
            CambiarIdioma("es", "flag_es.png", "ES");
        }
        else if (idiomaSeleccionado == "Català")
        {
            CambiarIdioma("ca", "flag_ca.png", "CA");
        }
    }


    private void CambiarIdioma(string idioma, string icono, string texto)
    {
        var culture = new CultureInfo(idioma);

        LocalizationResourceManager.Instance.SetCulture(culture);
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;

        Preferences.Set("idioma", idioma);
        IdiomaIcono.Source = icono;
        IdiomaTexto.Text = texto;

        //xapuza helper idioma
        Idioma.idioma = idioma;
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
    private async void OnContactoTapped(object sender, EventArgs e)
    {
        var uri = new Uri("https://webportfolio-cade5.firebaseapp.com/");
        await Launcher.Default.OpenAsync(uri);
    }
    private async void OnInstagramTapped(object sender, EventArgs e)
    {
        var uri = new Uri("https://www.instagram.com/loracodelvermutenc/");
        await Launcher.Default.OpenAsync(uri);
    }


}
