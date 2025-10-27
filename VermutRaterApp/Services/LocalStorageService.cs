using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SQLite;
using VermutRaterApp.Models;
using Microsoft.Maui.Storage;

namespace VermutRaterApp.Services
{
    public static class LocalStorageService
    {
        private static SQLiteAsyncConnection _database;

        public static async Task InitAsync()
        {
            if (_database != null) return;

            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "vermut2.db");
            _database = new SQLiteAsyncConnection(dbPath);

            // ¿Existe la tabla?
            var exists = await _database.ExecuteScalarAsync<long>(
                "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='VermutLocal';");

            if (exists == 0)
            {
                // Crear con esquema correcto (Id PK autoincremental)
                await _database.ExecuteAsync(@"
CREATE TABLE IF NOT EXISTS VermutLocal (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId TEXT,
    Nombre TEXT,
    MiPuntuacion INTEGER,
    Notas TEXT,
    Origen TEXT,
    YaVotado INTEGER,
    Tastat INTEGER
);");
            }
            else
            {
                // Inspección del esquema actual
                var cols = await _database.QueryAsync<TableCol>("PRAGMA table_info('VermutLocal');");
                bool hasIdPk = cols.Exists(c => c.name == "Id" && c.pk == 1);
                bool nombreEsPk = cols.Exists(c => c.name == "Nombre" && c.pk == 1);

                bool uniqueSoloNombre = false;
                var indexList = await _database.QueryAsync<IndexInfo>(
                    "SELECT name, [unique] as IsUnique FROM pragma_index_list('VermutLocal');");
                foreach (var idx in indexList)
                {
                    var icols = await _database.QueryAsync<IndexCol>(
                        $"SELECT name FROM pragma_index_info('{idx.name}');");
                    if (idx.IsUnique == 1 && icols.Count == 1 && icols[0].name == "Nombre")
                    {
                        uniqueSoloNombre = true;
                        break;
                    }
                }

                bool requiereRebuild = (!hasIdPk) || nombreEsPk || uniqueSoloNombre;

                if (requiereRebuild)
                {
                    // Recrear tabla sin UNIQUE/PK sobre Nombre
                    await _database.ExecuteAsync(@"
PRAGMA foreign_keys=off;

CREATE TABLE IF NOT EXISTS VermutLocal_new (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId TEXT,
    Nombre TEXT,
    MiPuntuacion INTEGER,
    Notas TEXT,
    Origen TEXT,
    YaVotado INTEGER,
    Tastat INTEGER
);

INSERT OR IGNORE INTO VermutLocal_new (UserId, Nombre, MiPuntuacion, Notas, Origen, YaVotado, Tastat)
SELECT IFNULL(UserId,''), Nombre, MiPuntuacion, Notas, Origen, YaVotado, Tastat
FROM VermutLocal;

DROP TABLE VermutLocal;
ALTER TABLE VermutLocal_new RENAME TO VermutLocal;

PRAGMA foreign_keys=on;");
                }
                else
                {
                    // Añade columnas que falten (no-PK)
                    try { await _database.ExecuteAsync("ALTER TABLE VermutLocal ADD COLUMN UserId TEXT"); } catch { }
                    try { await _database.ExecuteAsync("ALTER TABLE VermutLocal ADD COLUMN Nombre TEXT"); } catch { }
                    try { await _database.ExecuteAsync("ALTER TABLE VermutLocal ADD COLUMN MiPuntuacion INTEGER"); } catch { }
                    try { await _database.ExecuteAsync("ALTER TABLE VermutLocal ADD COLUMN Notas TEXT"); } catch { }
                    try { await _database.ExecuteAsync("ALTER TABLE VermutLocal ADD COLUMN Origen TEXT"); } catch { }
                    try { await _database.ExecuteAsync("ALTER TABLE VermutLocal ADD COLUMN YaVotado INTEGER"); } catch { }
                    try { await _database.ExecuteAsync("ALTER TABLE VermutLocal ADD COLUMN Tastat INTEGER"); } catch { }
                }
            }

            // Normaliza UserId nulo
            await _database.ExecuteAsync("UPDATE VermutLocal SET UserId = '' WHERE UserId IS NULL");

            // ÚNICO correcto por (UserId, Nombre)
            await _database.ExecuteAsync(
                "CREATE UNIQUE INDEX IF NOT EXISTS UX_VermutLocal_User_Nombre ON VermutLocal(UserId, Nombre)");
        }

        private static string Nz(string s) => s ?? string.Empty;

        public static async Task GuardarVermutLocalAsync(string userId, VermutLocal vermut)
        {
            await InitAsync();

            var uid = Nz(userId);
            var nombre = Nz(vermut.Nombre);

            var existente = await _database.QueryAsync<VermutLocal>(
                "SELECT * FROM VermutLocal WHERE UserId = ? AND Nombre = ? LIMIT 1",
                uid, nombre);

            if (existente.Count == 0)
            {
                vermut.UserId = uid;
                vermut.Nombre = nombre;
                await _database.InsertAsync(vermut);
            }
            else
            {
                var e = existente[0];
                e.MiPuntuacion = vermut.MiPuntuacion;
                e.Notas = vermut.Notas;
                e.Origen = vermut.Origen;
                e.YaVotado = vermut.YaVotado;
                e.Tastat = vermut.Tastat;
                await _database.UpdateAsync(e);
            }
        }

        public static async Task<VermutLocal> ObtenerVermutLocalAsync(string userId, string vermutName)
        {
            await InitAsync();

            var uid = Nz(userId);
            var nombre = Nz(vermutName);

            var rows = await _database.QueryAsync<VermutLocal>(
                "SELECT * FROM VermutLocal WHERE UserId = ? AND Nombre = ? LIMIT 1",
                uid, nombre);

            return rows.Count > 0 ? rows[0] : null;
        }

        public static async Task<List<VermutLocal>> ObtenerTodosAsync(string userId)
        {
            await InitAsync();

            var uid = Nz(userId);

            return await _database.QueryAsync<VermutLocal>(
                "SELECT * FROM VermutLocal WHERE UserId = ?",
                uid);
        }

        private class IndexInfo { public string name { get; set; } public int IsUnique { get; set; } }
        private class IndexCol { public string name { get; set; } }
        private class TableCol { public string name { get; set; } public int pk { get; set; } }
    }
}
