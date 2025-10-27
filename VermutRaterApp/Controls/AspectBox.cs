using System;
using Microsoft.Maui.Controls;

namespace VermutRaterApp.Controls
{
    // Alto = Ancho * Aspect
    public class AspectBox : ContentView
    {
        public static readonly BindableProperty AspectProperty =
            BindableProperty.Create(nameof(Aspect), typeof(double), typeof(AspectBox), 1.0);

        public double Aspect
        {
            get => (double)GetValue(AspectProperty);
            set => SetValue(AspectProperty, value);
        }

        public AspectBox()
        {
            this.SizeChanged += (_, __) =>
            {
                if (Width > 0)
                    HeightRequest = 206.5;//Width * Aspect;
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[AlturaActual]: {HeightRequest})");
#endif
            };
        }
    }
}
