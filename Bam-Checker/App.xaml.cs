using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace BamChecker
{
    public partial class App : Application
    {
    }

    // global interfaces
    public interface ICustomTitleBar
    {
        void MinimizeWindow(object sender, RoutedEventArgs e);
        void MaximizeRestoreWindow(object sender, RoutedEventArgs e);
        void CloseWindow(object sender, RoutedEventArgs e);
        void DragWindow(object sender, MouseButtonEventArgs e);
        void ToggleWindowState();
    }
}
