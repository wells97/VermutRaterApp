using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

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
    private const double LatVermu = 40.6178404;
    private const double LonVermu = 0.5904031;
    private const double RadiMetres = 50;

    public async Task<bool> LoginComplet(string email, string password)
    {
        if (SessioCaducada())
        {
            if (!await EsDinsDelRadiAsync())
            {
                await App.Current.MainPage.DisplayAlert("Ubicació", "Has d'estar a prop de la vermuteria per iniciar sessió.", "OK");
                return false;
            }

            return await FerLoginAsync(email, password);
        }

        return true;
    }

    public async Task<bool> RegisterAsync(string email, string password)
    {
#if !DEBUG
        if (!await EsDinsDelRadiAsync())
        {
            await App.Current.MainPage.DisplayAlert("Ubicació", "Només et pots registrar a prop de la vermuteria.", "OK");
            return false;
        }
#endif

        try
        {
            var firebase = new FirebaseRestAuth();
            var authData = await firebase.RegisterAsync(email, password);

            if (authData == null || string.IsNullOrEmpty(authData.IdToken))
            {
                await App.Current.MainPage.DisplayAlert("Error", "Error inesperat: resposta buida de Firebase.", "OK");
                return false;
            }

            await SecureStorage.SetAsync("FirebaseToken", authData.IdToken);
            Preferences.Set("SessioInici", DateTime.UtcNow);
            return true;
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("EMAIL_EXISTS"))
                await App.Current.MainPage.DisplayAlert("Error", "Aquest correu ja està registrat.", "OK");
            else
                await App.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");

            return false;
        }

    }

    private async Task<bool> FerLoginAsync(string email, string password)
    {
#if !DEBUG
        if (!await EsDinsDelRadiAsync())
        {
            await App.Current.MainPage.DisplayAlert("Ubicació", "Només et pots registrar a prop de la vermuteria.", "OK");
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
            return true;
        }
        catch (Exception ex)
        {
            await App.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
            return false;
        }

    }

    private bool SessioCaducada()
    {
        if (!Preferences.ContainsKey("SessioInici")) return true;
        var inici = Preferences.Get("SessioInici", DateTime.MinValue);
        return DateTime.UtcNow > inici.AddHours(3);
    }

    private async Task<bool> EsDinsDelRadiAsync()
    {
#if !DEBUG
        try
        {
            var ubicacio = await Geolocation.GetLocationAsync();
            if (ubicacio == null) return false;

            var distancia = Location.CalculateDistance(
                ubicacio.Latitude, ubicacio.Longitude,
                LatVermu, LonVermu,
                DistanceUnits.Kilometers) * 1000;

            return distancia <= RadiMetres;
        }
        catch
        {
            return false;
        }
#endif
        return true;
    }

}
