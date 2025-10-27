using Microsoft.Maui.Controls;
using System;
using System.ComponentModel;
using Microsoft.Maui.ApplicationModel; // MainThread
using VermutRaterApp.Models;

namespace VermutRaterApp.Views.Templates
{
    public partial class VermutFrameView : ContentView
    {
        private Vermut _current;

        public VermutFrameView()
        {
            InitializeComponent();
            BindingContextChanged += OnBindingContextChanged;
        }

        private void OnBindingContextChanged(object? sender, EventArgs e)
        {
            // Desuscribir del anterior
            if (_current != null)
                _current.PropertyChanged -= OnVermutPropertyChanged;

            _current = BindingContext as Vermut;

            if (_current == null)
            {
                Content = null;
                return;
            }

            // Suscribir al nuevo y construir vista inicial
            _current.PropertyChanged += OnVermutPropertyChanged;
            BuildChild(_current);
        }

        private void OnVermutPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // Si cambia la propiedad que decide el template, re-construimos
            if (e.PropertyName == nameof(Vermut.Tastat) /* || e.PropertyName == nameof(Vermut.YaVotado) */)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (_current != null)
                        BuildChild(_current);
                });
            }
        }

        private void BuildChild(Vermut vermut)
        {
            ContentView child = vermut.Tastat
                ? new FavoritoVermutView()
                : new NormalVermutView();

            child.BindingContext = vermut;
            child.HorizontalOptions = LayoutOptions.Fill;
            child.VerticalOptions = LayoutOptions.Start;
            child.Margin = new Thickness(0);
            child.Padding = 0;

            Content = child;
        }
    }
}
