
namespace VermutRaterApp.Models
{
    public class VotoVermut
    {
        public string VermutId { get; set; }
        public string UsuarioId { get; set; }
        public int Puntuacion { get; set; }

        public VotoVermut(string _vemutId, int _puntuacion) {

            VermutId = _vemutId;
            UsuarioId = Usuario.UID;
            Puntuacion = _puntuacion;
        }
    }
}
