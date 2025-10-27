using SQLite;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

namespace VermutRaterApp.Models
{
    public class VermutLocal : INotifyPropertyChanged
    {
        // Clave primaria autoincremental (sin UNIQUE en Nombre)
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        // Campos indexados; el UNIQUE compuesto (UserId, Nombre) lo crea LocalStorageService.InitAsync()
        [Indexed] public string UserId { get; set; } = string.Empty;

        private string _nombre = string.Empty;
        [Indexed]
        public string Nombre
        {
            get => _nombre;
            set => SetProperty(ref _nombre, value);
        }

        private int _miPuntuacion;
        public int MiPuntuacion
        {
            get => _miPuntuacion;
            set => SetProperty(ref _miPuntuacion, value);
        }

        private string _notas = string.Empty;
        public string Notas
        {
            get => _notas;
            set => SetProperty(ref _notas, value);
        }

        private string _origen = string.Empty;
        public string Origen
        {
            get => _origen;
            set => SetProperty(ref _origen, value);
        }

        private bool _yaVotado;
        public bool YaVotado
        {
            get => _yaVotado;
            set => SetProperty(ref _yaVotado, value);
        }

        private bool _tastat;
        public bool Tastat
        {
            get => _tastat;
            set => SetProperty(ref _tastat, value);
        }

        // ===== INotifyPropertyChanged =====
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        protected bool SetProperty<T>(ref T backingField, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(backingField, value))
                return false;
            backingField = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
