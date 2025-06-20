using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace VermutRaterApp.Models
{
    public class VermutLocal : INotifyPropertyChanged
    {
        private string _nombre = "";
        private int _miPuntuacion;
        private string _notas = "";
        private bool _yaVotado;


        public string Nombre
        {
            get => _nombre;
            set => SetProperty(ref _nombre, value);
        }


        public int MiPuntuacion
        {
            get => _miPuntuacion;
            set => SetProperty(ref _miPuntuacion, value);
        }

        public string Notas
        {
            get => _notas;
            set => SetProperty(ref _notas, value);
        }

        public bool YaVotado
        {
            get => _yaVotado;
            set => SetProperty(ref _yaVotado, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

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
