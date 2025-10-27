using System;
using Microsoft.Maui.Controls;

namespace VermutRaterApp.Behaviors
{
    public class ViewportPaddingBehavior : Behavior<Grid>
    {
        Grid? _grid;
        bool _applied;

        // Breakpoints (puedes cambiarlos una sola vez aquí)
        public double SmallMaxWidth { get; set; } = 400; // dp
        public double MediumMaxWidth { get; set; } = 720;

        // Preset PHONE COMPACT (tu caso 360dp)
        public double SmallSidePct { get; set; } = 0.028;
        public double SmallTopPct { get; set; } = 0.055;
        public double SmallBottomPct { get; set; } = 0.060;
        public double SmallSideMin { get; set; } = 8;
        public double SmallTopMin { get; set; } = 32;
        public double SmallBottomMin { get; set; } = 36;

        // Preset PHONE GRANDE
        public double MediumSidePct { get; set; } = 0.035;
        public double MediumTopPct { get; set; } = 0.065;
        public double MediumBottomPct { get; set; } = 0.070;
        public double MediumSideMin { get; set; } = 12;
        public double MediumTopMin { get; set; } = 44;
        public double MediumBottomMin { get; set; } = 48;

        // Preset TABLET
        public double LargeSidePct { get; set; } = 0.050;
        public double LargeTopPct { get; set; } = 0.080;
        public double LargeBottomPct { get; set; } = 0.080;
        public double LargeSideMin { get; set; } = 16;
        public double LargeTopMin { get; set; } = 56;
        public double LargeBottomMin { get; set; } = 56;

        protected override void OnAttachedTo(Grid bindable)
        {
            base.OnAttachedTo(bindable);
            _grid = bindable;
            bindable.Padding = new Thickness(0); // que no gane el XAML
            bindable.Loaded += OnLoaded;
            bindable.SizeChanged += OnSizeChanged;
        }

        protected override void OnDetachingFrom(Grid bindable)
        {
            base.OnDetachingFrom(bindable);
            bindable.Loaded -= OnLoaded;
            bindable.SizeChanged -= OnSizeChanged;
            _grid = null;
            _applied = false;
        }

        void OnLoaded(object? s, EventArgs e) { _applied = false; Apply(force: true); }
        void OnSizeChanged(object? s, EventArgs e) { Apply(); }

        void Apply(bool force = false)
        {
            if (_grid == null) return;

            double w = _grid.Width;
            double h = _grid.Height;
            if (w <= 0 || h <= 0)
            {
                _grid.Dispatcher.Dispatch(() => Apply(force));
                return;
            }

            // Selecciona preset por breakpoint
            double sidePct, topPct, bottomPct, sideMin, topMin, bottomMin;
            if (w < SmallMaxWidth)
            { sidePct = SmallSidePct; topPct = SmallTopPct; bottomPct = SmallBottomPct; sideMin = SmallSideMin; topMin = SmallTopMin; bottomMin = SmallBottomMin; }
            else if (w < MediumMaxWidth)
            { sidePct = MediumSidePct; topPct = MediumTopPct; bottomPct = MediumBottomPct; sideMin = MediumSideMin; topMin = MediumTopMin; bottomMin = MediumBottomMin; }
            else
            { sidePct = LargeSidePct; topPct = LargeTopPct; bottomPct = LargeBottomPct; sideMin = LargeSideMin; topMin = LargeTopMin; bottomMin = LargeBottomMin; }

            double side = Math.Max(sideMin, w * sidePct);
            double top = Math.Max(topMin, h * topPct);
            double bottom = Math.Max(bottomMin, h * bottomPct);

            var newPad = new Thickness(side, top, side, bottom);
            if (!force && _applied && _grid.Padding.Equals(newPad)) return;

            var old = _grid.Padding;
            _grid.Padding = newPad;
            _applied = true;

#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[ViewportBehavior] w={w:0} h={h:0} | old=({old.Left:0},{old.Top:0},{old.Right:0},{old.Bottom:0}) -> new=({newPad.Left:0},{newPad.Top:0},{newPad.Right:0},{newPad.Bottom:0})");
#endif
        }
    }
}
