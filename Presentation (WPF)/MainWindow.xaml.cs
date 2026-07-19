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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace DVLD_WPF
{
    public partial class MainWindow : Window
    {
        public static INavigationService Navigation { get; private set; } = null!;
        private readonly ICurrentUserService _currentUserService;
        private readonly IServiceProvider _serviceProvider;
        private readonly IDashboardService _dashboardService;

        // ═══════ متغيرات نظام الجسيمات ═══════
        private bool _isParticlesRunning = false;
        private readonly Random _random = new Random();

        // ═══════ متغيرات تأثير الكاتبة ═══════
        private DispatcherTimer? _typewriterTimer;
        private Storyboard? _cursorBlinkStoryboard;
        private int _typewriterIndex = 0;
        private const string TypewriterFullText =
            "Navigate through the menu above to manage licenses, applications, drivers, and system users.";

        // ═══════ متغيرات تحكم عامة ═══════
        private bool _isFirstLoad = true;

        public MainWindow(ICurrentUserService currentUserService, IServiceProvider serviceProvider, IDashboardService dashboardService)
        {
            InitializeComponent();
            _currentUserService = currentUserService;
            _serviceProvider = serviceProvider;
            this.WindowState = WindowState.Maximized;

            Navigation = new NavigationService(MainFrame);

            this.Loaded += MainWindow_Loaded;
            this.SizeChanged += MainWindow_SizeChanged;
            _dashboardService = dashboardService;
        }

        // ═══════════════════════════════════════════════════════
        //                  أحداث النافذة
        // ═══════════════════════════════════════════════════════

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            StartWelcomeAnimations();
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_isParticlesRunning && EmptyStatePlaceholder.Visibility == Visibility.Visible)
            {
                StopParticleAnimation();
                StartParticleAnimation();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _typewriterTimer?.Stop();
            StopParticleAnimation();
        }

        // ═══════════════════════════════════════════════════════
        //            1. نظام الجسيمات المتحركة (Particles)
        // ═══════════════════════════════════════════════════════

        private void StartParticleAnimation()
        {
            if (ParticleCanvas.ActualWidth <= 0 || ParticleCanvas.ActualHeight <= 0)
            {
                Dispatcher.BeginInvoke(new Action(StartParticleAnimation), DispatcherPriority.Loaded);
                return;
            }

            _isParticlesRunning = true;

            for (int i = 0; i < 40; i++)
            {
                CreateParticle();
            }
        }

        private void CreateParticle()
        {
            if (!_isParticlesRunning) return;

            double canvasW = ParticleCanvas.ActualWidth;
            double canvasH = ParticleCanvas.ActualHeight;
            if (canvasW <= 0 || canvasH <= 0) return;

            var particle = new Ellipse
            {
                Width = _random.Next(2, 6),
                Height = _random.Next(2, 6),
                IsHitTestVisible = false
            };

            // ألوان متنوعة بدرجات الأزرق/البنفسجي مع شفافية عالية
            byte alpha = (byte)_random.Next(15, 55);
            byte r = (byte)_random.Next(60, 110);
            byte g = (byte)_random.Next(50, 90);
            byte b = (byte)_random.Next(190, 245);
            particle.Fill = new SolidColorBrush(Color.FromArgb(alpha, r, g, b));
            particle.Opacity = _random.NextDouble() * 0.35 + 0.1;

            double startX = _random.NextDouble() * canvasW;
            double startY = _random.NextDouble() * canvasH;
            double endX = _random.NextDouble() * canvasW;
            double endY = _random.NextDouble() * canvasH;

            Canvas.SetLeft(particle, startX);
            Canvas.SetTop(particle, startY);
            ParticleCanvas.Children.Add(particle);

            // حركة أفقية
            var animX = new DoubleAnimation
            {
                From = startX,
                To = endX,
                Duration = TimeSpan.FromSeconds(_random.Next(10, 28)),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            };

            // حركة عمودية
            var animY = new DoubleAnimation
            {
                From = startY,
                To = endY,
                Duration = TimeSpan.FromSeconds(_random.Next(13, 32)),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            };

            particle.BeginAnimation(Canvas.LeftProperty, animX);
            particle.BeginAnimation(Canvas.TopProperty, animY);
        }

        private void StopParticleAnimation()
        {
            _isParticlesRunning = false;
            ParticleCanvas.Children.Clear();
        }

        // ═══════════════════════════════════════════════════════
        //            2. تأثير الكاتبة (Typewriter Effect)
        // ═══════════════════════════════════════════════════════

        private void StartTypewriterEffect()
        {
            _typewriterIndex = 0;
            TypewriterText.Text = "";
            TypewriterCursor.Opacity = 1;

            // تشغيل وميض المؤشر
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

                // إخفاء المؤشر بعد ثانيتين
                Dispatcher.BeginInvoke(new Action(async () =>
                {
                    await System.Threading.Tasks.Task.Delay(2000);
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

        // ═══════════════════════════════════════════════════════
        //      3 & 5. الإحصائيات مع حركة العد التنازلي للأرقام
        // ═══════════════════════════════════════════════════════

        private async void LoadStatisticsAsync()
        {
            var stats = await _dashboardService.GetStatisticsAsync();


            await Task.Delay(_isFirstLoad ? 800 : 400);

            _ = AnimateNumberAsync(
                StatTotalPeople,
                stats.TotalPeople);


            _ = AnimateNumberAsync(
                StatTotalDrivers,
                stats.TotalDrivers);


            _ = AnimateNumberAsync(
                StatActiveLicenses,
                stats.ActiveLicenses);


            _ = AnimateNumberAsync(
                StatPendingApps,
                stats.PendingApplications);


            _ = AnimateNumberAsync(
                StatLocalDLApps,
                stats.LocalDrivingLicenseApplications);


            _ = AnimateNumberAsync(
                StatInternationalLicenses,
                stats.InternationalLicenses);


            _ = AnimateNumberAsync(
                StatDetainedLicenses,
                stats.DetainedLicenses);


            _ = AnimateNumberAsync(
                StatUpcomingTests,
                stats.UpcomingTests);
        }

        private async System.Threading.Tasks.Task AnimateNumberAsync(
            TextBlock textBlock, int targetValue, int durationMs = 1500)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            while (stopwatch.ElapsedMilliseconds < durationMs)
            {
                // إيقاف إذا تم إخفاء الشاشة
                if (EmptyStatePlaceholder.Visibility != Visibility.Visible)
                    return;

                double progress = (double)stopwatch.ElapsedMilliseconds / durationMs;
                // Ease Out Cubic لتسريع ثم تبطيء
                double eased = 1 - Math.Pow(1 - progress, 3);
                int currentValue = (int)(targetValue * eased);

                textBlock.Text = currentValue.ToString("N0");

                await System.Threading.Tasks.Task.Delay(16); // ~60 FPS
            }

            textBlock.Text = targetValue.ToString("N0");
        }

        // ═══════════════════════════════════════════════════════
        //          6. زر Get Started + تحكم عام
        // ═══════════════════════════════════════════════════════

        private void GetStarted_Click(object sender, RoutedEventArgs e)
        {
            HideWelcomeScreen();
            MainFrame.Navigate(App.ServiceProvider.GetRequiredService<LDLAppPage>());
        }

        // ═══════════════════════════════════════════════════════
        //               وظائف مساعدة للتحكم
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// بدء جميع حركات شاشة الترحيب
        /// </summary>
        private void StartWelcomeAnimations()
        {
            // حركة الظهور التدريجي
            var fadeAnim = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(_isFirstLoad ? 1.0 : 0.5));
            fadeAnim.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };

            var slideAnim = new DoubleAnimation
            {
                From = _isFirstLoad ? 35 : 15,
                To = 0,
                Duration = TimeSpan.FromSeconds(_isFirstLoad ? 1.0 : 0.5),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            MainContent.RenderTransform = new TranslateTransform();
            MainContent.BeginAnimation(OpacityProperty, fadeAnim);
            MainContent.RenderTransform.BeginAnimation(TranslateTransform.YProperty, slideAnim);

            // تشغيل الأنظمة
            StartParticleAnimation();
            StartTypewriterEffect();
            LoadStatisticsAsync();
        }

        private void ShowWelcomeScreen()
        {
            EmptyStatePlaceholder.Visibility = Visibility.Visible;
            MainFrame.Content = null;
            StartWelcomeAnimations();
        }

        private void HideWelcomeScreen()
        {
            EmptyStatePlaceholder.Visibility = Visibility.Collapsed;
            StopParticleAnimation();
            StopTypewriterEffect();
        }

        // ═══════════════════════════════════════════════════════
        //          أحداث فتح الصفحات (Pages)
        // ═══════════════════════════════════════════════════════

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

        // ═══════════════════════════════════════════════════════
        //          أحداث فتح النوافذ (Windows)
        // ═══════════════════════════════════════════════════════

        private void NewLocalLicnnse_Click(object sender, RoutedEventArgs e)
        {
            ShowWelcomeScreen();
            var window = App.ServiceProvider.GetRequiredService<NewLocalLicnnse>();
            window.Owner = System.Windows.Application.Current.MainWindow;
            window.ShowDialog();
        }

        private void InternationalLicense_Click(object sender, RoutedEventArgs e)
        {
            ShowWelcomeScreen();
            var window = App.ServiceProvider.GetRequiredService<NewInternationalLicenseApplicationWin>();
            window.Owner = System.Windows.Application.Current.MainWindow;
            window.ShowDialog();
        }

        private async void CurrentUser_Click(object sender, RoutedEventArgs e)
        {
            ShowWelcomeScreen();
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
            ShowWelcomeScreen();
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
            ShowWelcomeScreen();
            var window = App.ServiceProvider.GetRequiredService<RenewLicenseApplicationWin>();
            window.Owner = System.Windows.Application.Current.MainWindow;
            window.ShowDialog();
        }

        private void ReplacementForLostOrDamaged_Click(object sender, RoutedEventArgs e)
        {
            ShowWelcomeScreen();
            var window = App.ServiceProvider.GetRequiredService<ReplacementDamagedLicense>();
            window.Owner = System.Windows.Application.Current.MainWindow;
            window.ShowDialog();
        }

        private void DetainLicense_Click(object sender, RoutedEventArgs e)
        {
            ShowWelcomeScreen();
            var window = App.ServiceProvider.GetRequiredService<DetainLicenseWin>();
            window.Owner = System.Windows.Application.Current.MainWindow;
            window.ShowDialog();
        }

        private void ReleaseDetainedLicense_Click(object sender, RoutedEventArgs e)
        {
            ShowWelcomeScreen();
            var window = App.ServiceProvider.GetRequiredService<ReleaseDetainedLicenseWin>();
            window.Owner = System.Windows.Application.Current.MainWindow;
            window.ShowDialog();
        }

        private void ReleaseDetainedDrivingLicense_Click(object sender, RoutedEventArgs e)
        {
            ShowWelcomeScreen();
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