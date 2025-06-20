using Microsoft.Maui.Controls;
using VermutRaterApp.Services;

namespace VermutRaterApp
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // Inicializar el identificador único del usuario
            UsuarioService.InicializarUsuarioAsync();

            // Usar AppShell como punto de entrada
            MainPage = new NavigationPage(new VermutRaterApp.Views.LoginPage());
        }
    }
}
