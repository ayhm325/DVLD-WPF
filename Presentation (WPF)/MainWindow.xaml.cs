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

            Navigation = new NavigationService(MainFrame);
        }

        // ═══════ وظائف مساعدة لتنظيم الكود ═══════

        // إظهار شاشة الترحيب وتفريغ الصفحة السابقة
        private void ShowWelcomeScreen()
        {
            EmptyStatePlaceholder.Visibility = Visibility.Visible;
            MainFrame.Content = null; // تفريغ الـ Frame لإخفاء الصفحة تماماً
        }

        // إخفاء شاشة الترحيب
        private void HideWelcomeScreen()
        {
            EmptyStatePlaceholder.Visibility = Visibility.Collapsed;
        }

        public void NavigateTo<TPage>() where TPage : Page
        {
            var page = App.ServiceProvider.GetRequiredService<TPage>();
            HideWelcomeScreen();
            MainFrame.Navigate(page);
        }

        // ═══════ أحداث فتح الصفحات (Pages) ═══════

        private void Users_Click(object sender, RoutedEventArgs e)
        {
            HideWelcomeScreen();
            MainFrame.Navigate(App.ServiceProvider.GetRequiredService<UserPage>());
        }

        private void ManagePeople_Click(object sender, RoutedEventArgs e)
        {
            HideWelcomeScreen();
            MainFrame.Navigate(App.ServiceProvider.GetRequiredService<PeoplePage>());
        }

        private void ApplicationType_Click(object sender, RoutedEventArgs e)
        {
            HideWelcomeScreen();
            MainFrame.Navigate(App.ServiceProvider.GetRequiredService<ManageApplicationTypePage>());
        }

        private void TestType_Click(object sender, RoutedEventArgs e)
        {
            HideWelcomeScreen();
            MainFrame.Navigate(App.ServiceProvider.GetRequiredService<ManageTestTypePage>());
        }

        private void Drivers_Click(object sender, RoutedEventArgs e)
        {
            HideWelcomeScreen();
            MainFrame.Navigate(App.ServiceProvider.GetRequiredService<DriversPage>());
        }

        private void LocalDrivingLicenseApplications_Click(object sender, RoutedEventArgs e)
        {
            HideWelcomeScreen();
            MainFrame.Navigate(App.ServiceProvider.GetRequiredService<LDLAppPage>());
        }

        private void InternationalLicenseApplications_Click(object sender, RoutedEventArgs e)
        {
            HideWelcomeScreen();
            MainFrame.Navigate(App.ServiceProvider.GetRequiredService<InterLAppPage>());
        }

        private void ManageDetainedLicenses_Click(object sender, RoutedEventArgs e)
        {
            HideWelcomeScreen();
            MainFrame.Navigate(App.ServiceProvider.GetRequiredService<ListDetainedLicenses>());
        }

        private void RetakeTest_Click(object sender, RoutedEventArgs e)
        {
            HideWelcomeScreen();
            MainFrame.Navigate(App.ServiceProvider.GetRequiredService<LDLAppPage>());
        }

        // ═══════ أحداث فتح النوافذ (Windows) ═══════

        private void NewLocalLicnnse_Click(object sender, RoutedEventArgs e)
        {
            ShowWelcomeScreen(); // إظهار شاشة الترحيب قبل فتح النافذة
            var newLicenseWindow = App.ServiceProvider.GetRequiredService<NewLocalLicnnse>();
            newLicenseWindow.Owner = System.Windows.Application.Current.MainWindow;
            newLicenseWindow.ShowDialog();
        }

        private void InternationalLicense_Click(object sender, RoutedEventArgs e)
        {
            ShowWelcomeScreen(); // إظهار شاشة الترحيب قبل فتح النافذة
            var newLicenseWindow = App.ServiceProvider.GetRequiredService<NewInternationalLicenseApplicationWin>();
            newLicenseWindow.Owner = System.Windows.Application.Current.MainWindow;
            newLicenseWindow.ShowDialog();
        }

        private async void CurrentUser_Click(object sender, RoutedEventArgs e)
        {
            ShowWelcomeScreen(); // إظهار شاشة الترحيب قبل فتح النافذة
            var userDetailsVm = App.ServiceProvider.GetRequiredService<AddEditUserViewModel>();
            var currentUser = App.ServiceProvider.GetRequiredService<ICurrentUserService>();

            await userDetailsVm.InitializeAsync(currentUser.UserId);

            var window = App.ServiceProvider.GetRequiredService<UserDetailsWindow>();
            window.Owner = System.Windows.Application.Current.MainWindow;
            window.DataContext = userDetailsVm;
            window.ShowDialog();
        }

        private void ChangePassword_Click(object sender, RoutedEventArgs e)
        {
            ShowWelcomeScreen(); // إظهار شاشة الترحيب قبل فتح النافذة
            var vm = App.ServiceProvider.GetRequiredService<ChangePasswordViewModel>();
            vm.UserId = _currentUserService.UserId;
            vm.UserName = _currentUserService.Username;

            var window = new ChangePasswordWindow(vm)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };
            window.ShowDialog();
        }

        private void RenewDrivingLicense_Click(object sender, RoutedEventArgs e)
        {
            ShowWelcomeScreen(); // إظهار شاشة الترحيب قبل فتح النافذة
            var window = App.ServiceProvider.GetRequiredService<RenewLicenseApplicationWin>();
            window.Owner = System.Windows.Application.Current.MainWindow;
            window.ShowDialog();
        }

        private void ReplacementForLostOrDamaged_Click(object sender, RoutedEventArgs e)
        {
            ShowWelcomeScreen(); // إظهار شاشة الترحيب قبل فتح النافذة
            var window = App.ServiceProvider.GetRequiredService<ReplacementDamagedLicense>();
            window.Owner = System.Windows.Application.Current.MainWindow;
            window.ShowDialog();
        }

        private void DetainLicense_Click(object sender, RoutedEventArgs e)
        {
            ShowWelcomeScreen(); // إظهار شاشة الترحيب قبل فتح النافذة
            var window = App.ServiceProvider.GetRequiredService<DetainLicenseWin>();
            window.Owner = System.Windows.Application.Current.MainWindow;
            window.ShowDialog();
        }

        private void ReleaseDetainedLicense_Click(object sender, RoutedEventArgs e)
        {
            ShowWelcomeScreen(); // إظهار شاشة الترحيب قبل فتح النافذة
            var window = App.ServiceProvider.GetRequiredService<ReleaseDetainedLicenseWin>();
            window.Owner = System.Windows.Application.Current.MainWindow;
            window.ShowDialog();
        }

        private void ReleaseDetainedDrivingLicense_Click(object sender, RoutedEventArgs e)
        {
            ShowWelcomeScreen(); // إظهار شاشة الترحيب قبل فتح النافذة
            var window = App.ServiceProvider.GetRequiredService<ReleaseDetainedLicenseWin>();
            window.Owner = System.Windows.Application.Current.MainWindow;
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

            var loginWindow = App.ServiceProvider.GetRequiredService<LoginWindow>();
            loginWindow.Show();
            Close();
        }
    }
}