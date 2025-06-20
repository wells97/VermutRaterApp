using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls;

namespace VermutRaterApp.Services
{
    public static class AudioPlayer
    {
        private static MediaElement? _mediaElement;

        public static void ReproducirSonido(string nombreArchivoConExtension)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (_mediaElement == null)
                {
                    _mediaElement = new MediaElement
                    {
                        Volume = 1.0,
                        HeightRequest = 0,
                        WidthRequest = 0,
                        IsVisible = false
                    };

                    if (Application.Current?.MainPage is ContentPage contentPage)
                    {
                        var layout = contentPage.FindByName<Layout>("RootLayout");
                        layout?.Children.Add(_mediaElement);
                    }
                }

                _mediaElement.Source = MediaSource.FromFile(nombreArchivoConExtension);
                _mediaElement.Play();
            });
        }
    }
}
