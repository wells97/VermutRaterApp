using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;

namespace VermutRaterApp.Models
{
    public class Vermut
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public int Puntuacion { get; set; } // 1 a 5
        public string ImagenPath { get; set; }
        public bool EsFavorito { get; set; }
        public string Notas { get; set; } // Notas personales
    }
}
