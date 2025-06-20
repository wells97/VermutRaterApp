using Firebase.Database;
using Firebase.Database.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VermutRaterApp.Models;

namespace VermutRaterApp.Services
{
    public static class FirebaseService
    {
        private static readonly string FirebaseUrl = "https://vermutraterapp-default-rtdb.europe-west1.firebasedatabase.app/";
        private static readonly FirebaseClient firebase = new FirebaseClient(FirebaseUrl);

        public static async Task<List<Vermut>> CargarVermutsDesdeFirebaseAsync(Action<int>? onProgressChanged = null)
        {
            var vermutList = await firebase.Child("vermut_list").OnceAsync<Vermut>();

            System.Diagnostics.Debug.WriteLine($"📦 Firebase devolvió: {vermutList.Count} elementos");

            List<Vermut> resultado = new();
            int total = vermutList.Count;
            int count = 0;

            foreach (var item in vermutList)
            {
                count++;
                var vermut = item.Object;

                if (vermut != null)
                {
                    vermut.Nombre = item.Key;

                    // Logging detallado por seguridad
                    System.Diagnostics.Debug.WriteLine($"✅ {vermut.Nombre} | Desc: {vermut.Descripcion} | Caract: {vermut.Caracteristicas?.Count} | Puntuación: {vermut.PuntuacionGlobal}");

                    resultado.Add(vermut);
                }

                onProgressChanged?.Invoke((count * 100) / total);
            }

            return resultado;
        }



        public static async Task<double> ObtenerPuntuacionGlobalDesdeVotosPorUsuarioAsync(string vermutId)
        {
            var votos = await firebase
                .Child("votosPorUsuario")
                .Child(vermutId)
                .OnceAsync<int>();

            if (votos == null || votos.Count == 0)
                return 0;

            return votos.Select(x => x.Object).Average();
        }


        // 🔐 Guarda la puntuación de un usuario concreto para un vermut
        public static async Task GuardarPuntuacionUsuarioAsync(string vermutId, string userId, int puntuacion)
        {
            await firebase
                .Child("votosPorUsuario")
                .Child(vermutId)
                .Child(userId)
                .PutAsync(puntuacion);
        }
        public static async Task<List<Vermut>> CargarVermutsConDatosLocalesAsync(Action<int>? onProgressChanged = null)
        {
            // 1. Cargar vermuts desde Firebase
            var vermuts = await FirebaseService.CargarVermutsDesdeFirebaseAsync(onProgressChanged);

            // 2. Cargar datos locales
            var locales = await LocalStorageService.ObtenerTodosAsync();

            // 3. Recorremos cada vermut y le añadimos sus datos locales y puntuación global
            foreach (var vermut in vermuts)
            {
                // a) Puntuación global
                vermut.PuntuacionGlobal = await FirebaseService.ObtenerPuntuacionGlobalDesdeVotosPorUsuarioAsync(vermut.Nombre);

                // b) Buscar datos locales si existen
                var local = locales.FirstOrDefault(v => v.Nombre == vermut.Nombre);
                if (local != null)
                {
                    vermut.MiPuntuacion = local.MiPuntuacion;
                    vermut.Notas = local.Notas;
                    vermut.YaVotado = local.YaVotado;
                }
            }

            return vermuts;
        }
    }
}
