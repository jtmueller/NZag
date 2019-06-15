using NZag.Extensions;
using NZag.Services;
using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace NZag.Windows
{
    internal class ZTextBufferWindow : ZWindow
    {
        private readonly FontFamily _normalFontFamily;
        private readonly FontFamily _fixedFontFamily;
        private readonly Size _fontCharSize;

        private readonly FlowDocument _document;
        private readonly Paragraph _paragraph;
        private readonly FlowDocumentScrollViewer _scrollViewer;

        private bool _bold;
        private bool _italic;
        private bool _fixedPitch;
        private bool _reverse;

        public ZTextBufferWindow(ZWindowManager manager, FontAndColorService fontAndColorService)
            : base(manager, fontAndColorService)
        {
            _normalFontFamily = new FontFamily("Cambria");
            _fixedFontFamily = new FontFamily("Consolas");

            var zero = new FormattedText(
                textToFormat: "0",
                culture: CultureInfo.InstalledUICulture,
                flowDirection: FlowDirection.LeftToRight,
                typeface: new Typeface(_normalFontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal),
                emSize: FontSize,
                foreground: Brushes.Black,
                pixelsPerDip: 1.0);

            _fontCharSize = new Size(zero.Width, zero.Height);

            _document = new FlowDocument
            {
                FontFamily = _normalFontFamily,
                FontSize = FontSize,
                PagePadding = new Thickness(8.0)
            };

            _paragraph = new Paragraph();
            _document.Blocks.Add(_paragraph);

            _scrollViewer = new FlowDocumentScrollViewer
            {
                FocusVisualStyle = null,
                Document = _document
            };

            Children.Add(_scrollViewer);
        }

        private void ForceFixedWidthFontAsync(bool value, Action action)
        {
            bool oldValue = SetFixedPitch(value);
            action();
            SetFixedPitch(oldValue);
        }

        private Run CreateFormattedRun(string text)
        {
            var run = new Run(text);

            if (_bold)
            {
                run.FontWeight = FontWeights.Bold;
            }

            if (_italic)
            {
                run.FontStyle = FontStyles.Italic;
            }

            run.FontFamily = _fixedPitch
                ? _fixedFontFamily
                : _normalFontFamily;

            if (_reverse)
            {
                run.Background = ForegroundBrush;
                run.Foreground = BackgroundBrush;
            }
            else
            {
                run.Background = BackgroundBrush;
                run.Foreground = ForegroundBrush;
            }

            return run;
        }

        private void ScrollToEnd()
        {
            var scroller = _scrollViewer.FindFirstVisualChild<ScrollViewer>();
            if (scroller != null)
            {
                scroller.ScrollToEnd();
            }
        }

        private void ClearInlines() => _paragraph.Inlines.Clear();

        public override void Clear() => ClearInlines();

        protected override async Task<char> ReadCharCoreAsync()
        {
            AssertIsForeground();

            Keyboard.Focus(_scrollViewer);
            var args = await _scrollViewer.TextInputAsync();

            return args.Text[0];
        }

        protected override async Task<string> ReadTextCoreAsync(int maxChars)
        {
            AssertIsForeground();

            var inputTextBox = new TextBox
            {
                FontFamily = _normalFontFamily,
                FontSize = FontSize,
                Padding = new Thickness(0.0),
                Margin = new Thickness(0.0),
                BorderBrush = Brushes.Transparent,
                BorderThickness = new Thickness(0.0),
                Background = Brushes.WhiteSmoke,
                MaxLength = maxChars
            };

            var scrollContext = _scrollViewer.FindFirstVisualChild<ScrollContentPresenter>();
            var lastCharacterRect = _document.ContentEnd.GetCharacterRect(LogicalDirection.Forward);
            double minWidth = scrollContext.ActualHeight - _document.PagePadding.Right - lastCharacterRect.Right;
            inputTextBox.MinWidth = Math.Max(minWidth, 0);

            var container = new InlineUIContainer(inputTextBox, _document.ContentEnd)
            {
                BaselineAlignment = BaselineAlignment.TextBottom
            };

            _paragraph.Inlines.Add(container);

            if (!inputTextBox.Focus())
            {
                inputTextBox.PostAction(() => inputTextBox.Focus());
            }

            string text = null;
            while (text == null)
            {
                var args = await inputTextBox.KeyUpAsync();
                if (args.Key == Key.Return)
                {
                    text = inputTextBox.Text;
                }
            }

            _paragraph.Inlines.Remove(container);
            PutText(text + "\r\n", forceFixedWidthFont: false);

            return text;
        }

        public override void PutChar(char ch, bool forceFixedWidthFont)
        {
            ForceFixedWidthFontAsync(forceFixedWidthFont, () =>
            {
                var run = CreateFormattedRun(ch.ToString(CultureInfo.InvariantCulture));
                _paragraph.Inlines.Add(run);
                ScrollToEnd();
            });
        }

        public override void PutText(string text, bool forceFixedWidthFont)
        {
            ForceFixedWidthFontAsync(forceFixedWidthFont, () =>
            {
                var run = CreateFormattedRun(text);
                _paragraph.Inlines.Add(run);
                ScrollToEnd();
            });
        }

        public override bool SetBold(bool value)
        {
            bool oldValue = _bold;
            _bold = value;
            return oldValue;
        }

        public override bool SetItalic(bool value)
        {
            bool oldValue = _italic;
            _italic = value;
            return oldValue;
        }

        public override bool SetFixedPitch(bool value)
        {
            bool oldValue = _fixedPitch;
            _fixedPitch = value;
            return oldValue;
        }

        public override bool SetReverse(bool value)
        {
            bool oldValue = _reverse;
            _reverse = value;
            return oldValue;
        }

        public override int RowHeight => (int)_fontCharSize.Height;

        public override int ColumnWidth => (int)_fontCharSize.Width;
    }
}
