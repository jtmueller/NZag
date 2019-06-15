using Microsoft.Win32;
using NZag.Services;
using SimpleMVVM;
using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NZag.ViewModels
{
    [Export]
    public class MainWindowViewModel : ViewModelBase<Window>
    {
        private readonly GameService _gameService;
        private readonly ScreenViewModel _screenViewModel;
        private readonly ProfilerViewModel _profilerViewModel;
        private bool _profilingEnabled;

        [ImportingConstructor]
        private MainWindowViewModel(
            GameService gameService,
            ScreenViewModel screenViewModel,
            ProfilerViewModel profilerViewModel)
            : base("Views/MainWindowView")
        {
            _gameService = gameService;
            _screenViewModel = screenViewModel;
            _profilerViewModel = profilerViewModel;

            _gameService.GameOpened += OnGameOpened;
            _gameService.ScriptLoaded += OnScriptLoaded;
        }

        public string Title => _gameService.IsGameOpen
                    ? "NZag - " + Path.GetFileName(_gameService.GameFileName)
                    : "NZag";

        public string GameName => _gameService.IsGameOpen
                    ? Path.GetFileName(_gameService.GameFileName)
                    : "None";

        public string ScriptName => _gameService.IsScriptOpen
                    ? Path.GetFileName(_gameService.ScriptFileName)
                    : "None";

        protected override void OnViewCreated(Window view)
        {
            var screenContent = view.FindName<Grid>("ScreenContent");
            screenContent.Children.Add(_screenViewModel.CreateView());

            var profilerContent = view.FindName<Grid>("ProfilerContent");
            profilerContent.Children.Add(_profilerViewModel.CreateView());

            OpenGameCommand = RegisterCommand("Open", "Open", OpenGameExecuted, CanOpenGameExecute, new KeyGesture(Key.O, ModifierKeys.Control));
            LoadScriptCommand = RegisterCommand("Load Script...", "LoadScript", LoadScriptExecuted, CanLoadScriptExecute);
            ProfileCommand = RegisterCommand<bool>("Profile", "Profile", ProfileExecuted, CanProfileExecute);
            PlayGameCommand = RegisterCommand("Play", "Play", PlayGameExecuted, CanPlayGameExecute, new KeyGesture(Key.F5));
            ResetGameCommand = RegisterCommand("Reset", "Reset", ResetGameExecuted, CanResetGameExecute);
        }

        private void OnGameOpened(object sender, EventArgs e)
        {
            PropertyChanged("Title");
            PropertyChanged("GameName");
        }

        private void OnScriptLoaded(object sender, EventArgs e) => PropertyChanged("ScriptName");

        private void StartGame()
        {
            if (_profilingEnabled)
            {
                _gameService.StartGame(_screenViewModel, _profilerViewModel);
            }
            else
            {
                _gameService.StartGame(_screenViewModel);
            }
        }

        private bool CanOpenGameExecute() => true;

        private void OpenGameExecuted()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Open Z-Machine File",
                Filter = "Story Files (*.z*)|*.z*"
            };

            if (dialog.ShowDialog() == true)
            {
                if (_gameService.IsGameOpen)
                {
                    _gameService.CloseGame();
                }

                _gameService.OpenGame(dialog.FileName);
            }
        }

        private bool CanLoadScriptExecute() => true;

        private void LoadScriptExecuted()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Load Script File",
                Filter = "Script Files (*.script)|*.script"
            };

            if (dialog.ShowDialog() == true)
            {
                _gameService.LoadScript(dialog.FileName);
            }
        }

        private bool CanProfileExecute(bool enabled) => true;

        private void ProfileExecuted(bool enabled) => _profilingEnabled = enabled;

        private bool CanPlayGameExecute() => _gameService.IsGameOpen;

        private void PlayGameExecuted() => StartGame();

        private bool CanResetGameExecute() => _gameService.IsGameOpen;

        private void ResetGameExecuted()
        {
            string gameFileName = _gameService.GameFileName;
            string scriptFileName = _gameService.ScriptFileName;

            _gameService.CloseGame();

            _gameService.OpenGame(gameFileName);

            if (!String.IsNullOrWhiteSpace(scriptFileName))
            {
                _gameService.LoadScript(scriptFileName);
            }

            StartGame();
        }

        public ICommand OpenGameCommand { get; private set; }
        public ICommand LoadScriptCommand { get; private set; }
        public ICommand ProfileCommand { get; private set; }
        public ICommand PlayGameCommand { get; private set; }
        public ICommand ResetGameCommand { get; private set; }
    }
}
