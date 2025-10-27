using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace VermutRaterApp.Models
{
    public class Vermut : INotifyPropertyChanged
    {
        string _nombre;
        public string Nombre
        {
            get => _nombre;
            set => SetProperty(ref _nombre, value);
        }

        string _origen;
        public string Origen
        {
            get => _origen;
            set => SetProperty(ref _origen, value);
        }

        string _descripcion;
        public string Descripcion
        {
            get => _descripcion;
            set => SetProperty(ref _descripcion, value);
        }

        bool _tastat;
        public bool Tastat
        {
            get => _tastat;
            set => SetProperty(ref _tastat, value);
        }

        bool _yaVotado;
        public bool YaVotado
        {
            get => _yaVotado;
            set => SetProperty(ref _yaVotado, value);
        }

        string _notas;
        public string Notas
        {
            get => _notas;
            set => SetProperty(ref _notas, value);
        }

        // IMPORTANTE: dobles si permites medias/decimales
        int _miPuntuacion;
        public int MiPuntuacion
        {
            get => _miPuntuacion;
            set
            {
                if (SetProperty(ref _miPuntuacion, value))
                {
                    // Si tu XAML usa converters sobre MiPuntuacion, con esto basta
                    OnPropertyChanged(nameof(EstrellasPersonales));
                }
            }
        }

        double _puntuacionGlobal;
        public double PuntuacionGlobal
        {
            get => _puntuacionGlobal;
            set
            {
                if (SetProperty(ref _puntuacionGlobal, value))
                {
                    OnPropertyChanged(nameof(EstrellasGlobales));
                }
            }
        }
        public bool EsFavorito => MiPuntuacion >= 5;
        // Si en algún sitio pintas estrellas como enteros:
        public int EstrellasPersonales => MiPuntuacion;
        public int EstrellasGlobales => (int)System.Math.Round(PuntuacionGlobal);

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value)) return false;
            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        // Útil para forzar repintado tras operaciones en bloque
        public void NotifyStarsChanged()
        {
            OnPropertyChanged(nameof(MiPuntuacion));
            OnPropertyChanged(nameof(PuntuacionGlobal));
            OnPropertyChanged(nameof(EstrellasPersonales));
            OnPropertyChanged(nameof(EstrellasGlobales));
        }
    }
}
