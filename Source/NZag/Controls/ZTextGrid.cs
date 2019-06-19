using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace NZag.Controls
{
    public class ZTextGrid : FrameworkElement
    {
        private readonly VisualCollection _visuals;
        private readonly SortedList<(int, int), VisualPair> _visualPairs;

        private readonly double _fontSize;
        private readonly Size _fontCharSize;
        private Typeface _typeface;
        private bool _bold;
        private bool _italic;
        private bool _reverse;

        public ZTextGrid(double fontSize)
        {
            _visuals = new VisualCollection(this);
            _visualPairs = new SortedList<(int, int), VisualPair>();

            _fontSize = fontSize;

            var zero = new FormattedText(
                textToFormat: "0",
                culture: CultureInfo.InstalledUICulture,
                flowDirection: FlowDirection.LeftToRight,
                typeface: GetTypeface(),
                emSize: fontSize,
                foreground: Brushes.Black,
                pixelsPerDip: 1.0);

            _fontCharSize = new Size(zero.Width, zero.Height);
        }

        private Typeface GetTypeface()
        {
            if (_typeface == null)
            {
                var style = _italic ? FontStyles.Italic : FontStyles.Normal;
                var weight = _bold ? FontWeights.Bold : FontWeights.Normal;
                _typeface = new Typeface(new FontFamily("Consolas"), style, weight, stretch: FontStretches.Normal);
            }

            return _typeface;
        }

        public void Clear()
        {
            _visuals.Clear();
            _visualPairs.Clear();
            CursorColumn = 0;
            CursorLine = 0;
        }

        public void PutChar(char ch, Brush foregroundBrush, Brush backgroundBrush)
        {
            if (ch == '\n')
            {
                CursorLine += 1;
                CursorColumn = 0;
            }
            else
            {
                // First, see if we've already inserted something at this position.
                // If so, delete the old visuals.
                var cursorPos = (CursorColumn, CursorLine);
                if (_visualPairs.TryGetValue(cursorPos, out var visualPair))
                {
                    _visuals.Remove(visualPair.Background);
                    _visuals.Remove(visualPair.Character);
                    _visualPairs.Remove(cursorPos);
                }

                var backgroundVisual = new DrawingVisual();
                var backgroundContext = backgroundVisual.RenderOpen();

                double x = _fontCharSize.Width * CursorColumn;
                double y = _fontCharSize.Height * CursorLine;

                var backgroundRect = new Rect(
                    Math.Floor(x),
                    Math.Floor(y),
                    Math.Ceiling(_fontCharSize.Width + 0.5),
                    Math.Ceiling(_fontCharSize.Height));

                backgroundContext.DrawRectangle(backgroundBrush, null, backgroundRect);
                backgroundContext.Close();

                _visuals.Insert(0, backgroundVisual);

                var textVisual = new DrawingVisual();
                var textContext = textVisual.RenderOpen();

                textContext.DrawText(
                    new FormattedText(
                        ch.ToString(CultureInfo.InvariantCulture),
                        CultureInfo.InstalledUICulture,
                        FlowDirection.LeftToRight,
                        GetTypeface(),
                        _fontSize,
                        foregroundBrush,
                        new NumberSubstitution(NumberCultureSource.User, CultureInfo.InstalledUICulture, NumberSubstitutionMethod.AsCulture),
                        TextFormattingMode.Display, 1.0),
                    new Point(x, y));

                textContext.Close();

                _visuals.Add(textVisual);

                var newVisualPair = new VisualPair(backgroundVisual, textVisual);
                _visualPairs.Add(cursorPos, newVisualPair);

                CursorColumn += 1;
            }
        }

        public void SetBold(bool value)
        {
            _bold = value;
            _typeface = null;
        }

        public void SetItalic(bool value)
        {
            _italic = value;
            _typeface = null;
        }

        public void SetReverse(bool value) => _reverse = value;

        public int CursorColumn { get; private set; }

        public int CursorLine { get; private set; }

        public void SetCursor(int line, int column)
        {
            CursorLine = line;
            CursorColumn = column;
        }

        public void SetHeight(int lines)
        {
            for (int i = _visualPairs.Count - 1; i >= 0; i--)
            {
                var cursorPos = _visualPairs.Keys[i];
                var (_, y) = cursorPos;
                if (y > lines - 1)
                {
                    var visualPair = _visualPairs[cursorPos];
                    _visuals.Remove(visualPair.Background);
                    _visuals.Remove(visualPair.Character);
                    _visualPairs.Remove(cursorPos);
                }
            }
        }

        protected override Visual GetVisualChild(int index) => _visuals[index];

        protected override int VisualChildrenCount => _visuals.Count;
    }
}
