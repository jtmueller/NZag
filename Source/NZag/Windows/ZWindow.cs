using NZag.Services;
using NZag.Utilities;
using SimpleMVVM.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;

namespace NZag.Windows
{
    internal abstract class ZWindow : Grid
    {
        protected readonly ZWindowManager Manager;
        private readonly FontAndColorService fontAndColorService;
        private readonly ForegroundThreadAffinitizedObject foregroundThreadAffinitizedObject;

        private ZPairWindow parentWindow;

        protected ZWindow(ZWindowManager manager, FontAndColorService fontAndColorService)
        {
            Manager = manager;
            this.fontAndColorService = fontAndColorService;
            foregroundThreadAffinitizedObject = new ForegroundThreadAffinitizedObject();

            UseLayoutRounding = true;
            SnapsToDevicePixels = true;

            TextOptions.SetTextFormattingMode(this, TextFormattingMode.Display);
            TextOptions.SetTextRenderingMode(this, TextRenderingMode.Auto);
        }

        protected void AssertIsForeground() => foregroundThreadAffinitizedObject.AssertIsForeground();

        public ZPairWindow ParentWindow => parentWindow;

        protected Brush ForegroundBrush => fontAndColorService.ForegroundBrush;

        protected Brush BackgroundBrush => fontAndColorService.BackgroundBrush;

        protected double FontSize => fontAndColorService.FontSize;

        public void SetParentWindow(ZPairWindow newParentWindow) => parentWindow = newParentWindow;

        public void Activate() => Manager.ActivateWindow(this);

        public virtual bool SetBold(bool value) => false;

        public virtual bool SetItalic(bool value) => false;

        public virtual bool SetFixedPitch(bool value) => false;

        public virtual bool SetReverse(bool value) => false;

        public virtual void Clear() => throw new Exceptions.RuntimeException("Window does not support clear operation.");

        protected virtual Task<char> ReadCharCoreAsync() => throw new Exceptions.RuntimeException("Window does not support user input.");

        public Task<char> ReadCharAsync()
        {
            return foregroundThreadAffinitizedObject.InvokeBelowInputPriority(() =>
            {
                return ReadCharCoreAsync();
            }).Unwrap();
        }

        protected virtual Task<string> ReadTextCoreAsync(int maxChars) => throw new Exceptions.RuntimeException("Window does not support user input.");

        public Task<string> ReadTextAsync(int maxChars)
        {
            return foregroundThreadAffinitizedObject.InvokeBelowInputPriority(() =>
            {
                return ReadTextCoreAsync(maxChars);
            }).Unwrap();
        }

        public virtual void PutChar(char ch, bool forceFixedWidthFont) => throw new Exceptions.RuntimeException("Window does not support text display.");

        public virtual void PutText(string text, bool forceFixedWidthFont) => throw new Exceptions.RuntimeException("Window does not support text display.");

        public virtual int GetHeight() => 0;

        public virtual void SetHeight(int lines)
        {
            // Do nothing in base implementation.
        }

        public virtual int GetCursorColumn() => 0;

        public virtual int GetCursorLine() => 0;

        public virtual void SetCursorAsync(int line, int column)
        {
            // Do nothing in base implementation.
        }

        public virtual int RowHeight => 0;

        public virtual int ColumnWidth => 0;
    }
}
