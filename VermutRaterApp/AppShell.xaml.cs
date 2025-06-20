using Microsoft.Maui.Controls;
using VermutRaterApp.Views;

namespace VermutRaterApp
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(DetallesPage), typeof(DetallesPage));



        }
    }
}
