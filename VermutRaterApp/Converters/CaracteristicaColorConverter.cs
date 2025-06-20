using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace VermutRaterApp.Converters
{
    public class CaracteristicaColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string c = value?.ToString()?.ToLowerInvariant() ?? "";

            return c switch
            {
                "blanco" => Colors.SkyBlue,
                "rojo" => Colors.IndianRed,
                "seco" => Colors.Goldenrod,
                "dulce" => Colors.MediumOrchid,
                "amargo" => Colors.ForestGreen,
                "rosado" => Colors.LightPink,
                _ => Colors.Gray
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }
}
