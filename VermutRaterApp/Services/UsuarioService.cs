using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Maui.Storage;

namespace VermutRaterApp.Services
{
    public static class UsuarioService
    {
        private const string UsuarioKey = "usuario_id";

        public static async Task InicializarUsuarioAsync()
        {
            if (!Preferences.ContainsKey(UsuarioKey))
            {
                var nuevoId = GenerarHashUnico();
                Preferences.Set(UsuarioKey, nuevoId);
            }
        }

        public static string ObtenerUsuarioId()
        {
            return Preferences.Get(UsuarioKey, string.Empty);
        }

        private static string GenerarHashUnico()
        {
            using var sha = SHA256.Create();
            var raw = Guid.NewGuid().ToString() + DateTime.UtcNow.Ticks;
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
            return BitConverter.ToString(bytes).Replace("-", "").ToLower();
        }
    }
}
