using Microsoft.Maui.Controls;
using Microsoft.Maui;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using VermutRaterApp.ViewModels;

namespace VermutRaterApp
{
    public partial class MainPage : ContentPage
    {
        private readonly MainViewModel viewModel;

        public MainPage()
        {
            InitializeComponent();
            BindingContext = viewModel = new MainViewModel();
            viewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        private async void BotonSorpresa_Clicked(object sender, EventArgs e)
        {
            await BotonSorpresa.ScaleTo(0.9, 50, Easing.CubicOut);
            await BotonSorpresa.ScaleTo(1.0, 100, Easing.CubicIn);
            // Aquí puedes mostrar un vermut aleatorio si implementas esa función en el ViewModel
            viewModel.MostrarVermutAleatorio();
        }

        private async void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(viewModel.MostrarSorpresa) && viewModel.MostrarSorpresa)
            {
                SorpresaFrame.Opacity = 0;
                SorpresaFrame.Scale = 0;
                SorpresaFrame.IsVisible = true;

                await SorpresaFrame.ScaleTo(1.05, 200, Easing.CubicOut);
                await SorpresaFrame.ScaleTo(1.0, 100, Easing.CubicIn);
                await SorpresaFrame.FadeTo(1, 150);

                try { Vibration.Default.Vibrate(); } catch { }

                await Task.Delay(4000);

                await SorpresaFrame.FadeTo(0, 400);
                SorpresaFrame.IsVisible = false;
                viewModel.MostrarSorpresa = false;
            }
        }

        private async void AbrirMapa_Clicked(object sender, EventArgs e)
        {
            try
            {
                var uri = new Uri("geo:0,0?q=Tu+bar+favorito");
                await Launcher.Default.OpenAsync(uri);
            }
            catch (Exception)
            {
                await DisplayAlert("Error", "No se pudo abrir el mapa.", "OK");
            }
        }
    }
}
