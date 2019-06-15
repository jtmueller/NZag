using NZag.Core;
using System;
using System.ComponentModel.Composition;
using System.IO;

namespace NZag.Services
{
    [Export]
    public class GameService
    {
        private string[] _script;
        private int _scriptIndex = -1;

        private void OnGameClosing() => GameClosing?.Invoke(this, EventArgs.Empty);

        private void OnGameOpened() => GameOpened?.Invoke(this, EventArgs.Empty);

        private void OnScriptLoaded() => ScriptLoaded?.Invoke(this, EventArgs.Empty);

        public void LoadScript(string fileName)
        {
            _script = File.ReadAllLines(fileName);
            _scriptIndex = 0;
            ScriptFileName = fileName;

            OnScriptLoaded();
        }

        public bool HasNextScriptCommand => _scriptIndex >= 0 && _scriptIndex < _script.Length;

        public string GetNextScriptCommand()
        {
            if (!HasNextScriptCommand)
            {
                return String.Empty;
            }

            string command = _script[_scriptIndex];
            _scriptIndex++;
            return command;
        }

        public void CloseGame()
        {
            OnGameClosing();

            GameFileName = null;
            Machine = null;
            _script = null;
        }

        public void OpenGame(string fileName)
        {
            using (var file = File.OpenRead(fileName))
            {
                var memory = Memory.CreateFrom(file);
                Machine = new Machine(memory, debugging: false);
            }

            GameFileName = fileName;

            OnGameOpened();
        }

        public void StartGame(IScreen screen, IProfiler profiler = null)
        {
            if (profiler != null)
            {
                Machine.RegisterProfiler(profiler);
            }

            Machine.RegisterScreen(screen);
            Machine.Randomize(42);
            Machine.RunAsync();
        }

        public bool IsGameOpen => Machine != null;

        public Machine Machine { get; private set; }

        public string GameFileName { get; private set; }

        public bool IsScriptOpen => _script != null;

        public string ScriptFileName { get; private set; }

        public event EventHandler GameClosing;
        public event EventHandler GameOpened;
        public event EventHandler ScriptLoaded;
    }
}
