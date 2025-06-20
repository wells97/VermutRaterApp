using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using VermutRaterApp.Models;
using VermutRaterApp.Services;
using VermutRaterApp.Views;

namespace VermutRaterApp.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<Vermut> Vermuts { get; set; } = new();

        private bool _mostrarSorpresa;
        public bool MostrarSorpresa
        {
            get => _mostrarSorpresa;
            set => SetProperty(ref _mostrarSorpresa, value);
        }

        private Vermut? _vermutSorpresa;
        public Vermut? VermutSorpresa
        {
            get => _vermutSorpresa;
            set => SetProperty(ref _vermutSorpresa, value);
        }

        private int _porcentajeCarga;
        public int PorcentajeCarga
        {
            get => _porcentajeCarga;
            set => SetProperty(ref _porcentajeCarga, value);
        }

        public ICommand IrADetallesCommand { get; }

        public MainViewModel()
        {
            IrADetallesCommand = new Command<Vermut>(async (vermut) =>
            {
                if (vermut == null)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ El vermut recibido era null");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"➡️ Navegando a detalles de: {vermut.Nom}");

                await Shell.Current.GoToAsync(nameof(DetallesPage), true, new Dictionary<string, object>
                {
                    { "vermut", vermut }
                });
            });
        }

        public async Task CargarVermutsAsync()
        {
            Vermuts.Clear();

            var lista = await FirebaseService.CargarVermutsDesdeFirebaseAsync(progress =>
            {
                PorcentajeCarga = progress;
            });

            System.Diagnostics.Debug.WriteLine($"📊 Se recibieron {lista.Count} vermuts");

            foreach (var vermut in lista)
            {
                // Cargar puntuación global
                vermut.PuntuacionGlobal = await FirebaseService.ObtenerPuntuacionGlobalDesdeVotosPorUsuarioAsync(vermut.Nombre);

                // Cargar datos locales
                var local = await LocalStorageService.ObtenerVermutLocalAsync(vermut.Nombre);
                if (local != null)
                {
                    vermut.MiPuntuacion = local.MiPuntuacion;
                    vermut.Notas = local.Notas;
                    vermut.YaVotado = local.YaVotado;
                }

                Vermuts.Add(vermut);
            }
        }

        public void MostrarVermutAleatorio()
        {
            if (Vermuts.Count == 0)
                return;

            var random = new Random();
            int index = random.Next(Vermuts.Count);
            VermutSorpresa = Vermuts[index];
            MostrarSorpresa = true;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(name);
            return true;
        }
    }
}
