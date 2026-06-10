using Microsoft.Extensions.DependencyInjection;
using Presentation.Services;
using Presentation.Views;
using Presentation.Views.Pages;
using Presentation.Views.Pages.Applications;
using Presentation.Views.Pages.Tests;
using Presentation.Views.Windows;
using System.Windows;
using System.Windows.Controls;

namespace DVLD_WPF
{
    public partial class MainWindow : Window
    {       
        public static INavigationService Navigation { get; private set; } = null!;

        public MainWindow()
        {
            InitializeComponent();
            this.WindowState = WindowState.Maximized;
          

            Navigation = new NavigationService(MainFrame);

            // استدعاء الصفحة من الـ ServiceProvider
            MainFrame.Navigate(App.ServiceProvider.GetRequiredService<HomePage>());
        }

        public void NavigateTo<TPage>() where TPage : Page
        {
            var page = App.ServiceProvider.GetRequiredService<TPage>();
            MainFrame.Navigate(page);
        }

        private void Home_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(App.ServiceProvider.GetRequiredService<HomePage>());
        }

        private void Users_Click(object sender, RoutedEventArgs e)
        {           
            MainFrame.Navigate(App.ServiceProvider.GetRequiredService<UserPage>());
        }

        private void NewLocalLicnnse_Click(object sender, RoutedEventArgs e)
        {
            // الحصول على النافذة من الـ ServiceProvider
            var newLicenseWindow = App.ServiceProvider.GetRequiredService<NewLocalLicnnse>();
            // إظهارها كنافذة مستقلة
            newLicenseWindow.Owner = System.Windows.Application.Current.MainWindow; // لربطها بالنافذة الرئيسية
            newLicenseWindow.ShowDialog();
        }

        private void ManagePeople_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(App.ServiceProvider.GetRequiredService<PeoplePage>());
        }

        private void ApplicationType_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(App.ServiceProvider.GetRequiredService<ManageApplicationTypePage>());

        }

        private void TestType_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(App.ServiceProvider.GetRequiredService<ManageTestTypePage>());
        }

        private void LDLApp_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(App.ServiceProvider.GetRequiredService<LDLAppPage>());
        }

        
        private void LAppDetails_Click(object sender, RoutedEventArgs e)
        {
            // الحصول على النافذة من الـ ServiceProvider
            var newLicenseWindow = App.ServiceProvider.GetRequiredService<LocalApplicationDetailsWin>();
            // إظهارها كنافذة مستقلة
            newLicenseWindow.Owner = System.Windows.Application.Current.MainWindow; // لربطها بالنافذة الرئيسية
            newLicenseWindow.ShowDialog();
        }

    }
}