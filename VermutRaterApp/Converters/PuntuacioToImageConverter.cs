using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace VermutRaterApp.Converters
{
    public class PuntuacioToImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is float puntuacio)
            {
                int valor = Math.Clamp((int)Math.Round(puntuacio), 1, 5);
                return $"got_puntuacio_{valor}.png";
            }

            return "got_puntuacio_1.png"; // Per defecte
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
