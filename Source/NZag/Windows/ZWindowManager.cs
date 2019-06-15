using NZag.Services;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace NZag.Windows
{
    internal class ZWindowManager
    {
        private readonly FontAndColorService _fontAndColorService;

        public ZWindowManager(FontAndColorService fontAndColorService)
        {
            _fontAndColorService = fontAndColorService;
        }

        public void ActivateWindow(ZWindow window) => ActiveWindow = window;

        public ZWindow OpenWindow(
            ZWindowKind kind,
            ZWindow splitWindow = null,
            ZWindowPosition position = ZWindowPosition.Left,
            ZWindowSizeKind sizeKind = ZWindowSizeKind.Fixed,
            int size = 0)
        {
            if (kind == ZWindowKind.Pair)
            {
                throw new InvalidOperationException("ZPairWindows can't be creatted directly");
            }

            if (RootWindow == null && splitWindow != null)
            {
                throw new InvalidOperationException("Cannot open a split window if the root window has not yet been created.");
            }

            if (RootWindow != null && splitWindow == null)
            {
                throw new InvalidOperationException("Cannot open a new root window if the root window has already bee created.");
            }

            var newWindow = CreateNewWindow(kind);

            if (RootWindow == null)
            {
                RootWindow = newWindow;
            }
            else
            {
                var splitSize = sizeKind switch
                {
                    ZWindowSizeKind.Fixed => 
                        new GridLength(IsVertical(position)
                            ? size * newWindow.RowHeight
                            : size * newWindow.ColumnWidth, GridUnitType.Pixel),
                    ZWindowSizeKind.Proportional => new GridLength(size / 100.0, GridUnitType.Star),
                    _ => throw new InvalidOperationException("Invalid size kind: " + sizeKind.ToString())
                };

                Debug.Assert(splitWindow != null, "splitWindow != null");

                var parentGrid = (Grid)splitWindow.Parent;
                parentGrid.Children.Remove(splitWindow);

                var newParentWindow = new ZPairWindow(this, _fontAndColorService, splitWindow, newWindow, position, splitSize);

                if (splitWindow.ParentWindow is ZPairWindow oldParentWindow)
                {
                    oldParentWindow.Replace(splitWindow, newParentWindow);
                }
                else
                {
                    RootWindow = newParentWindow;
                }

                parentGrid.Children.Add(newParentWindow);
            }

            return newWindow;
        }

        public void CloseWindow(ZWindow window)
        {
            var parentGrid = (Grid)window.Parent;
            parentGrid.Children.Remove(window);

            var parent = window.ParentWindow;
            if (parent == null) // root window
            {
                RootWindow = null;
            }
            else
            {
                var sibling = parent.Child1 == window
                    ? parent.Child2
                    : parent.Child1;

                parentGrid.Children.Remove(sibling);

                var grandParentGrid = (Grid)parent.Parent;
                grandParentGrid.Children.Remove(parent);

                var grandParent = parent.ParentWindow;
                if (grandParent == null) // root window
                {
                    RootWindow = sibling;
                    sibling.SetParentWindow(null);
                }
                else
                {
                    grandParent.Replace(parent, sibling);
                }

                grandParent.Children.Add(sibling);
            }
        }

        private bool IsVertical(ZWindowPosition position)
        {
            switch (position)
            {
                case ZWindowPosition.Above:
                case ZWindowPosition.Below:
                    return true;
                case ZWindowPosition.Left:
                case ZWindowPosition.Right:
                    return false;
                default:
                    throw new InvalidOperationException("Invalid window position: " + position.ToString());
            }
        }

        private ZWindow CreateNewWindow(ZWindowKind kind)
        {
            switch (kind)
            {
                case ZWindowKind.Blank:
                    return new ZBlankWindow(this, _fontAndColorService);
                case ZWindowKind.TextBuffer:
                    return new ZTextBufferWindow(this, _fontAndColorService);
                case ZWindowKind.TextGrid:
                    return new ZTextGridWindow(this, _fontAndColorService);
                default:
                    throw new InvalidOperationException("Invalid window kind: " + kind.ToString());
            }
        }

        public ZWindow RootWindow { get; private set; }

        public ZWindow ActiveWindow { get; private set; }
    }
}
