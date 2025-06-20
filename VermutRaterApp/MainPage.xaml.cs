using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;
using System;
using System.ComponentModel;
using VermutRaterApp.Services;
using VermutRaterApp.ViewModels;

namespace VermutRaterApp.Views
{
    public partial class MainPage : ContentPage
    {
        private MainViewModel viewModel = new();

        public MainPage()
        {
            InitializeComponent();
            BindingContext = viewModel;

            this.BindingContextChanged += async (_, _) =>
            {
                if (BindingContext is MainViewModel vm && vm.Vermuts.Count == 0)
                    await vm.CargarVermutsAsync();
            };

            viewModel.PropertyChanged += async (_, e) =>
            {
                if (e.PropertyName == nameof(viewModel.MostrarSorpresa) && viewModel.MostrarSorpresa)
                    await MostrarSorpresaAsync();
            };

            _ = viewModel.CargarVermutsAsync();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            double ancho = DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density;
            VermutCollectionView.ItemsLayout = ancho >= 720
                ? new GridItemsLayout(2, ItemsLayoutOrientation.Vertical)
                : new LinearItemsLayout(ItemsLayoutOrientation.Vertical);
        }

        private async void BotonSorpresa_Clicked(object sender, EventArgs e)
        {
            await BotonSorpresa.ScaleTo(0.9, 50, Easing.CubicOut);
            await BotonSorpresa.ScaleTo(1.0, 100, Easing.CubicIn);
            AudioPlayer.ReproducirSonido("sorpresa.mp3");
            viewModel.MostrarVermutAleatorio();
        }

        private async Task MostrarSorpresaAsync()
        {
            SorpresaFrame.Opacity = 0;
            SorpresaFrame.Scale = 0;
            SorpresaFrame.IsVisible = true;

            await SorpresaFrame.ScaleTo(1.05, 200, Easing.CubicOut);
            await SorpresaFrame.ScaleTo(1.0, 100, Easing.CubicIn);
            await SorpresaFrame.FadeTo(1, 150);

            try { Vibration.Default.Vibrate(); } catch { }

            await Task.Delay(3000);

            await SorpresaFrame.FadeTo(0, 400);
            SorpresaFrame.IsVisible = false;
            viewModel.MostrarSorpresa = false;
        }

        private async void MapaButton_Clicked(object sender, EventArgs e)
        {
            try
            {
                await Launcher.Default.OpenAsync("https://www.google.com/maps/place/Vermuter%C3%ADa+Lo+Rac%C3%B3+del+Vermutenc/@40.6178024,0.590393,21z/data=!4m6!3m5!1s0x12a05522045b5ee9:0x73e9338bf7a9f006!8m2!3d40.6178404!4d0.5904031!16s%2Fg%2F11q8k00mb9?entry=ttu&g_ep=EgoyMDI1MDYwNC4wIKXMDSoASAFQAw%3D%3D");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "No se pudo abrir el mapa: " + ex.Message, "OK");
            }
        }

        public async Task GuardarPuntuacionAsync(VermutRaterApp.Models.Vermut vermut)
        {
            await LocalStorageService.GuardarVermutLocalAsync(new Models.VermutLocal
            {
                Nombre = vermut.Nombre,
                MiPuntuacion = vermut.MiPuntuacion,
                Notas = vermut.Notas,
                YaVotado = true
            });

            await FirebaseService.GuardarPuntuacionUsuarioAsync(vermut.Nombre, UsuarioService.ObtenerUsuarioId(), vermut.MiPuntuacion);
            vermut.PuntuacionGlobal = await FirebaseService.ObtenerPuntuacionGlobalDesdeVotosPorUsuarioAsync(vermut.Nombre);
        }
    }
}
