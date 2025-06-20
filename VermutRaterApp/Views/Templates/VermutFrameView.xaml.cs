using Microsoft.Maui.Controls;
using System;
using VermutRaterApp.Models;
using VermutRaterApp.Views.Templates;

namespace VermutRaterApp.Views.Templates
{
    public partial class VermutFrameView : ContentView
    {
        public VermutFrameView()
        {
            InitializeComponent();
            this.BindingContextChanged += OnBindingContextChanged;
        }

        private void OnBindingContextChanged(object? sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("🧪 BindingContext changed: {BindingContext?.GetType().Name}\"");

            if (BindingContext is not Vermut vermut)
            {
                Content = new Label
                {
                    Text = "(⚠️ BindingContext nulo o inválido)\"",
                    TextColor = Colors.Red,
                    Margin = new Thickness(10)
                };
                return;
            }

            Content = vermut.EsFavorito
                ? new FavoritoVermutView { BindingContext = vermut }
                : new NormalVermutView { BindingContext = vermut };
        }
    }
}