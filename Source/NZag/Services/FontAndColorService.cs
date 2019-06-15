using NZag.Core;
using System;
using System.ComponentModel.Composition;
using System.Windows.Media;

namespace NZag.Services
{
    [Export]
    public class FontAndColorService
    {
        private readonly Brush _defaultForegroundBrush = Brushes.Black;
        private readonly Brush _defaultBackgroundBrush = Brushes.White;

        private static Brush GetZColorBrush(ZColor color) => color switch
        {
            ZColor.Black => Brushes.Black,
            ZColor.Blue => Brushes.Blue,
            ZColor.Cyan => Brushes.Cyan,
            ZColor.Gray => Brushes.Gray,
            ZColor.Green => Brushes.Green,
            ZColor.Magenta => Brushes.Magenta,
            ZColor.Red => Brushes.Red,
            ZColor.White => Brushes.White,
            ZColor.Yellow => Brushes.Yellow,
            _ => throw new ArgumentException("Unexpected color: " + color, nameof(color))
        };

        public void SetForegroundColor(ZColor foreground)
        {
            ForegroundBrush = foreground == ZColor.Default
                ? _defaultForegroundBrush
                : GetZColorBrush(foreground);
        }

        public void SetBackgroundColor(ZColor background)
        {
            BackgroundBrush = background == ZColor.Default
                ? _defaultBackgroundBrush
                : GetZColorBrush(background);
        }

        public Brush ForegroundBrush { get; private set; } = Brushes.Black;

        public Brush BackgroundBrush { get; private set; } = Brushes.White;

        public double FontSize { get; } = 20.0;
    }
}
