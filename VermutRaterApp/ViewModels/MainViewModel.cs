// ViewModels/MainViewModel.cs
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Media;
using SQLite;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using VermutRaterApp.Models; // Asegúrate que apunta a tu modelo Vermut
//using VermutRaterApp.Helpers;

namespace VermutRaterApp.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private SQLiteAsyncConnection db;
        public ObservableCollection<Vermut> Vermuts { get; set; } = new();
        private List<Vermut> todosLosVermuts = new();

        private string nuevoNombre;
        public string NuevoNombre { get => nuevoNombre; set { nuevoNombre = value; OnPropertyChanged(); } }

        private string nuevaDescripcion;
        public string NuevaDescripcion { get => nuevaDescripcion; set { nuevaDescripcion = value; OnPropertyChanged(); } }

        private string nuevasNotas;
        public string NuevasNotas { get => nuevasNotas; set { nuevasNotas = value; OnPropertyChanged(); } }

        private int nuevaPuntuacion = 3;
        public int NuevaPuntuacion { get => nuevaPuntuacion; set { nuevaPuntuacion = value; OnPropertyChanged(); Filtrar(); } }

        private int filtro = 1;
        public int Filtro { get => filtro; set { filtro = value; OnPropertyChanged(); Filtrar(); } }

        private bool soloFavoritos = false;
        public bool SoloFavoritos { get => soloFavoritos; set { soloFavoritos = value; OnPropertyChanged(); Filtrar(); } }

        private string imagenSeleccionada;
        public string ImagenSeleccionada { get => imagenSeleccionada; set { imagenSeleccionada = value; OnPropertyChanged(); } }

        private Vermut editando;
        public Vermut Editando { get => editando; set { editando = value; if (value != null) CargarParaEdicion(value); OnPropertyChanged(); } }
        private bool mostrarSorpresa;
        public bool MostrarSorpresa
        {
            get => mostrarSorpresa;
            set
            {
                if (mostrarSorpresa != value)
                {
                    mostrarSorpresa = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand AgregarCommand { get; }
        public ICommand EliminarCommand { get; }
        public ICommand EditarCommand { get; }
        public ICommand SeleccionarImagenCommand { get; }
        public ICommand TomarFotoCommand { get; }
        public ICommand CompartirCommand { get; }
        public ICommand FavoritoCommand { get; }
        public Command MostrarVermutAleatorioCommand { get; }

        public MainViewModel()
        {
            AgregarCommand = new Command(async () => await Guardar());
            EliminarCommand = new Command<Vermut>(async (v) => await Eliminar(v));
            EditarCommand = new Command<Vermut>((v) => Editando = v);
            SeleccionarImagenCommand = new Command(async () => await SeleccionarImagen());
            TomarFotoCommand = new Command(async () => await TomarFoto());
            CompartirCommand = new Command<Vermut>(async (v) => await Compartir(v));
            FavoritoCommand = new Command<Vermut>(async (v) => await ToggleFavorito(v));
            MostrarVermutAleatorioCommand = new Command(MostrarVermutAleatorio);

            InitDb();
        }

        private async void InitDb()
        {
            string dbPath = Path.Combine(FileSystem.AppDataDirectory, "vermut.db");
            db = new SQLiteAsyncConnection(dbPath);
            await db.CreateTableAsync<Vermut>();
            await Cargar();
        }

        private async Task Cargar()
        {
            todosLosVermuts = await db.Table<Vermut>().ToListAsync();
            Filtrar();
        }

        private void Filtrar()
        {
            var filtrados = todosLosVermuts
                .Where(v => v.Puntuacion >= Filtro)
                .Where(v => !SoloFavoritos || v.EsFavorito)
                .ToList();

            Vermuts.Clear();
            foreach (var v in filtrados)
                Vermuts.Add(v);
        }

        private async Task Guardar()
        {
            if (string.IsNullOrWhiteSpace(NuevoNombre))
            {
                await App.Current.MainPage.DisplayAlert("Error", "Nombre requerido", "OK");
                return;
            }

            if (Editando != null)
            {
                Editando.Nombre = NuevoNombre;
                Editando.Descripcion = NuevaDescripcion;
                Editando.Puntuacion = NuevaPuntuacion;
                Editando.ImagenPath = ImagenSeleccionada;
                Editando.Notas = NuevasNotas;
                await db.UpdateAsync(Editando);
            }
            else
            {
                var nuevo = new Vermut
                {
                    Nombre = NuevoNombre,
                    Descripcion = NuevaDescripcion,
                    Puntuacion = NuevaPuntuacion,
                    ImagenPath = ImagenSeleccionada,
                    EsFavorito = false,
                    Notas = NuevasNotas
                };
                await db.InsertAsync(nuevo);
            }

            LimpiarFormulario();
            await Cargar();
        }

        private void LimpiarFormulario()
        {
            Editando = null;
            NuevoNombre = string.Empty;
            NuevaDescripcion = string.Empty;
            NuevasNotas = string.Empty;
            NuevaPuntuacion = 3;
            ImagenSeleccionada = null;
        }

        private void CargarParaEdicion(Vermut v)
        {
            NuevoNombre = v.Nombre;
            NuevaDescripcion = v.Descripcion;
            NuevaPuntuacion = v.Puntuacion;
            ImagenSeleccionada = v.ImagenPath;
            NuevasNotas = v.Notas;
        }

        private async Task Eliminar(Vermut v)
        {
            bool confirm = await App.Current.MainPage.DisplayAlert("Confirmar", $"¿Eliminar '{v.Nombre}'?", "Sí", "No");
            if (!confirm) return;

            await db.DeleteAsync(v);
            todosLosVermuts.Remove(v);
            Filtrar();

            // Reproducir sonido
            //AudioPlayer.PlaySound("papelera.mp3");

            // Vibrar y animar (si tienes una referencia visual)
            try { Vibration.Default.Vibrate(); } catch { }
        }

        private async Task SeleccionarImagen()
        {
            try
            {
                var result = await MediaPicker.PickPhotoAsync();
                if (result != null) ImagenSeleccionada = result.FullPath;
            }
            catch { }
        }

        private async Task TomarFoto()
        {
            try
            {
                var foto = await MediaPicker.CapturePhotoAsync();
                if (foto != null)
                {
                    var path = Path.Combine(FileSystem.AppDataDirectory, foto.FileName);
                    using var stream = await foto.OpenReadAsync();
                    using var newStream = File.OpenWrite(path);
                    await stream.CopyToAsync(newStream);
                    ImagenSeleccionada = path;
                }
            }
            catch { }
        }

        private async Task Compartir(Vermut v)
        {
            try
            {
                if (!string.IsNullOrEmpty(v.ImagenPath) && File.Exists(v.ImagenPath))
                {
                    await Share.RequestAsync(new ShareFileRequest
                    {
                        Title = "Imagen del vermut",
                        File = new ShareFile(v.ImagenPath)
                    });
                }

                await Clipboard.Default.SetTextAsync($"Vermut: {v.Nombre}\nPuntuación: {v.Puntuacion}/5\nDescripción: {v.Descripcion}");
                await App.Current.MainPage.DisplayAlert("Texto copiado", "La descripción se copió al portapapeles.", "OK");
            }
            catch
            {
                await App.Current.MainPage.DisplayAlert("Error", "No se pudo compartir.", "OK");
            }
        }

        private async Task ToggleFavorito(Vermut v)
        {
            v.EsFavorito = !v.EsFavorito;
            await db.UpdateAsync(v);
            await Cargar();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    

    public async void MostrarVermutAleatorio()
        {

            if (Vermuts.Count == 0)
            {
                await Shell.Current.DisplayAlert("Oops", "No tienes vermuts aún", "Vale");
                return;
            }

            MostrarSorpresa = true;
  
        var random = new Random();
            int index = random.Next(Vermuts.Count);
            var elegido = Vermuts[index];

            // 🎉 Aquí puedes añadir vibración o sonido si quieres
            await Shell.Current.DisplayAlert("¡Sorpresa vermutera!", $"Te sugerimos:\n\n{elegido.Nombre}\n⭐ {elegido.Puntuacion}/5", "¡A catar!");
        }
    } 
}