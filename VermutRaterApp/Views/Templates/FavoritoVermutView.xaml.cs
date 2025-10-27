using Microsoft.Maui.Controls;
using VermutRaterApp.Models;
using VermutRaterApp.Views;

namespace VermutRaterApp.Views.Templates
{
    public partial class FavoritoVermutView : ContentView
    {
        public FavoritoVermutView()
        {
            InitializeComponent();
        }

        private async void OnFrameTapped(object sender, EventArgs e)
        {
            if (BindingContext is Vermut vermut)
            {

                try
                {

                    await Shell.Current.GoToAsync(nameof(DetallesPage), true, new Dictionary<string, object>
                    {
                        ["vermut"] = vermut
                    });

                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Error navegant a DetallesPage forçant carrega de pagina: {ex.Message}");
                }
            }
        }
    }
    
}
