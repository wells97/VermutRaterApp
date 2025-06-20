using Microsoft.Maui.Controls;
using VermutRaterApp.Models;
using VermutRaterApp.Views;

namespace VermutRaterApp.Views.Templates
{
    public partial class NormalVermutView : ContentView
    {
        public NormalVermutView()
        {
            InitializeComponent();
        }

        private async void VerDetalles_Clicked(object sender, EventArgs e)
        {
            if (BindingContext is Vermut vermut)
            {
                await Shell.Current.GoToAsync(nameof(DetallesPage), true, new Dictionary<string, object>
                {
                    ["vermut"] = vermut
                });
            }
        }
    }
}
