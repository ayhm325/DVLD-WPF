using Microsoft.Extensions.DependencyInjection; // تأكد من استيراد هذا
using Presentation.Views;
using Presentation.Views.Pages.Applications;
using System.Windows;

namespace DVLD_WPF
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.WindowState = WindowState.Maximized;

            // استدعاء الصفحة من الـ ServiceProvider
            MainFrame.Navigate(App.ServiceProvider.GetRequiredService<HomePage>());
        }

        private void Home_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(App.ServiceProvider.GetRequiredService<HomePage>());
        }

        private void Users_Click(object sender, RoutedEventArgs e)
        {           
            MainFrame.Navigate(App.ServiceProvider.GetRequiredService<UserPage>());
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(App.ServiceProvider.GetRequiredService<SettingsPage>());
        }

        private void ManagePeople_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(App.ServiceProvider.GetRequiredService<PeoplePage>());
        }

        private void ApplicationType_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(App.ServiceProvider.GetRequiredService<ManageApplicationTypePage>());

        }
    }
}