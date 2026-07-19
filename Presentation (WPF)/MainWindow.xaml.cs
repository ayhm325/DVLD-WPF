using Microsoft.Extensions.DependencyInjection;
using Presentation;
using Presentation.Services;
using Presentation.ViewModels;
using Presentation.Views;
using Presentation.Views.Pages;
using Presentation.Views.Pages.Applications;
using Presentation.Views.Pages.Tests;
using Presentation.Views.Windows;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace DVLD_WPF
{
    public partial class MainWindow : Window
    {
        public static INavigationService Navigation { get; private set; } = null!;
        private readonly ICurrentUserService _currentUserService;
        private readonly IServiceProvider _serviceProvider;
        private readonly IDashboardService _dashboardService;

        // ═══════ متغيرات تأثير الكاتبة ═══════
        private DispatcherTimer? _typewriterTimer;
        private Storyboard? _cursorBlinkStoryboard;
        private int _typewriterIndex = 0;
        private const string TypewriterFullText =
            "Welcome back. Use the sidebar to navigate, or choose a quick action below to get started.";

        // ═══════ متغيرات الساعة ═══════
        private DispatcherTimer? _clockTimer;

        // ═══════ متغيرات الـ Sidebar ═══════
        private readonly List<Border> _allNavItems;
        private Border? _activeNavItem;

        // ═══════ متغيرات تحكم عامة ═══════
        private bool _isFirstLoad = true;

        public MainWindow(
            ICurrentUserService currentUserService,
            IServiceProvider serviceProvider,
            IDashboardService dashboardService)
        {
            InitializeComponent();

            _currentUserService = currentUserService;
            _serviceProvider = serviceProvider;
            _dashboardService = dashboardService;

            WindowState = WindowState.Maximized;
            Navigation = new NavigationService(MainFrame);

            // جمع كل عناصر التنقل
            _allNavItems = new List<Border>
            {
                NavDashboard,
                NavPeople,
                NavDrivers,
                NavNewLocal,
                NavNewInternational,
                NavRenew,
                NavReplace,
                NavReleaseDetained,
                NavLocalApps,
                NavIntlApps,
                NavDetained,
                NavDetainLicense,
                NavRetakeTest,
                NavUsers,
                NavAppTypes,
                NavTestTypes,
                NavMyProfile,
                NavChangePassword,
                NavSignOut
            };
            _activeNavItem = NavDashboard;

            this.Loaded += MainWindow_Loaded;
        }

        // ═══════════════════════════════════════════════════════════
        //                     أحداث النافذة
        // ═══════════════════════════════════════════════════════════

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // تشغيل الساعة
            _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _clockTimer.Tick += UpdateClock;
            _clockTimer.Start();
            UpdateClock(null, null);

            // بدء حركات الداشبورد
            StartDashboardAnimations();
            StartTypewriterEffect();
            LoadStatisticsAsync();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _clockTimer?.Stop();
            _typewriterTimer?.Stop();
        }

        // ═══════════════════════════════════════════════════════════
        //              Title Bar — سحب + أزرار النافذة
        // ═══════════════════════════════════════════════════════════

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void MinBtn_Click(object sender, MouseButtonEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaxBtn_Click(object sender, MouseButtonEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }

        private void CloseBtn_Click(object sender, MouseButtonEventArgs e)
        {
            Close();
        }

        // ═══════════════════════════════════════════════════════════
        //                         الساعة
        // ═══════════════════════════════════════════════════════════

        private void UpdateClock(object? sender, EventArgs? e)
        {
            ClockText.Text = DateTime.Now.ToString("hh:mm tt");
            DateText.Text = DateTime.Now.ToString("ddd, MMM dd");
        }

        // ═══════════════════════════════════════════════════════════
        //            Sidebar — تحديد العنصر النشط
        // ═══════════════════════════════════════════════════════════

        private void SetActiveNav(Border item)
        {
            foreach (var nav in _allNavItems)
            {
                if (nav == NavSignOut)
                    continue;

                nav.Style = (Style)FindResource("NavItemStyle");
            }

            if (item != NavSignOut)
                item.Style = (Style)FindResource("NavItemActiveStyle");

            _activeNavItem = item;
        }

        // ═══════════════════════════════════════════════════════════
        //            Sidebar — التنقل إلى صفحات (Pages)
        // ═══════════════════════════════════════════════════════════

        private void NavigateToPage(string title, string subtitle, Border navItem, Page page)
        {
            DashboardPanel.Visibility = Visibility.Collapsed;
            StopTypewriterEffect();

            MainFrame.Visibility = Visibility.Visible;
            MainFrame.Navigate(page);

            SetActiveNav(navItem);
            HeaderTitle.Text = title;
            HeaderSubtitle.Text = subtitle;
        }

        // ═══════════════════════════════════════════════════════════
        //            Sidebar — فتح نوافذ منبثقة (Windows)
        // ═══════════════════════════════════════════════════════════

        private void OpenWindow(Window window)
        {
            window.Owner = System.Windows.Application.Current.MainWindow;
            window.ShowDialog();

            // تحديث الإحصائيات عند العودة إذا الداشبورد ظاهر
            if (DashboardPanel.Visibility == Visibility.Visible)
                LoadStatisticsAsync();
        }

        // ═══════════════════════════════════════════════════════════
        //            Sidebar — العودة للداشبورد
        // ═══════════════════════════════════════════════════════════

        private void ShowDashboard()
        {
            DashboardPanel.Visibility = Visibility.Visible;
            MainFrame.Visibility = Visibility.Collapsed;
            MainFrame.Content = null;

            SetActiveNav(NavDashboard);
            HeaderTitle.Text = "Dashboard";
            HeaderSubtitle.Text = "Overview of your driving license system";

            _isFirstLoad = false;
            StartDashboardAnimations();
            StartTypewriterEffect();
            LoadStatisticsAsync();
        }

        // ═══════════════════════════════════════════════════════════
        //         Sidebar Click Handlers — الصفحات (Pages)
        // ═══════════════════════════════════════════════════════════

        private void NavDashboard_Click(object sender, MouseButtonEventArgs e)
        {
            ShowDashboard();
        }

        private void NavPeople_Click(object sender, MouseButtonEventArgs e)
        {
            NavigateToPage(
                "Manage People",
                "View and manage all registered people",
                NavPeople,
                App.ServiceProvider.GetRequiredService<PeoplePage>());
        }

        private void NavDrivers_Click(object sender, MouseButtonEventArgs e)
        {
            NavigateToPage(
                "Manage Drivers",
                "View and manage all licensed drivers",
                NavDrivers,
                App.ServiceProvider.GetRequiredService<DriversPage>());
        }

        private void NavLocalApps_Click(object sender, MouseButtonEventArgs e)
        {
            NavigateToPage(
                "Local Applications",
                "Manage local driving license applications",
                NavLocalApps,
                App.ServiceProvider.GetRequiredService<LDLAppPage>());
        }

        private void NavIntlApps_Click(object sender, MouseButtonEventArgs e)
        {
            NavigateToPage(
                "International Applications",
                "Manage international license applications",
                NavIntlApps,
                App.ServiceProvider.GetRequiredService<InterLAppPage>());
        }

        private void NavDetained_Click(object sender, MouseButtonEventArgs e)
        {
            NavigateToPage(
                "Detained Licenses",
                "View and manage all detained licenses",
                NavDetained,
                App.ServiceProvider.GetRequiredService<ListDetainedLicenses>());
        }

        private void NavRetakeTest_Click(object sender, MouseButtonEventArgs e)
        {
            NavigateToPage(
                "Retake Test",
                "Schedule a test retake for an applicant",
                NavRetakeTest,
                App.ServiceProvider.GetRequiredService<LDLAppPage>());
        }

        private void NavUsers_Click(object sender, MouseButtonEventArgs e)
        {
            NavigateToPage(
                "Users",
                "Manage system users and permissions",
                NavUsers,
                App.ServiceProvider.GetRequiredService<UserPage>());
        }

        private void NavAppTypes_Click(object sender, MouseButtonEventArgs e)
        {
            NavigateToPage(
                "Application Types",
                "Configure application type settings",
                NavAppTypes,
                App.ServiceProvider.GetRequiredService<ManageApplicationTypePage>());
        }

        private void NavTestTypes_Click(object sender, MouseButtonEventArgs e)
        {
            NavigateToPage(
                "Test Types",
                "Configure test type settings",
                NavTestTypes,
                App.ServiceProvider.GetRequiredService<ManageTestTypePage>());
        }

        // ═══════════════════════════════════════════════════════════
        //        Sidebar Click Handlers — النوافذ (Windows)
        // ═══════════════════════════════════════════════════════════

        private void NavNewLocal_Click(object sender, MouseButtonEventArgs e)
        {
            OpenWindow(App.ServiceProvider.GetRequiredService<NewLocalLicnnse>());
        }

        private void NavNewInternational_Click(object sender, MouseButtonEventArgs e)
        {
            OpenWindow(App.ServiceProvider.GetRequiredService<NewInternationalLicenseApplicationWin>());
        }

        private void NavRenew_Click(object sender, MouseButtonEventArgs e)
        {
            OpenWindow(App.ServiceProvider.GetRequiredService<RenewLicenseApplicationWin>());
        }

        private void NavReplace_Click(object sender, MouseButtonEventArgs e)
        {
            OpenWindow(App.ServiceProvider.GetRequiredService<ReplacementDamagedLicense>());
        }

        private void NavReleaseDetained_Click(object sender, MouseButtonEventArgs e)
        {
            OpenWindow(App.ServiceProvider.GetRequiredService<ReleaseDetainedLicenseWin>());
        }

        private void NavDetainLicense_Click(object sender, MouseButtonEventArgs e)
        {
            OpenWindow(App.ServiceProvider.GetRequiredService<DetainLicenseWin>());
        }

        private async void NavMyProfile_Click(object sender, MouseButtonEventArgs e)
        {
            var userDetailsVm = App.ServiceProvider.GetRequiredService<AddEditUserViewModel>();
            await userDetailsVm.InitializeAsync(_currentUserService.UserId);

            var window = App.ServiceProvider.GetRequiredService<UserDetailsWindow>();
            window.DataContext = userDetailsVm;
            OpenWindow(window);
        }

        private void NavChangePassword_Click(object sender, MouseButtonEventArgs e)
        {
            var vm = App.ServiceProvider.GetRequiredService<ChangePasswordViewModel>();
            vm.UserId = _currentUserService.UserId;
            vm.UserName = _currentUserService.Username;

            var window = new ChangePasswordWindow(vm)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };
            OpenWindow(window);
        }

        // ═══════════════════════════════════════════════════════════
        //                    Sign Out
        // ═══════════════════════════════════════════════════════════

        private void NavSignOut_Click(object sender, MouseButtonEventArgs e)
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

        // ═══════════════════════════════════════════════════════════
        //       حركات الداشبورد (Staggered Entrance Animation)
        // ═══════════════════════════════════════════════════════════

        private void StartDashboardAnimations()
        {
            // إعادة تعيين الحالة قبل الحركة
            StatsRow1.Opacity = 0;
            StatsRow1RT.Y = 24;

            StatsRow2.Opacity = 0;
            StatsRow2RT.Y = 24;

            QuickActionsSection.Opacity = 0;
            QuickActionsRT.Y = 24;

            RecentActivitiesSection.Opacity = 0;
            RecentActivitiesRT.Y = 24;

            // تشغيل الـ Storyboard المتدرج
            var stagger = (Storyboard)FindResource("StaggerEnterStoryboard");
            stagger.Begin(this);

            // تشغيل توهج النبض
            var glow = (Storyboard)FindResource("PulseGlowStoryboard");
            glow.Begin(this);
        }

        // ═══════════════════════════════════════════════════════════
        //            تأثير الكاتبة (Typewriter Effect)
        // ═══════════════════════════════════════════════════════════

        private void StartTypewriterEffect()
        {
            _typewriterIndex = 0;
            TypewriterText.Text = "";
            TypewriterCursor.Opacity = 1;

            _cursorBlinkStoryboard = (Storyboard)FindResource("CursorBlinkStoryboard");
            _cursorBlinkStoryboard.Begin(TypewriterCursor, true);

            _typewriterTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(25) };
            _typewriterTimer.Tick += TypewriterTimer_Tick;
            _typewriterTimer.Start();
        }

        private void TypewriterTimer_Tick(object? sender, EventArgs e)
        {
            if (_typewriterIndex < TypewriterFullText.Length)
            {
                TypewriterText.Text += TypewriterFullText[_typewriterIndex];
                _typewriterIndex++;
            }
            else
            {
                _typewriterTimer?.Stop();

                Dispatcher.BeginInvoke(new Action(async () =>
                {
                    await Task.Delay(2000);
                    if (TypewriterCursor != null)
                    {
                        TypewriterCursor.BeginAnimation(OpacityProperty,
                            new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.5)));
                    }
                }), DispatcherPriority.Background);
            }
        }

        private void StopTypewriterEffect()
        {
            _typewriterTimer?.Stop();
            _cursorBlinkStoryboard?.Stop(TypewriterCursor);
            TypewriterText.Text = TypewriterFullText;
            TypewriterCursor.Opacity = 0;
        }

        // ═══════════════════════════════════════════════════════════
        //         الإحصائيات مع حركة العد التصاعدي للأرقام
        // ═══════════════════════════════════════════════════════════

        private async void LoadStatisticsAsync()
        {
            var stats = await _dashboardService.GetStatisticsAsync();

            await Task.Delay(_isFirstLoad ? 800 : 400);

            _ = AnimateNumberAsync(StatTotalPeople, stats.TotalPeople);
            _ = AnimateNumberAsync(StatTotalDrivers, stats.TotalDrivers);
            _ = AnimateNumberAsync(StatActiveLicenses, stats.ActiveLicenses);
            _ = AnimateNumberAsync(StatPendingApps, stats.PendingApplications);
            _ = AnimateNumberAsync(StatLocalDLApps, stats.LocalDrivingLicenseApplications);
            _ = AnimateNumberAsync(StatInternationalLicenses, stats.InternationalLicenses);
            _ = AnimateNumberAsync(StatDetainedLicenses, stats.DetainedLicenses);
            _ = AnimateNumberAsync(StatUpcomingTests, stats.UpcomingTests);
        }

        private async Task AnimateNumberAsync(
            TextBlock textBlock, int targetValue, int durationMs = 1500)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            while (stopwatch.ElapsedMilliseconds < durationMs)
            {
                if (DashboardPanel.Visibility != Visibility.Visible)
                    return;

                double progress = (double)stopwatch.ElapsedMilliseconds / durationMs;
                double eased = 1 - Math.Pow(1 - progress, 3);
                int currentValue = (int)(targetValue * eased);

                textBlock.Text = currentValue.ToString("N0");

                await Task.Delay(16);
            }

            textBlock.Text = targetValue.ToString("N0");
        }
    }
}