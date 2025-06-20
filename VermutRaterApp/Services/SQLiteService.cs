
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SQLite;
using VermutRaterApp.Models;

namespace VermutRaterApp.Services
{
    public static class SQLiteService
    {
        private static SQLiteAsyncConnection database;

        public static async Task InitAsync()
        {
            if (database != null)
                return;

            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "vermutes.db");
            database = new SQLiteAsyncConnection(dbPath);
            await database.CreateTableAsync<VermutLocal>();
        }

        public static async Task<List<VermutLocal>> CargarTodasLasPuntuacionesAsync()
        {
            await InitAsync();
            return await database.Table<VermutLocal>().ToListAsync();
        }

        public static async Task GuardarOActualizarAsync(VermutLocal vermut)
        {
            await InitAsync();
            var existente = await database.Table<VermutLocal>()
                                          .Where(v => v.Nombre == vermut.Nombre)
                                          .FirstOrDefaultAsync();

            if (existente == null)
            {
                await database.InsertAsync(vermut);
            }
            else
            {
                existente.MiPuntuacion = vermut.MiPuntuacion;
                existente.Notas = vermut.Notas;
                existente.YaVotado = vermut.YaVotado;
                await database.UpdateAsync(existente);
            }
        }
    }
}
