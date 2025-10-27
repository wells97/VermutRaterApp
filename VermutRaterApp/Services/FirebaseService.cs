using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using Firebase.Database;
using Firebase.Database.Query;
using VermutRaterApp.Models;
using Microsoft.Maui; // para Application.Current

namespace VermutRaterApp.Services
{
    public static class FirebaseService
    {
        // ========= Config =========
        private static readonly string FirebaseUrl =
            "https://vermutraterapp-default-rtdb.europe-west1.firebasedatabase.app/"; // termina en '/'

        private static FirebaseClient firebase = new FirebaseClient(
            FirebaseUrl,
            new FirebaseOptions
            {
                AuthTokenAsyncFactory = () => FirebaseAuthHolder.GetFreshTokenAsync()
            });

        private static readonly HttpClient _http = new HttpClient(); // para REST .json (solo stats en guardado)

        // ========= Helpers REST RTDB (solo para actualizar stats al votar) =========
        private static async Task RtdbPatchAsync(string pathJson, object body, CancellationToken ct = default)
        {
            var json = JsonSerializer.Serialize(body);
            var token = await FirebaseAuthHolder.GetFreshTokenAsync();
            var url = $"{FirebaseUrl}{pathJson}?auth={token}";

            using var req = new HttpRequestMessage(HttpMethod.Patch, url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            using var res = await _http.SendAsync(req, ct);
            res.EnsureSuccessStatusCode();
        }

        private static async Task<T?> RtdbGetAsync<T>(string pathJson, CancellationToken ct = default)
        {
            var token = await FirebaseAuthHolder.GetFreshTokenAsync();
            var url = $"{FirebaseUrl}{pathJson}?auth={token}";

            using var res = await _http.GetAsync(url, ct);
            res.EnsureSuccessStatusCode();
            var s = await res.Content.ReadAsStringAsync(ct);
            if (string.IsNullOrWhiteSpace(s) || s == "null") return default;
            return JsonSerializer.Deserialize<T>(s);
        }

        // ========= Catálogo =========
        public static async Task<List<Vermut>> CargarVermutsDesdeFirebaseAsync(Action<int>? onProgressChanged = null)
        {
            Debug.WriteLine("[FirebaseService] Descargando catálogo…");
            try
            {
                var vermutList = await firebase.Child("vermut_list").OnceAsync<Vermut>();

                Debug.WriteLine($"[FirebaseService] Catálogo recibido: {vermutList.Count} items");
                var resultado = new List<Vermut>();
                int total = Math.Max(vermutList.Count, 1);
                int count = 0;

                foreach (var item in vermutList)
                {
                    count++;
                    var vermut = item.Object;
                    if (vermut != null)
                    {
                        vermut.Nombre = item.Key ?? vermut.Nombre;
                        resultado.Add(vermut);
                    }
                    onProgressChanged?.Invoke(count * 100 / total);
                }

                return resultado;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FirebaseService][ERROR catálogo] {ex.GetType().Name}: {ex.Message}");
                if (ex.InnerException != null)
                    Debug.WriteLine($"[FirebaseService][ERROR catálogo][Inner] {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
                return new List<Vermut>();
            }
        }

        // ========= Lecturas de votos (fallback lento) =========
        public static async Task<double> ObtenerPuntuacionGlobalDesdeVotosPorUsuarioAsync(string vermutId, CancellationToken ct = default)
        {
            var task = firebase.Child("votosPorUsuario").Child(vermutId).OnceAsync<int>();
            var completed = await Task.WhenAny(task, Task.Delay(8000, ct)); // timeout 8s
            if (completed != task)
            {
                Debug.WriteLine($"[FirebaseService] timeout leyendo votos de {vermutId}; devolviendo 0");
                return 0;
            }

            var votos = await task;
            if (votos == null || votos.Count == 0) return 0;
            return votos.Select(x => x.Object).Average();
        }

        public static async Task<int?> ObtenerVotoDeUsuarioAsync(string vermutId, string usuarioIdHash, CancellationToken ct = default)
        {
            try
            {
                var task = firebase.Child("votosPorUsuario")
                                   .Child(vermutId)
                                   .Child(usuarioIdHash)
                                   .OnceSingleAsync<object>();
                var completed = await Task.WhenAny(task, Task.Delay(5000, ct));
                if (completed != task) return null;

                var val = await task;
                return val == null ? (int?)null : Convert.ToInt32(val);
            }
            catch
            {
                return null;
            }
        }

        // ========= Stats rápidos (/stats/{id}) =========
        public class VermutStats
        {
            public int count { get; set; }
            public double sum { get; set; }
            public double avg { get; set; }
        }

        public static async Task<double?> TryGetMediaRapidaAsync(string vermutId, CancellationToken ct = default)
        {
            try
            {
                var task = firebase.Child("stats").Child(vermutId).OnceSingleAsync<VermutStats>();
                var completed = await Task.WhenAny(task, Task.Delay(3000, ct)); // 3s
                if (completed != task) return null;

                var s = await task;
                return s?.avg;
            }
            catch
            {
                return null;
            }
        }

        // ==== Guardado de voto + actualización atómica de stats (REST) ====
        private static async Task IncrementarStatsAsync(string vermutId, int deltaSum, int deltaCount, CancellationToken ct = default)
        {
            var body = new Dictionary<string, object?>
            {
                ["sum"] = new Dictionary<string, object?>
                {
                    [".sv"] = new Dictionary<string, object?> { ["increment"] = deltaSum }
                },
                ["count"] = new Dictionary<string, object?>
                {
                    [".sv"] = new Dictionary<string, object?> { ["increment"] = deltaCount }
                }
            };

            await RtdbPatchAsync($"stats/{Uri.EscapeDataString(vermutId)}.json", body, ct);
        }

        private static async Task RecalcularAvgAsync(string vermutId, CancellationToken ct = default)
        {
            var stats = await RtdbGetAsync<VermutStats>($"stats/{Uri.EscapeDataString(vermutId)}.json", ct);
            var sum = (int)Math.Round(stats?.sum ?? 0);
            var count = stats?.count ?? 0;
            double avg = count > 0 ? (double)sum / count : 0;

            await RtdbPatchAsync($"stats/{Uri.EscapeDataString(vermutId)}.json", new { avg }, ct);
        }

        public static async Task GuardarPuntuacionUsuarioAsync(VotoVermut votoVermut)
        {
            Debug.WriteLine($"[FirebaseService] Guardando voto {votoVermut.Puntuacion} para {votoVermut.VermutId}…");

            // 1) Voto previo
            int? votoPrevio = null;
            try
            {
                votoPrevio = await ObtenerVotoDeUsuarioAsync(votoVermut.VermutId, Usuario.UID);
            }
            catch { }

            // 2) Escribe nuevo voto
            await firebase
              .Child("votosPorUsuario")
              .Child(votoVermut.VermutId)
              .Child(Usuario.UID)
              .PutAsync(votoVermut.Puntuacion);

            // 3) Aplica deltas
            var deltaSum = votoVermut.Puntuacion - (votoPrevio ?? 0);
            var deltaCount = votoPrevio == null ? 1 : 0;

            await IncrementarStatsAsync(votoVermut.VermutId, deltaSum, deltaCount);
            await RecalcularAvgAsync(votoVermut.VermutId);

            Debug.WriteLine("[FirebaseService] Voto guardado y stats actualizados.");
        }

        // ========= Carga robusta combinando local + stats + voto usuario =========
        public static async Task<List<Vermut>> CargarVermutsConDatosLocalesAsync(Action<int>? onProgressChanged = null)
        {
            Debug.WriteLine("[FirebaseService] Inicio carga combinada…");

            await FirebaseAuthHolder.InitAsync();
            var uid = !string.IsNullOrWhiteSpace(Usuario.UID) ? Usuario.UID : (FirebaseAuthHolder.Uid ?? string.Empty);

            List<Vermut> vermuts = new();
            try
            {
                // 1) Catálogo
                vermuts = await CargarVermutsDesdeFirebaseAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FirebaseService] Error obteniendo catálogo: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert(
                    "Error Firebase",
                    $"Fallo al obtener catálogo:\n{ex.Message}\n\n{ex.StackTrace}",
                    "OK");
                return vermuts;
            }

            // 2) Local cache (scoped por usuario) + defensas contra duplicados por Nombre
            var localesRaw = await LocalStorageService.ObtenerTodosAsync(uid);

            var locales = localesRaw
                .GroupBy(l => l.Nombre, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();

            var localMap = locales
                .GroupBy(l => l.Nombre, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            // 3) Paraleliza con límite de concurrencia
            const int MAX_CONCURRENCY = 8;
            var throttler = new SemaphoreSlim(MAX_CONCURRENCY);
            var cts = new CancellationTokenSource();

            int done = 0;
            int total = Math.Max(vermuts.Count, 1);

            var tasks = vermuts.Select(async v =>
            {
                await throttler.WaitAsync();
                try
                {
                    // Mezcla local (si existe, aplica datos locales)
                    if (!string.IsNullOrWhiteSpace(v?.Nombre) &&
                        localMap.TryGetValue(v.Nombre, out var local))
                    {
                        v.MiPuntuacion = local.MiPuntuacion;
                        v.Notas = local.Notas;
                        v.YaVotado = local.YaVotado;
                        v.Tastat = local.Tastat;
                    }

                    // Media (rápida con timeout; si falla, lenta)
                    double media;
                    var rapida = await TryGetMediaRapidaAsync(v.Nombre, cts.Token);
                    media = rapida.HasValue
                        ? rapida.Value
                        : await ObtenerPuntuacionGlobalDesdeVotosPorUsuarioAsync(v.Nombre, cts.Token);

                    v.PuntuacionGlobal = media;

                    // Voto de usuario (con timeout)
                    var miVoto = await ObtenerVotoDeUsuarioAsync(v.Nombre, uid, cts.Token);
                    if (miVoto.HasValue)
                    {
                        v.MiPuntuacion = miVoto.Value;
                        v.Tastat = true;
                    }

                    // Sembrar en local si no existe (scoped por usuario)
                    if (!string.IsNullOrWhiteSpace(v?.Nombre) && !localMap.ContainsKey(v.Nombre))
                    {
                        await LocalStorageService.GuardarVermutLocalAsync(uid, new VermutLocal
                        {
                            UserId = uid,
                            Nombre = v.Nombre,
                            MiPuntuacion = v.MiPuntuacion,
                            Notas = v.Notas,
                            YaVotado = v.YaVotado,
                            Tastat = v.Tastat
                        });

                        localMap[v.Nombre] = new VermutLocal
                        {
                            UserId = uid,
                            Nombre = v.Nombre,
                            MiPuntuacion = v.MiPuntuacion,
                            Notas = v.Notas,
                            YaVotado = v.YaVotado,
                            Tastat = v.Tastat
                        };
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[FirebaseService] Error procesando {v?.Nombre}: {ex.Message}");
                    await Application.Current.MainPage.DisplayAlert(
                        "Error procesando vermut",
                        $"{v?.Nombre ?? "??"}\n{ex.Message}\n\n{ex.StackTrace}",
                        "OK");
                }
                finally
                {
                    throttler.Release();
                    int p = (Interlocked.Increment(ref done) * 100) / total;
                    onProgressChanged?.Invoke(p);
                }
            });

            await Task.WhenAll(tasks);
            Debug.WriteLine("[FirebaseService] Carga combinada terminada.");
            return vermuts;
        }
        //Whitelist
        public static async Task<List<string>> GetWhitelist(Action<int>? onProgressChanged = null)
        {
            Debug.WriteLine("[FirebaseService] Descargando whitelist…");

            List<string> whitelistEmails;
            try
            {
                var vermutList = await firebase.Child("whitelist").OnceAsync<Vermut>();

                Debug.WriteLine($"[FirebaseService] whitelist recibido: {vermutList.Count} items");
                var resultado = new List<string>();


                return resultado;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FirebaseService][ERROR catálogo] {ex.GetType().Name}: {ex.Message}");
                if (ex.InnerException != null)
                    Debug.WriteLine($"[FirebaseService][ERROR catálogo][Inner] {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
                return new List<string>();
            }
        }

        

    }
}
