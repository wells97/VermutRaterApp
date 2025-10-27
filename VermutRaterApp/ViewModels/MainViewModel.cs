using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using VermutRaterApp.Models;
using VermutRaterApp.Services;
using Microsoft.Maui; // para Application.Current

namespace VermutRaterApp.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        // ===== Estado principal =====
        public ObservableCollection<Vermut> Vermuts { get; set; } = new();
        private List<Vermut> _totsElsVermuts = new();  // base completa (no ObservableCollection)

        public Vermut? VermutSorpresa { get; private set; }

        // Control de carga / filtro
        private bool _isDataReady;                  // <- gate: no filtrar hasta tener datos
        private System.Timers.Timer _debounceTimer; // para SearchText

        public int LoadProgress { get; private set; }
        public bool IsLoading { get; private set; }

        // ===== Filtros =====
        public List<string> FiltreOpcions { get; } = new() { "Tots", "Tastats", "No tastats" };

        private string _filtreTastats = "Tots";
        public string FiltreTastats
        {
            get => _filtreTastats;
            set
            {
                if (_filtreTastats != value)
                {
                    _filtreTastats = value;
                    OnPropertyChanged();
                    if (_isDataReady) AplicarFiltre();   // gate
                }
            }
        }

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged();
                    IniciarTemporitzadorFiltratge();      // hará gate internamente
                }
            }
        }

        private bool _isFiltering;
        public bool IsFiltering
        {
            get => _isFiltering;
            set { if (_isFiltering != value) { _isFiltering = value; OnPropertyChanged(); } }
        }

        private string _origenSeleccionat = "Tots";
        public string OrigenSeleccionat
        {
            get => _origenSeleccionat;
            set
            {
                if (_origenSeleccionat != value)
                {
                    _origenSeleccionat = value;
                    OnPropertyChanged();
                    if (_isDataReady) AplicarFiltre();   // gate
                }
            }
        }

        public List<string> OrigenesDisponibles { get; private set; } = new() { "Tots" };

        // ===== Carga de datos =====
        public async Task CargarVermutsAsync(CancellationToken ct = default)
        {
            _isDataReady = false;             // desactiva triggers durante la carga
            IsLoading = true;
            OnPropertyChanged(nameof(IsLoading));

            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                // 1) Asegura sesión y UID
                await FirebaseAuthHolder.InitAsync();
                var uid = FirebaseAuthHolder.Uid ?? string.Empty;

                // 2) Carga de Firebase (lista base sin mezclar)
                var lista = await FirebaseService.CargarVermutsConDatosLocalesAsync(p =>
                {
                    LoadProgress = p;
                    OnPropertyChanged(nameof(LoadProgress));
                });

                _totsElsVermuts = (lista ?? Enumerable.Empty<Vermut>()).ToList();

                // 3) Carga local del usuario y mezcla por nombre (clave lógica: UserId + Nombre)
                await LocalStorageService.InitAsync();
                var locals = await LocalStorageService.ObtenerTodosAsync(uid);
                var localsPorNombre = locals?
                    .GroupBy(l => l.Nombre, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase)
                    ?? new Dictionary<string, VermutLocal>(StringComparer.OrdinalIgnoreCase);

                foreach (var v in _totsElsVermuts)
                {
                    if (v is null || string.IsNullOrWhiteSpace(v.Nombre)) continue;

                    if (localsPorNombre.TryGetValue(v.Nombre, out var loc))
                    {
                        // Sobrescribe campos personales desde local
                        v.MiPuntuacion = loc.MiPuntuacion;
                        v.Tastat = loc.Tastat;
                        // Si tu modelo Vermut tiene Nota/Notas personales visibles, sincronízalas aquí
                        // v.NotasPersonales = loc.Notas; // (si existe)
                    }
                }

                // 4) Rellena orígenes una vez con los datos cargados
                OrigenesDisponibles = new List<string> { "Tots" };
                OrigenesDisponibles.AddRange(
                    _totsElsVermuts
                        .Where(v => !string.IsNullOrWhiteSpace(v.Origen))
                        .Select(v => v.Origen)
                        .Distinct()
                        .OrderBy(o => o)
                );
                OnPropertyChanged(nameof(OrigenesDisponibles));

                _isDataReady = true;          // ✅ ya se pueden aplicar filtros
                AplicarFiltre();              // primera proyección segura
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainViewModel] Error cargando vermuts: {ex.GetType().Name} - {ex.Message}");

                // (Opcional) mensaje en UI
                try
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Error cargando vermuts",
                        $"{ex.GetType().Name}\n{ex.Message}",
                        "OK");
                }
                catch { /* por si no hay MainPage aún */ }

                _totsElsVermuts = new List<Vermut>();
                _isDataReady = true;          // evita quedarse bloqueado
                AplicarFiltre();
            }
            finally
            {
                sw.Stop();
                System.Diagnostics.Debug.WriteLine($"⏱ Carga completa en {sw.ElapsedMilliseconds} ms");
                IsLoading = false;
                OnPropertyChanged(nameof(IsLoading));
            }
        }

        // ===== Filtro (con defensas) =====
        private void AplicarFiltre()
        {
            if (!_isDataReady) return;

            var baseList = _totsElsVermuts ?? new List<Vermut>();
            IEnumerable<Vermut> q = baseList;

            // Texto
            var t = _searchText?.Trim();
            if (!string.IsNullOrEmpty(t))
            {
                var tl = t.ToLowerInvariant();
                q = q.Where(v =>
                    (!string.IsNullOrEmpty(v.Nombre) && v.Nombre.ToLowerInvariant().Contains(tl)) ||
                    (!string.IsNullOrEmpty(v.Origen) && v.Origen.ToLowerInvariant().Contains(tl)));
            }

            // Origen
            if (!string.IsNullOrEmpty(OrigenSeleccionat) && OrigenSeleccionat != "Tots")
                q = q.Where(v => v.Origen == OrigenSeleccionat);

            // Tastats / No tastats
            switch (FiltreTastats)
            {
                case "Tastats": q = q.Where(v => v.Tastat); break;
                case "No tastats": q = q.Where(v => !v.Tastat); break;
            }

            // ✅ Ordenación aquí
            q = OrdenacioSeleccionada switch
            {
                "Tots" => q.OrderBy(v => v.Nombre ?? string.Empty), //Per a que tots ordene alfabeticament
                "Global (↓)" => q.OrderByDescending(v => v.PuntuacionGlobal),
                "Global (↑)" => q.OrderBy(v => v.PuntuacionGlobal),
                "Personal (↓)" => q.OrderByDescending(v => v.MiPuntuacion),
                "Personal (↑)" => q.OrderBy(v => v.MiPuntuacion),
                "(A → Z)" => q.OrderBy(v => v.Nombre ?? string.Empty),
                "(Z → A)" => q.OrderByDescending(v => v.Nombre ?? string.Empty),
                "Origen (A → Z)" => q.OrderBy(v => v.Origen ?? string.Empty),
                "Origen (Z → A)" => q.OrderByDescending(v => v.Origen ?? string.Empty),
                _ => q
            };

            var resultado = q.ToList();

            App.Current.Dispatcher.Dispatch(() =>
            {
                Vermuts.Clear();
                foreach (var v in resultado)
                    Vermuts.Add(v);
            });
            // 🔔 avisa a la vista que ya puede re-medir
            RequestRemeasure?.Invoke(this, EventArgs.Empty);
            System.Diagnostics.Debug.WriteLine($"[VM] AplicarFiltre -> {resultado.Count} items (orden: {OrdenacioSeleccionada})");
        }

        private void IniciarTemporitzadorFiltratge()
        {
            if (!_isDataReady) return;    // no debounces hasta tener datos

            IsFiltering = true;

            _debounceTimer?.Stop();
            _debounceTimer?.Dispose();

            _debounceTimer = new System.Timers.Timer(500) { AutoReset = false };
            _debounceTimer.Elapsed += (_, __) =>
            {
                try { AplicarFiltre(); }
                finally
                {
                    IsFiltering = false;
                    OnPropertyChanged(nameof(IsFiltering));
                }
            };
            _debounceTimer.Start();
        }

        // ===== Sorpresa =====
        public void MostrarVermutAleatorio()
        {
            if (Vermuts.Count == 0) return;
            var rnd = new Random();
            VermutSorpresa = Vermuts[rnd.Next(Vermuts.Count)];
            OnPropertyChanged(nameof(VermutSorpresa));
            MostrarSorpresa = true;
        }

        private bool _mostrarSorpresa;
        public bool MostrarSorpresa
        {
            get => _mostrarSorpresa;
            set { if (_mostrarSorpresa != value) { _mostrarSorpresa = value; OnPropertyChanged(); } }
        }

        // ===== Guardar cambios personales (helper opcional) =====
        // Llama a este método cuando el usuario puntúe/edite notas desde la UI.
        public async Task GuardarCambiosPersonalesAsync(Vermut vermut, int miPuntuacion, string? notas, bool tastat = true)
        {
            if (vermut is null || string.IsNullOrWhiteSpace(vermut.Nombre)) return;

            // Actualiza el modelo en memoria
            vermut.MiPuntuacion = miPuntuacion;
            vermut.Tastat = tastat;
            OnPropertyChanged(nameof(Vermuts));
            AplicarFiltre(); // re-ordenación si procede

            // Persiste en local con scope por usuario
            await FirebaseAuthHolder.InitAsync();
            var uid = FirebaseAuthHolder.Uid ?? string.Empty;

            await LocalStorageService.GuardarVermutLocalAsync(uid, new VermutLocal
            {
                UserId = uid,
                Nombre = vermut.Nombre,
                MiPuntuacion = miPuntuacion,
                Notas = notas,
                Tastat = tastat
            });

            // (Opcional) persiste en remoto
            // Si usas el nombre como clave en Firebase:
            // await FirebaseService.GuardarVotoAsync(vermut.Nombre, miPuntuacion);
            // Y si recalculas promedio global, hazlo en FirebaseService.
        }

        // ===== INotifyPropertyChanged =====
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? nom = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nom));

        // ===== Ordenación =====
        public List<string> OrdenacioOpcions { get; } = new()
        {
            "(A → Z)",
            "(Z → A)",
            "Origen (A → Z)",
            "Origen (Z → A)",
            "Global (↓)",
            "Global (↑)",
            "Personal (↓)",
            "Personal (↑)",


        };

        // Que coincida con las opciones
        private string _ordenacioSeleccionada = "(A → Z)";
        public string OrdenacioSeleccionada
        {
            get => _ordenacioSeleccionada;
            set
            {
                if (_ordenacioSeleccionada != value)
                {
                    _ordenacioSeleccionada = value;
                    OnPropertyChanged();
                    if (_isDataReady) AplicarFiltre();
                }
            }
        }

        public event EventHandler? RequestRemeasure;
    }
}
