/*// Helpers/AudioPlayer.cs
using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;

namespace VermutRaterApp.Helpers
{
    public static class AudioPlayer
    {
        private static MediaElement mediaElement;

        public static void PlaySound(string fileName)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (mediaElement == null)
                {
                    mediaElement = new MediaElement
                    {
                        AutoPlay = false,
                        IsVisible = false,
                        ShowsPlaybackControls = false,
                        HeightRequest = 1,
                        WidthRequest = 1
                    };

                    if (Application.Current?.MainPage is ContentPage page &&
                        page.FindByName<Grid>("RootGrid") is Grid grid)
                    {
                        grid.Children.Add(mediaElement);
                    }
                }

                mediaElement.Source = MediaSource.FromFile(fileName);
                mediaElement.Play();
            });
        }
    }
}

*/