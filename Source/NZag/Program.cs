using NZag.ViewModels;
using System;
using System.ComponentModel.Composition.Hosting;
using System.Reflection;
using System.Windows;

namespace NZag
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            var catalog = new AssemblyCatalog(Assembly.GetExecutingAssembly());
            using var container = new CompositionContainer(catalog);

            var mainWindowViewModel = container.GetExportedValue<MainWindowViewModel>();
            var mainWindow = mainWindowViewModel.CreateView();

            var app = new Application();
            app.Run(mainWindow);
        }
    }
}
