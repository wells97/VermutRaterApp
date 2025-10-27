using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Maui.Controls;

namespace VermutRaterApp.Converters
{
    public class FixedWidthWrapConverter : IValueConverter
    {
        // Ajusta aquí si quieres otros límites
        private const int MaxCharsPerLine = 16;
        private const int MaxLines = 3;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var s = (value as string) ?? string.Empty;
            s = Regex.Replace(s.Trim(), @"\s+", " "); // normaliza espacios

            if (s.Length <= MaxCharsPerLine) return s;

            var lines = new System.Collections.Generic.List<string>();
            var words = s.Split(' ');

            string current = "";
            foreach (var w in words)
            {
                // Si la palabra cabe en la línea actual (considerando el espacio)
                var sep = current.Length == 0 ? "" : " ";
                if (current.Length + sep.Length + w.Length <= MaxCharsPerLine)
                {
                    current += sep + w;
                    continue;
                }

                // Si la palabra no cabe, cerramos la línea actual (si tiene algo)
                if (!string.IsNullOrEmpty(current))
                {
                    lines.Add(current);
                    current = "";
                    if (lines.Count == MaxLines) break;
                }

                // Ahora intentamos meter la palabra (puede ser más larga que el límite)
                int idx = 0;
                while (idx < w.Length && lines.Count < MaxLines)
                {
                    int remaining = MaxCharsPerLine;
                    int take = Math.Min(remaining, w.Length - idx);
                    string chunk = w.Substring(idx, take);

                    // Si el trozo llenaría la línea entera, lo ponemos como línea completa
                    if (take == MaxCharsPerLine)
                    {
                        lines.Add(chunk);
                    }
                    else
                    {
                        current = chunk; // empezamos la nueva línea con el resto de la palabra
                    }
                    idx += take;

                    if (lines.Count == MaxLines && (idx < w.Length || !string.IsNullOrEmpty(current)))
                        break;
                }

                if (lines.Count == MaxLines) break;
            }

            // Añade última línea si quedó texto
            if (lines.Count < MaxLines && !string.IsNullOrEmpty(current))
                lines.Add(current);

            // Si aún quedaba contenido por procesar, añadimos “…” al final de la última línea
            string original = s;
            string rebuilt = string.Join(" ", original.Split(' ').ToList()); // igual que original normalizado
            string joined = string.Join("\n", lines);

            // Detecta si truncamos: reconstruimos sin saltos y comparamos inicio
            var flat = string.Concat(lines.Select(l => l)).Replace("\n", "");
            bool truncated = false;
            {
                // reconstrucción aproximada para saber si falta algo
                var withoutSpaces = Regex.Replace(rebuilt, @"\s+", "");
                var builtNoSpaces = Regex.Replace(string.Join("", lines), @"\s+", "");
                truncated = builtNoSpaces.Length < withoutSpaces.Length;
            }

            if (truncated && lines.Count > 0)
            {
                // añade “…” a la última línea sin pasarse del límite
                int last = lines.Count - 1;
                var ellipsis = "…";
                if (lines[last].Length + ellipsis.Length > MaxCharsPerLine)
                    lines[last] = lines[last].Substring(0, Math.Max(0, MaxCharsPerLine - ellipsis.Length)) + ellipsis;
                else
                    lines[last] += ellipsis;
            }

            return string.Join("\n", lines);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => value;
    }
}
