using Microsoft.Extensions.DependencyInjection;
using Presentation.Services;
using Presentation.ViewModels;
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
            var newLicenseWindow = App.ServiceProvider.GetRequiredService<NewLocalLicnnse>();           
            newLicenseWindow.Owner = System.Windows.Application.Current.MainWindow; 
            newLicenseWindow.ShowDialog();
        }

        private void InternationalLicense_Click(object sender, RoutedEventArgs e)
        {
            var newLicenseWindow = App.ServiceProvider.GetRequiredService<NewInternationalLicenseApplicationWin>();
            newLicenseWindow.Owner = System.Windows.Application.Current.MainWindow;
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

        private void Drivers_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(App.ServiceProvider.GetRequiredService<DriversPage>());
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }


    }
}