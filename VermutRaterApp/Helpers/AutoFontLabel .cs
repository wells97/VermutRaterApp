using Microsoft.Maui.Controls;

namespace VermutRaterApp.Helpers
{
    public class AutoFontLabel : Label
    {
        private bool _isAdjusting = false;

        public AutoFontLabel()
        {
            LineBreakMode = LineBreakMode.WordWrap;

            PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(Text))
                    AdjustLater();
            };

            SizeChanged += (_, __) => AdjustLater();
        }

        private void AdjustLater()
        {
            // Esperar tras render para evitar errores de altura 0
            Device.StartTimer(TimeSpan.FromMilliseconds(100), () =>
            {
                AdjustFontSize();
                return false;
            });
        }

        private void AdjustFontSize()
        {
            if (_isAdjusting || string.IsNullOrWhiteSpace(Text) || Width <= 0 || Height <= 0)
                return;

            _isAdjusting = true;

            double maxFontSize = Height * 0.6;
            double minFontSize = 20;
            double currentFontSize = maxFontSize;

            var probe = new Label
            {
                FontFamily = FontFamily,
                FontAttributes = FontAttributes,
                LineBreakMode = LineBreakMode.CharacterWrap,
                HorizontalTextAlignment = HorizontalTextAlignment,
                VerticalTextAlignment = VerticalTextAlignment,
                Text = Text
            };

            while (currentFontSize > minFontSize)
            {
                probe.FontSize = currentFontSize;
                var size = probe.Measure(Width, Height);

                if (size.Width <= Width &&
                    size.Height <= Height)
                    break;

                currentFontSize--;
            }

            FontSize = Math.Max(currentFontSize, minFontSize);
            _isAdjusting = false;
        }



    }
}
