using Presentation.Views;
using System.Configuration;
using System.Windows;

namespace DVLD_WPF
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            this.WindowState = WindowState.Maximized;
            // أول صفحة عند التشغيل
            MainFrame.Navigate(new HomePage());
        }

        private void Home_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new HomePage());
        }

        private void Users_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new UserPage());
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new SettingsPage());
        }

        private void ManagePeople_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new PeoplePage());
        }
    }
}