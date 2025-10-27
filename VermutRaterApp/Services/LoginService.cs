using CommunityToolkit.Maui.Alerts;
using FirebaseAdmin.Auth;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Storage;
using System.Globalization;
using System.Resources;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using VermutRaterApp.Helpers;
using VermutRaterApp.Models;
using VermutRaterApp.Resources.Strings;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;
using VermutRaterApp.Views;
using static Android.Graphics.ImageDecoder;
using System.Net.Http;             
using System.Linq;                 
using System.Collections.Generic;
using System.ComponentModel.Design;

namespace VermutRaterApp.Services;

public class FirebaseAuthResponse
{
    [JsonPropertyName("idToken")]
    public string IdToken { get; set; }

    [JsonPropertyName("localId")]
    public string LocalId { get; set; }

    [JsonPropertyName("email")]
    public string Email { get; set; }
}

public class FirebaseRestAuth
{
    public const string ApiKey = "AIzaSyAN9T4POw2_-IfZMvJQrUTuEZmnT0nWV-A";

    public async Task<FirebaseAuthResponse> LoginAsync(string email, string password)
    {
        var payload = new
        {
            email,
            password,
            returnSecureToken = true
        };

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var url = $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={ApiKey}";

        var response = await new HttpClient().PostAsync(url, content);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Error d'autenticació: {body}");

        return JsonSerializer.Deserialize<FirebaseAuthResponse>(body);
    }

    public async Task<FirebaseAuthResponse> RegisterAsync(string email, string password)
    {
        var payload = new
        {
            email,
            password,
            returnSecureToken = true
        };

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var url = $"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={ApiKey}";

        var response = await new HttpClient().PostAsync(url, content);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception(body);

        return JsonSerializer.Deserialize<FirebaseAuthResponse>(body);
    }
}

public class LoginService
{
    // 🔧 Cambia esta URL si tu RTDB tiene otro nombre/ubicación.
    private const string whitelist =  "https://vermutraterapp-default-rtdb.firebaseio.com";

    private const double LatVermu = 40.6178404;
    private const double LonVermu = 0.5904031;
#if DEBUG
    private const double RadiMetres = 11150;
#else
    private const double RadiMetres = 50;
#endif

    // cache en memoria + copia en Preferences por si hay fallo de red
    private static List<string> _whitelistCache = new();
    private static DateTime _whitelistLoadedAt = DateTime.MinValue;
    private static readonly TimeSpan WhiteListTtl = TimeSpan.FromMinutes(30);

    private static string NormalizeEmail(string email) =>
        (email ?? string.Empty).Trim().ToLowerInvariant();


    public async Task<bool> LoginComplet(string email, string password)
    {
        string alertText = string.Empty;

        // 🟢 Si el email está en whitelist, saltamos restricción de radio
        bool isWhite = await isWhitelisted(email);

        if (SessioCaducada())
        {
#if !DEBUG
            if (!isWhite && !await EsDinsDelRadiAsync())
            {
                if (Idioma.idioma.Equals("ca")) alertText = "Has d'estar a prop de la vermuteria per iniciar sessió.";
                else alertText = "Debes estar cerca de la vermutería para iniciar sesión.";
                await App.Current.MainPage.DisplayAlert("Error", alertText, "OK");
                return false;
            }
#endif
            return await FerLoginAsync(email, password, isWhite);
        }

        // sesión válida: repuebla Usuario desde Preferences
        if (string.IsNullOrEmpty(Usuario.UID))
        {
            Usuario.UID = Preferences.Get("Uid", null);
            Usuario.Email = Preferences.Get("Email", null);
        }
        return true;
    }

    // ⛔ Nos pediste ignorar RegisterAsync, lo dejo como estaba
    public async Task<bool> RegisterAsync(string email, string password)
    {
        string alertText = string.Empty;
#if !DEBUG
        if (!await EsDinsDelRadiAsync())
        {
            if (Idioma.idioma.Equals("ca")) alertText = "Només et pots registrar a prop de la vermuteria.";
            else alertText = "Solo puedes registrarte cerca de la vermutería.";
            await App.Current.MainPage.DisplayAlert("Error", alertText, "OK");
            return false;
        }
#endif

        try
        {
            var firebase = new FirebaseRestAuth();
            var authData = await firebase.RegisterAsync(email, password);

            if (authData == null || string.IsNullOrEmpty(authData.IdToken))
                return false;

            await SecureStorage.SetAsync("FirebaseToken", authData.IdToken);
            Preferences.Set("SessioInici", DateTime.UtcNow);
            return true;
        }
        catch (Exception ex)
        {
            var missatge = GetMissatgeErrorAmable(ex.Message);
            await Toast.Make(missatge).Show();
            return false;
        }
    }

    private async Task<bool> FerLoginAsync(string email, string password, bool isWhite = false)
    {
        string alertText = string.Empty;

#if !DEBUG
        // Segunda barrera por si se llama FerLoginAsync directamente
        if (!isWhite && !await EsDinsDelRadiAsync())
        {
            if (Idioma.idioma.Equals("ca")) alertText = "Has d'estar a prop de la vermuteria per iniciar sessió.";
            else alertText = "Debes estar cerca de la vermutería para iniciar sesión.";
            await App.Current.MainPage.DisplayAlert("Error", alertText, "OK");
            return false;
        }
#endif

        try
        {
            var firebase = new FirebaseRestAuth();
            var authData = await firebase.LoginAsync(email, password);

            if (authData == null || string.IsNullOrEmpty(authData.IdToken))
            {
                await App.Current.MainPage.DisplayAlert("Error", "Error inesperat: resposta buida de Firebase.", "OK");
                return false;
            }

            await SecureStorage.SetAsync("FirebaseToken", authData.IdToken);
            Preferences.Set("SessioInici", DateTime.UtcNow);
            Usuario.Email = email;
            Usuario.UID = authData.LocalId;

            // guarda para siguientes arranques/sesiones no caducadas
            Preferences.Set("Uid", Usuario.UID);
            Preferences.Set("Email", Usuario.Email);

            await SecureStorage.SetAsync("FirebaseToken", authData.IdToken);
            Preferences.Set("SessioInici", DateTime.UtcNow);

            return true;
        }
        catch (Exception ex)
        {
            var missatge = GetMissatgeErrorAmable(ex.Message);
            await Toast.Make(missatge).Show();
            return false;
        }
    }

