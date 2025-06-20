using Microsoft.Maui.Controls;
using System;
using VermutRaterApp.Models;
using VermutRaterApp.Services;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;

namespace VermutRaterApp.Views
{
    public partial class DetallesPage : ContentPage, IQueryAttributable
    {
        private Vermut _vermut;

        public DetallesPage()
        {
            InitializeComponent();
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("vermut", out var vermutObj) && vermutObj is Vermut vermut)
            {
                _vermut = vermut;
                BindingContext = _vermut;
                _ = CargarDatosLocalesYActualizar();
            }
        }

        private async Task CargarDatosLocalesYActualizar()
        {
            if (_vermut == null) return;

            var local = await LocalStorageService.ObtenerVermutLocalAsync(_vermut.Nombre);
            if (local != null)
            {
                _vermut.MiPuntuacion = local.MiPuntuacion;
                _vermut.Notas = local.Notas;
                _vermut.YaVotado = local.YaVotado;
            }

            ActualizarEstrellas();

            _vermut.PuntuacionGlobal = await FirebaseService.ObtenerPuntuacionGlobalDesdeVotosPorUsuarioAsync(_vermut.Nombre);
        }

        private void Estrella_Clicked(object sender, EventArgs e)
        {
            if (_vermut == null) return;

            if (sender is ImageButton boton &&
                int.TryParse(boton.CommandParameter?.ToString(), out int puntuacion))
            {
                _vermut.MiPuntuacion = puntuacion;
                ActualizarEstrellas();
            }
        }

        private void ActualizarEstrellas()
        {
            if (_vermut == null) return;

            var estrellas = new[] { Star1, Star2, Star3, Star4, Star5 };

            for (int i = 0; i < estrellas.Length; i++)
            {
                estrellas[i].Source = i < _vermut.MiPuntuacion
                    ? "estrella_activa.png"
                    : "estrella_inactiva.png";
            }
        }

        private async void Volver_Clicked(object sender, EventArgs e)
        {
            if (_vermut == null) return;

            string usuarioId = UsuarioService.ObtenerUsuarioId();

            await FirebaseService.GuardarPuntuacionUsuarioAsync(_vermut.Nombre, usuarioId, _vermut.MiPuntuacion);

            await LocalStorageService.GuardarVermutLocalAsync(new VermutLocal
            {
                Nombre = _vermut.Nombre,
                MiPuntuacion = _vermut.MiPuntuacion,
                Notas = _vermut.Notas,
                YaVotado = true
            });

            _vermut.PuntuacionGlobal = await FirebaseService.ObtenerPuntuacionGlobalDesdeVotosPorUsuarioAsync(_vermut.Nombre);

            var snackbar = Snackbar.Make("✅ Guardado correctamente", duration: TimeSpan.FromSeconds(2));
            await snackbar.Show();

            await Navigation.PopAsync();
        }
    }
}
