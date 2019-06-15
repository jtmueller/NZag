using NZag.Core;
using NZag.Services;
using NZag.Windows;
using SimpleMVVM;
using SimpleMVVM.Threading;
using System;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace NZag.ViewModels
{
    [Export]
    public class ScreenViewModel : ViewModelBase<UserControl>, IScreen
    {
        private readonly ForegroundThreadAffinitizedObject _foregroundThreadAffinitedObject;

        private readonly GameService _gameService;
        private readonly FontAndColorService _fontAndColorService;
        private readonly ZWindowManager _windowManager;

        private Grid _windowContainer;
        private ZWindow _mainWindow;
        private ZWindow _upperWindow;

        private int _currentStatusHeight;
        private int _machineStatusHeight;

        [ImportingConstructor]
        private ScreenViewModel(GameService gameService, FontAndColorService fontAndColorService)
            : base("Views/ScreenView")
        {
            _foregroundThreadAffinitedObject = new ForegroundThreadAffinitizedObject();

            _gameService = gameService;
            _fontAndColorService = fontAndColorService;
            _windowManager = new ZWindowManager(fontAndColorService);

            _gameService.GameOpened += OnGameOpened;
            _gameService.GameClosing += OnGameClosing;
        }

        protected override void OnViewCreated(UserControl view) => _windowContainer = view.FindName<Grid>("WindowContainer");

        private void OnGameOpened(object sender, EventArgs e)
        {
            _mainWindow = _windowManager.OpenWindow(ZWindowKind.TextBuffer);
            _windowContainer.Children.Add(_mainWindow);
            _upperWindow = _windowManager.OpenWindow(ZWindowKind.TextGrid, _mainWindow, ZWindowPosition.Above);

            _windowManager.ActivateWindow(_mainWindow);
        }

        private void OnGameClosing(object sender, EventArgs e)
        {
            _mainWindow = null;
            _upperWindow = null;
            _windowManager.CloseWindow(_windowManager.RootWindow);
        }

        private void ResetStatusHeight()
        {
            _foregroundThreadAffinitedObject.AssertIsForeground();

            if (_upperWindow != null)
            {
                int height = _upperWindow.GetHeight();
                if (_machineStatusHeight != height)
                {
                    _upperWindow.SetHeight(_machineStatusHeight);
                }
            }
        }

        public Task<char> ReadCharAsync()
        {
            return _foregroundThreadAffinitedObject.InvokeBelowInputPriority(async () =>
            {
                char ch = await _windowManager.ActiveWindow.ReadCharAsync();

                ResetStatusHeight();
                _currentStatusHeight = 0;

                return ch;
            }).Unwrap();
        }

        public Task<string> ReadTextAsync(int maxChars)
        {
            return _foregroundThreadAffinitedObject.InvokeBelowInputPriority(async () =>
            {
                string command;

                if (_gameService.HasNextScriptCommand)
                {
                    command = _gameService.GetNextScriptCommand();
                    bool forceFixedWidthFont = _gameService.Machine.ForceFixedWidthFont();
                    _windowManager.ActiveWindow.PutText(command + "\r\n", forceFixedWidthFont);
                }
                else
                {
                    command = await _windowManager.ActiveWindow.ReadTextAsync(maxChars);
                }

                ResetStatusHeight();
                _currentStatusHeight = 0;

                return command;
            }).Unwrap();
        }

        public Task WriteCharAsync(char value)
        {
            return _foregroundThreadAffinitedObject.InvokeBelowInputPriority(() =>
            {
                bool forceFixedWidthFont = _gameService.Machine.ForceFixedWidthFont();
                _windowManager.ActiveWindow.PutChar(value, forceFixedWidthFont);
            });
        }

        public Task WriteTextAsync(string value)
        {
            return _foregroundThreadAffinitedObject.InvokeBelowInputPriority(() =>
            {
                bool forceFixedWidthFont = _gameService.Machine.ForceFixedWidthFont();
                _windowManager.ActiveWindow.PutText(value, forceFixedWidthFont);
            });
        }

        public Task ClearAsync(int window)
        {
            return _foregroundThreadAffinitedObject.InvokeBelowInputPriority(() =>
            {
                if (window == 0)
                {
                    _mainWindow.Clear();
                }
                else if (window == 1 && _upperWindow != null)
                {
                    _upperWindow.Clear();

                    ResetStatusHeight();

                    _currentStatusHeight = 0;
                }
            });
        }

        public Task ClearAllAsync(bool unsplit)
        {
            return _foregroundThreadAffinitedObject.InvokeBelowInputPriority(async () =>
            {
                _mainWindow.Clear();

                if (_upperWindow != null)
                {
                    if (unsplit)
                    {
                        await UnsplitAsync();
                    }
                    else
                    {
                        _upperWindow.Clear();
                    }
                }
            }).Unwrap();
        }

        public Task SplitAsync(int lines)
        {
            return _foregroundThreadAffinitedObject.InvokeBelowInputPriority(() =>
            {
                if (_upperWindow == null)
                {
                    return;
                }

                if (lines == 0 || lines > _currentStatusHeight)
                {
                    int height = _upperWindow.GetHeight();
                    if (lines != height)
                    {
                        _upperWindow.SetHeight(lines);
                        _currentStatusHeight = lines;
                    }
                }

                _machineStatusHeight = lines;

                if (_gameService.Machine.Memory.Version == 0)
                {
                    _upperWindow.Clear();
                }
            });
        }

        public Task UnsplitAsync()
        {
            return _foregroundThreadAffinitedObject.InvokeBelowInputPriority(() =>
            {
                if (_upperWindow == null)
                {
                    return;
                }

                _upperWindow.SetHeight(0);
                _upperWindow.Clear();
                ResetStatusHeight();
                _currentStatusHeight = 0;
            });
        }

        public Task SetWindowAsync(int window)
        {
            return _foregroundThreadAffinitedObject.InvokeBelowInputPriority(() =>
            {
                if (window == 0)
                {
                    _mainWindow.Activate();
                }
                else if (window == 1)
                {
                    _upperWindow.Activate();
                }
            });
        }

        public Task ShowStatusAsync()
        {
            return _foregroundThreadAffinitedObject.InvokeBelowInputPriority(() =>
            {
                if (_gameService.Machine.Memory.Version > 3)
                {
                    return;
                }

                if (_upperWindow == null)
                {
                    _upperWindow = _windowManager.OpenWindow(
                        ZWindowKind.TextGrid,
                        _mainWindow,
                        ZWindowPosition.Above,
                        ZWindowSizeKind.Fixed,
                        size: 1);
                }
                else
                {
                    int height = _upperWindow.GetHeight();
                    if (height != 1)
                    {
                        _upperWindow.SetHeight(1);
                        _machineStatusHeight = 1;
                    }
                }

                _upperWindow.Clear();

                byte charWidth = ScreenWidthInColumns;
                ushort locationObject = _gameService.Machine.ReadGlobalVariable(0);
                string locationText = " " + _gameService.Machine.ReadObjectShortName(locationObject);

                _upperWindow.SetReverse(true);

                if (charWidth < 5)
                {
                    _upperWindow.PutText(new string(' ', charWidth), forceFixedWidthFont: false);
                    return;
                }

                if (locationText.Length > charWidth)
                {
                    locationText = locationText.Substring(0, charWidth - 3) + "...";
                    _upperWindow.PutText(locationText, forceFixedWidthFont: false);
                    return;
                }

                _upperWindow.PutText(locationText, forceFixedWidthFont: false);

                string rightText;
                if (_gameService.Machine.IsScoreGame())
                {
                    int score = (short)_gameService.Machine.ReadGlobalVariable(1);
                    int moves = _gameService.Machine.ReadGlobalVariable(2);
                    rightText = String.Format("Score: {0,-8} Moves: {1,-6} ", score, moves);
                }
                else
                {
                    int hours = _gameService.Machine.ReadGlobalVariable(1);
                    int minutes = _gameService.Machine.ReadGlobalVariable(2);
                    bool pm = (hours / 12) > 0;
                    if (pm)
                    {
                        hours %= 12;
                    }

                    rightText = String.Format("{0}:{1:n2} {2}", hours, minutes, (pm ? "pm" : "am"));
                }

                if (rightText.Length < charWidth - locationText.Length - 1)
                {
                    _upperWindow.PutText(
                        new string(' ', charWidth - locationText.Length - rightText.Length),
                        forceFixedWidthFont: false);

                    _upperWindow.PutText(rightText, forceFixedWidthFont: false);
                }
            });
        }

        public Task<int> GetCursorColumnAsync()
        {
            return _foregroundThreadAffinitedObject.InvokeBelowInputPriority(() =>
                _windowManager.ActiveWindow.GetCursorColumn());
        }

        public Task<int> GetCursorLineAsync()
        {
            return _foregroundThreadAffinitedObject.InvokeBelowInputPriority(() =>
                _windowManager.ActiveWindow.GetCursorLine());
        }

        public Task SetCursorAsync(int line, int column)
        {
            return _foregroundThreadAffinitedObject.InvokeBelowInputPriority(() =>
            {
                _windowManager.ActiveWindow.SetCursorAsync(line, column);
            });
        }

        public Task SetTextStyleAsync(ZTextStyle style)
        {
            return _foregroundThreadAffinitedObject.InvokeBelowInputPriority(() =>
            {
                var window = _windowManager.ActiveWindow;

                switch (style)
                {
                    case ZTextStyle.Roman:
                        window.SetBold(false);
                        window.SetItalic(false);
                        window.SetFixedPitch(false);
                        window.SetReverse(false);
                        break;
                    case ZTextStyle.Bold:
                        window.SetBold(true);
                        break;
                    case ZTextStyle.Italic:
                        window.SetItalic(true);
                        break;
                    case ZTextStyle.FixedPitch:
                        window.SetFixedPitch(true);
                        break;
                    case ZTextStyle.Reverse:
                        window.SetReverse(true);
                        break;
                }
            });
        }

        public Task SetForegroundColorAsync(ZColor color)
        {
            return _foregroundThreadAffinitedObject.InvokeBelowInputPriority(() =>
                _fontAndColorService.SetForegroundColor(color));
        }

        public Task SetBackgroundColorAsync(ZColor color)
        {
            return _foregroundThreadAffinitedObject.InvokeBelowInputPriority(() =>
                _fontAndColorService.SetBackgroundColor(color));
        }

        private FormattedText GetFixedFontMeasureText()
        {
            return new FormattedText(
                textToFormat: "0",
                culture: CultureInfo.InstalledUICulture,
                flowDirection: FlowDirection.LeftToRight,
                typeface: new Typeface(new FontFamily("Consolas"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal),
                emSize: _fontAndColorService.FontSize,
                foreground: Brushes.Black,
                pixelsPerDip: 1.0);
        }

        public byte FontHeightInUnits => (byte)GetFixedFontMeasureText().Height;

        public byte FontWidthInUnits => (byte)GetFixedFontMeasureText().Width;

        public byte ScreenHeightInLines => (byte)(_windowContainer.ActualHeight / GetFixedFontMeasureText().Height);

        public ushort ScreenHeightInUnits => (ushort)_windowContainer.ActualHeight;

        public byte ScreenWidthInColumns => (byte)(_windowContainer.ActualWidth / GetFixedFontMeasureText().Width);

        public ushort ScreenWidthInUnits => (ushort)_windowContainer.ActualWidth;
    }
}
