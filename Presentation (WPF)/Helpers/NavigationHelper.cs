using DVLD_WPF;
using System.Windows;
using System.Windows.Controls;

namespace Presentation.Helpers
{
    public static class NavigationHelper
    {
        public static void Navigate(Page page)
        {
            var main = System.Windows.Application.Current.MainWindow as MainWindow;
            main?.MainFrame.Navigate(page);
        }
    }
}