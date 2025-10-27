using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Diagnostics;

namespace VermutRaterApp.Services
{
    public static class FirebaseAuthHolder
    {
        private const string ApiKey = "AIzaSyAN9T4POw2_-IfZMvJQrUTuEZmnT0nWV-A";
        private static readonly HttpClient _http = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };

        private static string? _idToken;
        private static string? _refreshToken;
        private static string? _uid;
        private static DateTime _expiresAtUtc = DateTime.MinValue;

        public static string? Uid => _uid;

        public static async Task InitAsync()
        {
            if (string.IsNullOrEmpty(_idToken))
                await SignInAnonymouslyAsync(); // o quita esta línea si solo quieres email/pass
        }

        public static async Task<string> GetFreshTokenAsync()
        {
            if (string.IsNullOrEmpty(_idToken))
                await SignInAnonymouslyAsync();

            if (DateTime.UtcNow >= _expiresAtUtc.AddSeconds(-60))
                await RefreshAsync();

            return _idToken!;
        }

        // ===== Login EMAIL/PASSWORD =====
        public static async Task SignInWithEmailPasswordAsync(string email, string password)
        {
            var url = $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={ApiKey}";
            var payload = JsonSerializer.Serialize(new { email, password, returnSecureToken = true });

            using var req = new HttpRequestMessage(HttpMethod.Post, url)
            { Content = new StringContent(payload, Encoding.UTF8, "application/json") };

            using var res = await _http.SendAsync(req);
            var body = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
                throw new Exception($"Firebase email sign-in failed: {body}");

            using var doc = JsonDocument.Parse(body);
            _idToken = doc.RootElement.GetProperty("idToken").GetString();
            _refreshToken = doc.RootElement.GetProperty("refreshToken").GetString();
            _uid = doc.RootElement.GetProperty("localId").GetString();
            var expiresIn = int.Parse(doc.RootElement.GetProperty("expiresIn").GetString() ?? "3600");
            _expiresAtUtc = DateTime.UtcNow.AddSeconds(expiresIn);

            Debug.WriteLine($"[Auth] Email OK. uid={_uid} exp={_expiresAtUtc:u}");
        }

        // ===== Login ANÓNIMO (por si quieres mantenerlo) =====
        public static async Task SignInAnonymouslyAsync()
        {
            var url = $"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={ApiKey}";
            var payload = JsonSerializer.Serialize(new { returnSecureToken = true });

            using var req = new HttpRequestMessage(HttpMethod.Post, url)
            { Content = new StringContent(payload, Encoding.UTF8, "application/json") };

            using var res = await _http.SendAsync(req);
            var body = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
                throw new Exception($"Firebase anonymous sign-in failed: {body}");

            using var doc = JsonDocument.Parse(body);
            _idToken = doc.RootElement.GetProperty("idToken").GetString();
            _refreshToken = doc.RootElement.GetProperty("refreshToken").GetString();
            _uid = doc.RootElement.GetProperty("localId").GetString();
            var expiresIn = int.Parse(doc.RootElement.GetProperty("expiresIn").GetString() ?? "3600");
            _expiresAtUtc = DateTime.UtcNow.AddSeconds(expiresIn);

            Debug.WriteLine($"[Auth] Anonymous OK. uid={_uid} exp={_expiresAtUtc:u}");
        }

        // ===== Refresh token =====
        private static async Task RefreshAsync()
        {
            if (string.IsNullOrEmpty(_refreshToken))
            {
                await SignInAnonymouslyAsync();
                return;
            }

            var url = $"https://securetoken.googleapis.com/v1/token?key={ApiKey}";
            var form = $"grant_type=refresh_token&refresh_token={_refreshToken}";
            using var req = new HttpRequestMessage(HttpMethod.Post, url)
            { Content = new StringContent(form, Encoding.UTF8, "application/x-www-form-urlencoded") };

            using var res = await _http.SendAsync(req);
            var body = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
            {
                Debug.WriteLine($"[Auth] Refresh failed: {body}");
                await SignInAnonymouslyAsync();
                return;
            }

            using var doc = JsonDocument.Parse(body);
            _idToken = doc.RootElement.GetProperty("id_token").GetString();
            _refreshToken = doc.RootElement.GetProperty("refresh_token").GetString();
            _uid = doc.RootElement.GetProperty("user_id").GetString();
            var expiresIn = int.Parse(doc.RootElement.GetProperty("expires_in").GetString() ?? "3600");
            _expiresAtUtc = DateTime.UtcNow.AddSeconds(expiresIn);

            Debug.WriteLine($"[Auth] Refreshed. uid={_uid} exp={_expiresAtUtc:u}");
        }
    }
}