    public string GetMissatgeErrorAmable(string rawMessage)
    {
        System.Diagnostics.Debug.WriteLine($"❌Missatge retornat de error al fer login: {rawMessage}");

        string idioma = Idioma.idioma;

        if (rawMessage.Contains("INVALID_LOGIN_CREDENTIALS") || rawMessage.Contains("INVALID_PASSWORD"))
            return idioma == "ca" ? "Contrasenya incorrecta." : "Contraseña incorrecta.";

        if (rawMessage.Contains("EMAIL_NOT_FOUND"))
            return idioma == "ca" ? "Aquest usuari no existeix." : "Este usuario no existe.";

        if (rawMessage.Contains("INVALID_EMAIL"))
            return idioma == "ca" ? "El correu electrònic no és vàlid." : "El correo electrónico no es válido.";

        if (rawMessage.Contains("WEAK_PASSWORD"))
            return idioma == "ca" ? "La contrasenya ha de tenir almenys 6 caràcters."
                                  : "La contraseña debe tener al menos 6 caracteres.";

        if (rawMessage.Contains("EMPTY_FIELDS"))
            return idioma == "ca" ? "Has d'omplir tots els camps."
                                  : "Tienes que rellenar todos los campos.";

        if (rawMessage.Contains("EMAIL_EXISTS"))
            return idioma == "ca" ? "Aquest email ja està registrat."
                                  : "Este correo ya está registrado.";

        return idioma == "ca" ? "Error inesperat en iniciar sessió."
                              : "Error inesperado al iniciar sesión.";
    }

    private bool SessioCaducada()
    {
        if (!Preferences.ContainsKey("SessioInici")) return true;
        var inici = Preferences.Get("SessioInici", DateTime.MinValue);
        return DateTime.UtcNow > inici.AddHours(3);
    }

    private async Task<bool> EsDinsDelRadiAsync()
    {
#if DEBUG
        // En debug, si quieres probar, fuerza el mismo flujo que Release:
        // return await EsDinsDelRadiReleaseAsync();
        return true;
#else
        return await EsDinsDelRadiReleaseAsync();
#endif
    }

    private async Task<bool> EsDinsDelRadiReleaseAsync()
    {
        // 1) Permisos
        var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
        if (status != PermissionStatus.Granted)
        {
            status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                await App.Current.MainPage.DisplayAlert(
                    Idioma.idioma.Equals("ca") ? "Permís de localització" : "Permiso de ubicación",
                    Idioma.idioma.Equals("ca")
                        ? "Necessitem la teva ubicació per verificar que estàs a prop de la vermuteria."
                        : "Necesitamos tu ubicación para verificar que estás cerca de la vermutería.",
                    "OK");

                AppInfo.ShowSettingsUI();
                return false;
            }
        }

        try
        {
            var req = new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(10));
            var ubicacio = await Geolocation.GetLocationAsync(req);

            if (ubicacio == null)
            {
                ubicacio = await Geolocation.GetLastKnownLocationAsync();
                if (ubicacio == null) return false;
            }

            var distancia = Location.CalculateDistance(
                ubicacio.Latitude, ubicacio.Longitude,
                LatVermu, LonVermu,
                DistanceUnits.Kilometers) * 1000.0;

            return distancia <= RadiMetres;
        }
        catch (FeatureNotEnabledException)
        {
            var abrir = await App.Current.MainPage.DisplayAlert(
                Idioma.idioma.Equals("ca") ? "Ubicació desactivada" : "Ubicación desactivada",
                Idioma.idioma.Equals("ca")
                    ? "Activa la localització del dispositiu per continuar."
                    : "Activa la ubicación del dispositivo para continuar.",
                Idioma.idioma.Equals("ca") ? "Obrir ajustos" : "Abrir ajustes",
                Idioma.idioma.Equals("ca") ? "Cancel·lar" : "Cancelar");

            if (abrir)
            {
                try
                {
#if ANDROID
                    await Launcher.OpenAsync(new Uri("android.settings.LOCATION_SOURCE_SETTINGS"));
#else
                    AppInfo.ShowSettingsUI();
#endif
                }
                catch { }
            }
            return false;
        }
        catch (PermissionException)
        {
            await App.Current.MainPage.DisplayAlert(
                Idioma.idioma.Equals("ca") ? "Permís de localització" : "Permiso de ubicación",
                Idioma.idioma.Equals("ca")
                    ? "Sense permís de localització no podem verificar la distància."
                    : "Sin permiso de ubicación no podemos verificar la distancia.",
                "OK");

            AppInfo.ShowSettingsUI();
            return false;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> isWhitelisted(string login) {
        var whitelist = await FirebaseService.GetWhitelist();

        if (whitelist.Contains(login)) return true;

        return false;
       
    
    }
}
