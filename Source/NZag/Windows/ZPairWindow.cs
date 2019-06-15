using NZag.Services;
using System.Windows;
using System.Windows.Controls;

namespace NZag.Windows
{
    internal class ZPairWindow : ZWindow
    {
        public ZPairWindow(
            ZWindowManager manager,
            FontAndColorService fontAndColorService,
            ZWindow child1,
            ZWindow child2,
            ZWindowPosition child2Position,
            GridLength child2Size)
            : base(manager, fontAndColorService)
        {
            Child1 = child1;
            Child2 = child2;

            switch (child2Position)
            {
                case ZWindowPosition.Left:
                    ColumnDefinitions.Add(new ColumnDefinition { Width = child2Size });
                    ColumnDefinitions.Add(new ColumnDefinition());
                    SetColumn(Child1, 1);
                    SetColumn(Child2, 0);
                    break;
                case ZWindowPosition.Right:
                    ColumnDefinitions.Add(new ColumnDefinition());
                    ColumnDefinitions.Add(new ColumnDefinition { Width = child2Size });
                    SetColumn(Child1, 0);
                    SetColumn(Child2, 1);
                    break;
                case ZWindowPosition.Above:
                    RowDefinitions.Add(new RowDefinition { Height = child2Size });
                    RowDefinitions.Add(new RowDefinition());
                    SetRow(Child1, 1);
                    SetRow(Child2, 0);
                    break;
                case ZWindowPosition.Below:
                    RowDefinitions.Add(new RowDefinition());
                    RowDefinitions.Add(new RowDefinition { Height = child2Size });
                    SetRow(Child1, 0);
                    SetRow(Child2, 1);
                    break;
            }

            child1.SetParentWindow(this);
            child2.SetParentWindow(this);

            Children.Add(child1);
            Children.Add(child2);
        }

        public void Replace(ZWindow child, ZWindow newChild)
        {
            if (Child1.Equals(child))
            {
                Child1 = newChild;
                Child1.SetParentWindow(null);
                Children[0] = newChild;
                newChild.SetParentWindow(this);
            }
            else if (Child2.Equals(child))
            {
                Child2 = newChild;
                Child2.SetParentWindow(null);
                Children[0] = newChild;
                newChild.SetParentWindow(this);
            }
        }

        public ZWindow Child1 { get; private set; }

        public ZWindow Child2 { get; private set; }
    }
}
