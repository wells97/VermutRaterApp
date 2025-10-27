using System.Globalization;

namespace VermutRaterApp.Converters
{
    public class PuntuacioVisibleConverter : IValueConverter
    {
        // Devuelve true si MiPuntuacion >= umbral (1..5)
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null || parameter is null) return false;

            if (!double.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var puntuacion))
                return false;

            if (!double.TryParse(parameter.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var umbral))
                return false;

            return puntuacion >= umbral - 1e-6; // tolerancia por decimales
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
