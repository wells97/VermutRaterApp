using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VermutRaterApp.Models;
using VermutRaterApp.Services;
using VermutRaterApp.Helpers;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;

namespace VermutRaterApp.Views
{
    public partial class DetallesPage : ContentPage, IQueryAttributable
    {
        private Vermut _vermut;
        private Vermut _vermutOriginal;
        private AutoFontLabel _fontLabel;
        private bool _estaMarcado;
        bool _animating; // opcional, para evitar taps locos

        public DetallesPage()
        {
            InitializeComponent();
            // _vermutOriginal se setea al recibir el parámetro en ApplyQueryAttributes
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("vermut", out var vermutObj) && vermutObj is Vermut vermut)
            {
                _vermut = vermut;
                // snapshot para detectar cambios al volver
                _vermutOriginal = new Vermut
                {
                    Nombre = vermut.Nombre,
                    Descripcion = vermut.Descripcion,
                    Origen = vermut.Origen,
                    MiPuntuacion = vermut.MiPuntuacion,
                    Notas = vermut.Notas,
                    YaVotado = vermut.YaVotado,
                    Tastat = vermut.Tastat,
                    PuntuacionGlobal = vermut.PuntuacionGlobal
                };

                BindingContext = _vermut;
                _ = CargarDatosLocalesYActualizar();
            }
        }

        private async Task CargarDatosLocalesYActualizar()
        {
            if (_vermut == null) return;

            // UID actual (usa tu helper Usuario.UID o el holder de Firebase)
            var uid = !string.IsNullOrWhiteSpace(Usuario.UID)
                        ? Usuario.UID
                        : (FirebaseAuthHolder.Uid ?? string.Empty);

            // ⛳️ Nueva firma: (userId, vermutName)
            var local = await LocalStorageService.ObtenerVermutLocalAsync(uid, _vermut.Nombre);
            if (local != null)
            {
                _vermut.MiPuntuacion = local.MiPuntuacion;
                _vermut.Notas = local.Notas;
                _vermut.YaVotado = local.YaVotado;
                _vermut.Tastat = local.Tastat;

                _vermut.PuntuacionGlobal =
                    await FirebaseService.ObtenerPuntuacionGlobalDesdeVotosPorUsuarioAsync(_vermut.Nombre);
            }

            ActualizarEstrellas();
            ActualizarGotVermut();

            // 🧪 Debug
            System.Diagnostics.Debug.WriteLine("🧾 Datos del vermut cargado:");
            System.Diagnostics.Debug.WriteLine($"  Nombre: {_vermut.Nombre}");
            System.Diagnostics.Debug.WriteLine($"  Descripción: {_vermut.Descripcion}");
            System.Diagnostics.Debug.WriteLine($"  MiPuntuacion: {_vermut.MiPuntuacion}");
            System.Diagnostics.Debug.WriteLine($"  Notas: {_vermut.Notas}");
            System.Diagnostics.Debug.WriteLine($"  YaVotado: {_vermut.YaVotado}");
            System.Diagnostics.Debug.WriteLine($"  Tastat: {_vermut.Tastat}");
            System.Diagnostics.Debug.WriteLine($"  PuntuacionGlobal: {_vermut.PuntuacionGlobal}");
        }

        private void Star_Clicked(object sender, EventArgs e)
        {
            if (sender is ImageButton boton &&
                int.TryParse(boton.CommandParameter?.ToString(), out int puntuacion) &&
                _vermut.Tastat)
            {
                _vermut.MiPuntuacion = puntuacion;
                ActualizarEstrellas();
            }
            else
            {
                if (Idioma.idioma == "ca") { Toast.Make("No pots votar un vermut que encara no has tastat!").Show(); }
                else if (Idioma.idioma == "es") { Toast.Make("¡No puedes votar un vermut que aún no has catado!").Show(); }
            }
        }

