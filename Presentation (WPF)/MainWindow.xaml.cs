using Microsoft.Extensions.DependencyInjection;
using Presentation;
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
        private readonly ICurrentUserService _currentUserService;
        private readonly IServiceProvider _serviceProvider;

        public MainWindow(ICurrentUserService currentUserService, IServiceProvider serviceProvider)
        {
            InitializeComponent();
            _currentUserService = currentUserService;
            _serviceProvider = serviceProvider;
            this.WindowState = WindowState.Maximized;
            MainFrame.Navigate(App.ServiceProvider.GetRequiredService<HomePage>());

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

        private async void CurrentUser_Click(object sender, RoutedEventArgs e)
        {
            var userDetailsVm = App.ServiceProvider
        .GetRequiredService<AddEditUserViewModel>();

            var currentUser = App.ServiceProvider
                .GetRequiredService<ICurrentUserService>();

            await userDetailsVm.InitializeAsync(currentUser.UserId);

            var window = App.ServiceProvider
                .GetRequiredService<UserDetailsWindow>();

            window.Owner = System.Windows.Application.Current.MainWindow;
            window.DataContext = userDetailsVm;

            window.ShowDialog();
        }

        private void ChangePassword_Click(object sender, RoutedEventArgs e)
        {
            var vm = App.ServiceProvider
                .GetRequiredService<ChangePasswordViewModel>();

            vm.UserId = _currentUserService.UserId;
            vm.UserName = _currentUserService.Username;

            var window = new ChangePasswordWindow(vm)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };

            window.ShowDialog();
        }

        private void SignOut_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to sign out?",
                "Sign Out",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;
          
            _currentUserService.Clear();
          
            var loginWindow = App.ServiceProvider
                .GetRequiredService<LoginWindow>();

            loginWindow.Show();
           
            Close();
        }

        private void LocalDrivingLicenseApplications_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(App.ServiceProvider.GetRequiredService<LDLAppPage>());
            
        }

        private void InternationalLicenseApplications_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(App.ServiceProvider.GetRequiredService<InterLAppPage>());            
        }

        private void RenewDrivingLicense_Click(object sender, RoutedEventArgs e)
        {
            var window =
                App.ServiceProvider
                .GetRequiredService<RenewLicenseApplicationWin>();

            window.Owner = System.Windows.Application.Current.MainWindow;

            window.ShowDialog();
        }

    }
}