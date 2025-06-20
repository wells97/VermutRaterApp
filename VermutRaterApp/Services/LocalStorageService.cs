using SQLite;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using VermutRaterApp.Models;

namespace VermutRaterApp.Services
{
    public static class LocalStorageService
    {
        private static SQLiteAsyncConnection _database;

        public static async Task InitAsync()
        {
            if (_database != null)
                return;

            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "vermut.db");
            _database = new SQLiteAsyncConnection(dbPath);
            await _database.CreateTableAsync<VermutLocal>();
        }

        public static async Task GuardarVermutLocalAsync(VermutLocal vermut)
        {
            await InitAsync();
            await _database.InsertOrReplaceAsync(vermut);
        }

        public static async Task<VermutLocal> ObtenerVermutLocalAsync(string nombre)
        {
            await InitAsync();
            return await _database
                .Table<VermutLocal>()
                .FirstOrDefaultAsync(v => v.Nombre == nombre);
        }

        public static async Task<List<VermutLocal>> ObtenerTodosAsync()
        {
            await InitAsync();
            return await _database
                .Table<VermutLocal>()
                .ToListAsync();
        }
    }
}
