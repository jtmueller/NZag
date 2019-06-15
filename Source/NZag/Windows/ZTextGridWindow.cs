using NZag.Controls;
using NZag.Services;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace NZag.Windows
{
    internal class ZTextGridWindow : ZWindow
    {
        private readonly Size fontCharSize;
        private readonly ZTextGrid textGrid;

        private bool bold;
        private bool italic;
        private bool reverse;

        public ZTextGridWindow(ZWindowManager manager, FontAndColorService fontAndColorService)
            : base(manager, fontAndColorService)
        {
            var zero = new FormattedText(
                textToFormat: "0",
                culture: CultureInfo.InstalledUICulture,
                flowDirection: FlowDirection.LeftToRight,
                typeface: new Typeface(new FontFamily("Consolas"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal),
                emSize: FontSize,
                foreground: Brushes.Black,
                pixelsPerDip: 1.0);

            fontCharSize = new Size(zero.Width, zero.Height);

            textGrid = new ZTextGrid(FontSize);
            Children.Add(textGrid);
        }

        public override bool SetBold(bool value)
        {
            bool oldValue = bold;
            bold = value;
            textGrid.SetBold(value);
            return oldValue;
        }

        public override bool SetItalic(bool value)
        {
            bool oldValue = italic;
            italic = value;
            textGrid.SetItalic(value);
            return oldValue;
        }

        public override bool SetReverse(bool value)
        {
            bool oldValue = reverse;
            reverse = value;
            textGrid.SetReverse(value);
            return oldValue;
        }

        public override void Clear() => textGrid.Clear();

        public override void PutChar(char ch, bool forceFixedWidthFont)
        {
            Brush foregroundBrush, backgroundBrush;
            if (reverse)
            {
                foregroundBrush = BackgroundBrush;
                backgroundBrush = ForegroundBrush;
            }
            else
            {
                foregroundBrush = ForegroundBrush;
                backgroundBrush = BackgroundBrush;
            }

            textGrid.PutChar(ch, foregroundBrush, backgroundBrush);
        }

        public override void PutText(string text, bool forceFixedWidthFont)
        {
            Brush foregroundBrush, backgroundBrush;
            if (reverse)
            {
                foregroundBrush = BackgroundBrush;
                backgroundBrush = ForegroundBrush;
            }
            else
            {
                foregroundBrush = ForegroundBrush;
                backgroundBrush = BackgroundBrush;
            }

            foreach (char ch in text)
            {
                textGrid.PutChar(ch, foregroundBrush, backgroundBrush);
            }
        }

        public override int GetCursorColumn() => textGrid.CursorColumn;

        public override int GetCursorLine() => textGrid.CursorLine;

        public override void SetCursorAsync(int line, int column) => textGrid.SetCursor(line, column);

        public override int GetHeight()
        {
            int rowIndex = GetRow(this);
            return (int)(ParentWindow.RowDefinitions[rowIndex].Height.Value / RowHeight);
        }

        public override void SetHeight(int lines)
        {
            int rowIndex = GetRow(this);
            ParentWindow.RowDefinitions[rowIndex].Height = new GridLength(lines * RowHeight, GridUnitType.Pixel);
            textGrid.SetHeight(lines);
        }

        public override int RowHeight => (int)fontCharSize.Height;

        public override int ColumnWidth => (int)fontCharSize.Width;
    }
}
