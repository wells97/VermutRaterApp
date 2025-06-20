using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace VermutRaterApp.Converters
{
    public class PuntuacioVisibleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int puntuacio && parameter is string nivelString && int.TryParse(nivelString, out int nivel))
            {
                return puntuacio >= nivel;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
