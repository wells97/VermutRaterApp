using Microsoft.Maui.ApplicationModel; // Launcher, MainThread
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;          // DeviceDisplay, Vibration
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using VermutRaterApp.Helpers;
using VermutRaterApp.Models;
using VermutRaterApp.Services;
using VermutRaterApp.ViewModels;

namespace VermutRaterApp.Views
{
    public partial class MainPage : ContentPage, IQueryAttributable
    {
        private readonly INotificationManagerService _notificacions;
        private readonly MainViewModel viewModel = new();

        public MainPage(INotificationManagerService notificacions)
        {
            InitializeComponent();
            _notificacions = notificacions;
            BindingContext = viewModel;

            // Elegimos ItemsLayout UNA sola vez (evita recalculazos que colapsan la lista)
            double anchoDp = DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density;
            VermutCollectionView.ItemsLayout = anchoDp >= 720
                ? new GridItemsLayout(2, ItemsLayoutOrientation.Vertical)
                : new LinearItemsLayout(ItemsLayoutOrientation.Vertical);

            // Cargamos los vermuts SOLO una vez
            _ = viewModel.CargarVermutsAsync();

            // Spinner + fade-in al terminar la carga
            viewModel.PropertyChanged += async (_, e) =>
            {
                if (e.PropertyName == nameof(MainViewModel.IsLoading))
                {
                    if (!viewModel.IsLoading)
                    {
                        await MainThread.InvokeOnMainThreadAsync(async () =>
                        {
                            VermutCollectionView.InvalidateMeasure();
                            await VermutCollectionView.FadeTo(1, 220, Easing.CubicOut);
                        });
                    }
                    else
                    {
                        // Si vuelve a cargar, empieza oculta
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            VermutCollectionView.Opacity = 0;
                        });
                    }
                }
            };

            // Overlay sorpresa
            viewModel.PropertyChanged += async (_, e) =>
            {
                if (e.PropertyName == nameof(viewModel.MostrarSorpresa) && viewModel.MostrarSorpresa)
                    await MostrarSorpresaAsync();
            };

            // Establecer idioma guardado al iniciar
            string idiomaActual = Preferences.Get("idioma", "ca");
            LocalizationResourceManager.Instance.SetCulture(new CultureInfo(idiomaActual));
            IdiomaIcono.Source = idiomaActual == "ca" ? "flag_ca.png" : "flag_es.png";
            IdiomaTexto.Text = idiomaActual == "ca" ? "CA" : "ES";

            Idioma.idioma = idiomaActual;
        }

        // Actualiza un item al volver de Detalles
        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("vermut", out var vermutObj) && vermutObj is Vermut vermut)
            {
                var original = viewModel.Vermuts.FirstOrDefault(v => v.Nombre == vermut.Nombre);
                if (original != null)
                {
                    // Copia propiedad a propiedad (dispara OnPropertyChanged en cada setter)
                    original.MiPuntuacion = vermut.MiPuntuacion;
                    original.PuntuacionGlobal = vermut.PuntuacionGlobal;
                    original.Notas = vermut.Notas;
                    original.YaVotado = vermut.YaVotado;
                    original.Tastat = vermut.Tastat;

                    // Por si acaso (converters/reciclado)
                    original.NotifyStarsChanged();
                }
                else
                {
                    viewModel.Vermuts.Add(vermut);
                }
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // (Opcional) aviso si hay sin puntuar
            // if (viewModel.Vermuts.Any(v => v.MiPuntuacion == null))
            //     _notificacions.ShowVermutReminder();
        }

        // ====== Overlay Sorpresa ======
        private async Task MostrarSorpresaAsync()
        {
            SorpresaOverlay.IsVisible = true;
            SorpresaOverlay.Opacity = 0;
            SorpresaFrame.Scale = 0.95;

            await Task.WhenAll(
                SorpresaOverlay.FadeTo(1, 200, Easing.CubicOut),
                SorpresaFrame.ScaleTo(1, 200, Easing.CubicOut)
            );

            try { Vibration.Default.Vibrate(50); } catch { }

            await Task.Delay(3000);

            await HideSorpresaOverlayAsync();
            viewModel.MostrarSorpresa = false;
        }

        private async Task HideSorpresaOverlayAsync()
        {
            await Task.WhenAll(
                SorpresaOverlay.FadeTo(0, 150, Easing.CubicIn),
                SorpresaFrame.ScaleTo(0.97, 150, Easing.CubicIn)
            );
            SorpresaOverlay.IsVisible = false;
            SorpresaFrame.Scale = 1;
        }

        private async void OnOverlayTapped(object sender, EventArgs e)
        {
            await HideSorpresaOverlayAsync();
            viewModel.MostrarSorpresa = false;
        }
        // ====== Fin overlay ======

        private async void BotonSorpresa_Clicked(object sender, EventArgs e)
        {
            await BotonSorpresa.ScaleTo(0.9, 50, Easing.CubicOut);
            await BotonSorpresa.ScaleTo(1.0, 100, Easing.CubicIn);
            AudioPlayer.ReproducirSonido("sorpresa.mp3");
            viewModel.MostrarVermutAleatorio();
        }

        private async void MapaButton_Clicked(object sender, EventArgs e)
        {
            try
            {
                await Launcher.Default.OpenAsync("https://maps.app.goo.gl/5HVoXcjwVF7SSjrZ7");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "No se pudo abrir el mapa: " + ex.Message, "OK");
            }
        }

        private async void OnIdiomaSelectorTapped(object sender, EventArgs e)
        {
            string idiomaSeleccionado = await DisplayActionSheet("Selecciona idioma", null, null, "Castellano", "Català");

            if (idiomaSeleccionado == "Castellano")
            {
                CambiarIdioma("es", "flag_es.png", "ES");
            }
            else if (idiomaSeleccionado == "Català")
            {
                CambiarIdioma("ca", "flag_ca.png", "CA");
            }
        }


        private void CambiarIdioma(string idioma, string icono, string texto)
        {
            var culture = new CultureInfo(idioma);

            LocalizationResourceManager.Instance.SetCulture(culture);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;

            Preferences.Set("idioma", idioma);
            IdiomaIcono.Source = icono;
            IdiomaTexto.Text = texto;

            //xapuza helper idioma
            Idioma.idioma = idioma;
        }
    }
}
