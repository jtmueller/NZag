using System;
using System.Text;
using System.Threading.Tasks;

namespace NZag.Core.Tests.Mocks
{
    internal class MockScreen : IScreen
    {
        private readonly StringBuilder _builder;
        private readonly string[] _script;
        private int _scriptIndex;

        public MockScreen(string script = null)
        {
            _builder = new StringBuilder();
            _script = script?.Split("\r\n", StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
        }

        public Task<char> ReadCharAsync() => Task.FromResult((char)0);

        public Task<string> ReadTextAsync(int maxChars)
        {
            if (_scriptIndex >= _script.Length)
                return Task.FromResult(string.Empty);
            string command = _script[_scriptIndex++];
            _builder.Append(command);
            _builder.Append('\n');
            return Task.FromResult(command);
        }

        public Task WriteCharAsync(char ch)
        {
            _builder.Append(ch);
            return Task.CompletedTask;
        }

        public Task WriteTextAsync(string s)
        {
            _builder.Append(s);
            return Task.CompletedTask;
        }

        public Task ClearAsync(int window) => Task.CompletedTask;

        public Task ClearAllAsync(bool unsplit) => Task.CompletedTask;

        public Task SplitAsync(int lines) => Task.CompletedTask;

        public Task UnsplitAsync() => Task.CompletedTask;

        public Task SetWindowAsync(int window) => Task.CompletedTask;

        public Task ShowStatusAsync() => Task.CompletedTask;

        public Task<int> GetCursorColumnAsync() => Task.FromResult(0);

        public Task<int> GetCursorLineAsync() => Task.FromResult(0);

        public Task SetCursorAsync(int line, int column) => Task.CompletedTask;

        public Task SetTextStyleAsync(ZTextStyle style) => Task.CompletedTask;

        public Task SetBackgroundColorAsync(ZColor color) => Task.CompletedTask;

        public Task SetForegroundColorAsync(ZColor color) => Task.CompletedTask;

        public byte FontHeightInUnits => 0;

        public byte FontWidthInUnits => 0;

        public byte ScreenHeightInLines => 0;

        public ushort ScreenHeightInUnits => 0;

        public byte ScreenWidthInColumns => 0;

        public ushort ScreenWidthInUnits => 0;

        public string Output => _builder.ToString();
    }
}
