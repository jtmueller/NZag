using System.Windows;
using System.Windows.Controls;
using NZag.Services;

namespace NZag.Windows
{
    internal class ZPairWindow : ZWindow
    {
        private ZWindow _child1;
        private ZWindow _child2;

        public ZPairWindow(
            ZWindowManager manager,
            FontAndColorService fontAndColorService,
            ZWindow child1,
            ZWindow child2,
            ZWindowPosition child2Position,
            GridLength child2Size)
            : base(manager, fontAndColorService)
        {
            _child1 = child1;
            _child2 = child2;

            switch (child2Position)
            {
                case ZWindowPosition.Left:
                    ColumnDefinitions.Add(new ColumnDefinition { Width = child2Size });
                    ColumnDefinitions.Add(new ColumnDefinition());
                    SetColumn(_child1, 1);
                    SetColumn(_child2, 0);
                    break;
                case ZWindowPosition.Right:
                    ColumnDefinitions.Add(new ColumnDefinition());
                    ColumnDefinitions.Add(new ColumnDefinition { Width = child2Size });
                    SetColumn(_child1, 0);
                    SetColumn(_child2, 1);
                    break;
                case ZWindowPosition.Above:
                    RowDefinitions.Add(new RowDefinition { Height = child2Size });
                    RowDefinitions.Add(new RowDefinition());
                    SetRow(_child1, 1);
                    SetRow(_child2, 0);
                    break;
                case ZWindowPosition.Below:
                    RowDefinitions.Add(new RowDefinition());
                    RowDefinitions.Add(new RowDefinition { Height = child2Size });
                    SetRow(_child1, 0);
                    SetRow(_child2, 1);
                    break;
            }

            child1.SetParentWindow(this);
            child2.SetParentWindow(this);

            Children.Add(child1);
            Children.Add(child2);
        }

        public void Replace(ZWindow child, ZWindow newChild)
        {
            if (ReferenceEquals(this, newChild))
                return;

            if (_child1.Equals(child))
            {
                Children.Remove(_child1);
                _child1.SetParentWindow(null);
                _child1 = newChild;
                Children.Add(newChild);
                newChild.SetParentWindow(this);
            }
            else if (_child2.Equals(child))
            {
                Children.Remove(_child2);
                _child2.SetParentWindow(null);
                _child2 = newChild;
                Children.Add(newChild);
                newChild.SetParentWindow(this);
            }
        }

        public ZWindow Child1 => _child1;

        public ZWindow Child2 => _child2;
    }
}