        private void ActualizarEstrellas()
        {
            if (_vermut == null || StarButtonsLayout == null) return;

            var estrellas = StarButtonsLayout.Children.OfType<ImageButton>().ToList();

            for (int i = 0; i < estrellas.Count; i++)
            {
                estrellas[i].Source = i < _vermut.MiPuntuacion
                    ? "estrella_activa.png"
                    : "estrella_inactiva.png";
            }
        }

        private void ActualizarGotVermut()
        {
            if (_vermut == null) return;

            var gots = new[] { Global1, Global2, Global3, Global4, Global5 };
            for (int i = 0; i < gots.Length; i++)
                gots[i].IsVisible = false;

            int puntuacio = (int)Math.Round(_vermut.PuntuacionGlobal);
            if (puntuacio < 2)
                gots[0].IsVisible = true;
            else
                gots[Math.Max(0, Math.Min(gots.Length - 1, (int)Math.Round(_vermut.PuntuacionGlobal) - 1))].IsVisible = true;

            PuntuacioGlobalLabel.Text = "⭐ " + _vermut.PuntuacionGlobal.ToString("0.0");
        }

        private async void Volver_Clicked(object sender, EventArgs e)
        {
            if (_vermut == null || _vermut == _vermutOriginal) return;

            if (_vermut.MiPuntuacion > 0)
            {
                if (string.IsNullOrWhiteSpace(Usuario.UID))
                    throw new InvalidOperationException("UID del usuario no disponible. Vuelve a iniciar sesión.");

                await FirebaseService.GuardarPuntuacionUsuarioAsync(new VotoVermut(_vermut.Nombre, _vermut.MiPuntuacion));
            }

            var uid = Usuario.UID; // ya validado arriba

            // ⛳️ Nueva firma: (userId, VermutLocal)
            await LocalStorageService.GuardarVermutLocalAsync(uid, new VermutLocal
            {
                UserId = uid,
                Nombre = _vermut.Nombre,
                MiPuntuacion = _vermut.MiPuntuacion,
                Notas = _vermut.Notas,
                YaVotado = _vermut.YaVotado,
                Tastat = _vermut.Tastat
            });

            _vermut.PuntuacionGlobal =
                await FirebaseService.ObtenerPuntuacionGlobalDesdeVotosPorUsuarioAsync(_vermut.Nombre);

            _vermut.NotifyStarsChanged();

            var snackbar = Snackbar.Make("✅ Guardado correctamente", duration: TimeSpan.FromSeconds(2));
            await snackbar.Show();

            await Shell.Current.GoToAsync("///MainPage", true, new Dictionary<string, object>
            {
                ["vermut"] = _vermut
            });

            // 🧪 Debug
            System.Diagnostics.Debug.WriteLine("🧾 Datos del vermut guardado:");
            System.Diagnostics.Debug.WriteLine($"  Nombre: {_vermut.Nombre}");
            System.Diagnostics.Debug.WriteLine($"  Descripción: {_vermut.Descripcion}");
            System.Diagnostics.Debug.WriteLine($"  MiPuntuacion: {_vermut.MiPuntuacion}");
            System.Diagnostics.Debug.WriteLine($"  Notas: {_vermut.Notas}");
            System.Diagnostics.Debug.WriteLine($"  YaVotado: {_vermut.YaVotado}");
            System.Diagnostics.Debug.WriteLine($"  Tastat: {_vermut.Tastat}");
            System.Diagnostics.Debug.WriteLine($"  PuntuacionGlobal: {_vermut.PuntuacionGlobal}");
        }

        private async void OnCheckTapped(object sender, EventArgs e)
        {
            if (_animating) return;
            _animating = true;

            var v = _vermut;
            v.Tastat = !v.Tastat;
            CheckMark.IsChecked = v.Tastat;

            try
            {
                if (v.Tastat)
                {
                    CheckMark.Scale = 0.85;
                    await CheckMark.ScaleTo(1.0, 140, Easing.SpringOut);
                }
                else
                {
                    await CheckMark.ScaleTo(0.9, 90, Easing.CubicIn);
                    await CheckMark.ScaleTo(1.0, 1);
                }
            }
            finally
            {
                _animating = false;
            }
        }
    }
}
