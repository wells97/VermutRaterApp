using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace VermutRaterApp.Converters
{
    public class TastatColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is bool tastat && tastat)
                ? "marco_vermu.png"//Color.FromArgb("#455e3f") // Verd oliva fosc
                : "marco_vermu.png";// Color.FromArgb("#7A2020"); // Vermell original
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

}

