using Microsoft.Extensions.DependencyInjection;
using Presentation;
using Presentation.Services;
using Presentation.Services.Api;
using Presentation.Services.UI;
using Presentation.ViewModels;
using Presentation.Views;
using Presentation.Views.Pages;
using Presentation.Views.Pages.Applications;
using Presentation.Views.Pages.Tests;
using Presentation.Views.Windows;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace DVLD_WPF;

public partial class MainWindow : Window
{
    public static INavigationService Navigation { get; private set; } = null!;

    private readonly ICurrentUserSession _currentUserSession;
    private readonly IServiceProvider _serviceProvider;
    private readonly DashboardViewModel _dashboardViewModel;
    private readonly IUserNotificationService _userNotifications;
    private readonly List<Border> _allNavItems;

    private Border? _activeNavItem;
    private DispatcherTimer? _typewriterTimer;
    private Storyboard? _cursorBlinkStoryboard;
    private DispatcherTimer? _clockTimer;
    private int _typewriterIndex;

    private const string TypewriterFullText =
        "Welcome back. Use the sidebar to navigate, or choose a quick action below to get started.";

    public string CurrentUserFullName => _currentUserSession.FullName;
    public string CurrentUserRole => _currentUserSession.Role;

    public MainWindow(
        ICurrentUserSession currentUserSession,
        IServiceProvider serviceProvider,
        DashboardViewModel dashboardViewModel,
        IUserNotificationService userNotifications)
    {
        InitializeComponent();

        _currentUserSession = currentUserSession ?? throw new ArgumentNullException(nameof(currentUserSession));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _dashboardViewModel = dashboardViewModel ?? throw new ArgumentNullException(nameof(dashboardViewModel));
        _userNotifications = userNotifications ?? throw new ArgumentNullException(nameof(userNotifications));

        DataContext = _dashboardViewModel;
        WindowState = WindowState.Maximized;
        Navigation = new NavigationService(MainFrame);

        _allNavItems =
        [
            NavDashboard, NavPeople, NavDrivers, NavNewLocal, NavNewInternational,
            NavRenew, NavReplace, NavReleaseDetained, NavLocalApps, NavIntlApps,
            NavDetained, NavDetainLicense, NavRetakeTest, NavUsers, NavAppTypes,
            NavTestTypes, NavMyProfile, NavChangePassword, NavSignOut
        ];

        _activeNavItem = NavDashboard;
        ApplyRoleVisibility();
        Loaded += MainWindow_Loaded;
    }

    private bool IsAdmin() =>
        string.Equals(_currentUserSession.Role, "Admin", StringComparison.OrdinalIgnoreCase);

    private bool IsStaff() =>
        string.Equals(_currentUserSession.Role, "Staff", StringComparison.OrdinalIgnoreCase);

    private static void SetVisibility(Visibility visibility, params UIElement[] elements)
    {
        foreach (var element in elements)
            element.Visibility = visibility;
    }

    private void ApplyRoleVisibility()
    {
        var isAdmin = IsAdmin();
        var isStaff = IsStaff();

        SetVisibility(
            isStaff ? Visibility.Visible : Visibility.Collapsed,
            NavPeople,
            NavNewLocal,
            NavNewInternational,
            NavRenew,
            NavReplace,
            NavDetainLicense,
            NavReleaseDetained,
            NavLocalApps,
            NavIntlApps,
            NavRetakeTest,
            NavDrivers,
            NavDetained,
            NavPeopleHeader,
            NavLicenseServicesHeader,
            NavApplicationsHeader,
            NavLicenseManagementHeader,
            QuickActionsSection);

        SetVisibility(
            isAdmin ? Visibility.Visible : Visibility.Collapsed,
            NavUsers,
            NavAppTypes,
            NavTestTypes,
            NavManagementHeader);
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clockTimer.Tick += UpdateClock;
        _clockTimer.Start();

        UpdateClock(null, null);
        StartDashboardAnimations();
        StartTypewriterEffect();

        await _dashboardViewModel.LoadAsync();
    }

    protected override void OnClosed(EventArgs e)
    {
        _clockTimer?.Stop();
        StopTypewriterEffect();
        base.OnClosed(e);
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }

    private void MinBtn_Click(object sender, MouseButtonEventArgs e) =>
        WindowState = WindowState.Minimized;

    private void MaxBtn_Click(object sender, MouseButtonEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void CloseBtn_Click(object sender, MouseButtonEventArgs e) => Close();

    private void UpdateClock(object? sender, EventArgs? e)
    {
        var now = DateTime.Now;
        ClockText.Text = now.ToString("hh:mm tt");
        DateText.Text = now.ToString("ddd, MMM dd");
        PeriodText.Text = now.ToString("tt");
    }

    private void SetActiveNav(Border item)
    {
        var defaultStyle = (Style)FindResource("NavItemStyle");
        var activeStyle = (Style)FindResource("NavItemActiveStyle");

        foreach (var nav in _allNavItems)
            if (nav != NavSignOut)
                nav.Style = defaultStyle;

        if (item != NavSignOut)
            item.Style = activeStyle;

        _activeNavItem = item;
    }

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

    private void OpenWindow(Window? window)
    {
        if (window is null)
            return;

        window.Owner = Application.Current.MainWindow;
        window.ShowDialog();
    }

    private async void ShowDashboard()
    {
        DashboardPanel.Visibility = Visibility.Visible;
        MainFrame.Visibility = Visibility.Collapsed;
        MainFrame.Content = null;

        SetActiveNav(NavDashboard);
        HeaderTitle.Text = "Dashboard";
        HeaderSubtitle.Text = "Overview of your driving license system";

        StartDashboardAnimations();
        StartTypewriterEffect();

        await _dashboardViewModel.LoadAsync();
    }

    private void NavDashboard_Click(object sender, MouseButtonEventArgs e) => ShowDashboard();

    private void NavPeople_Click(object sender, MouseButtonEventArgs e) =>
        NavigateToPage("Manage People", "View and manage all registered people", NavPeople,
            _serviceProvider.GetRequiredService<PeoplePage>());

    private void NavDrivers_Click(object sender, MouseButtonEventArgs e) =>
        NavigateToPage("Manage Drivers", "View and manage all licensed drivers", NavDrivers,
            _serviceProvider.GetRequiredService<DriversPage>());

    private void NavLocalApps_Click(object sender, MouseButtonEventArgs e) =>
        NavigateToPage("Local Applications", "Manage local driving license applications", NavLocalApps,
            _serviceProvider.GetRequiredService<LDLAppPage>());

    private void NavIntlApps_Click(object sender, MouseButtonEventArgs e) =>
        NavigateToPage("International Applications", "Manage international license applications", NavIntlApps,
            _serviceProvider.GetRequiredService<InterLAppPage>());

    private void NavDetained_Click(object sender, MouseButtonEventArgs e) =>
        NavigateToPage("Detained Licenses", "View and manage all detained licenses", NavDetained,
            _serviceProvider.GetRequiredService<ListDetainedLicenses>());

    private void NavRetakeTest_Click(object sender, MouseButtonEventArgs e) =>
        NavigateToPage("Retake Test", "Schedule a test retake for an applicant", NavRetakeTest,
            _serviceProvider.GetRequiredService<LDLAppPage>());

    private void NavUsers_Click(object sender, MouseButtonEventArgs e)
    {
        if (!IsAdmin())
            return;

        NavigateToPage("Users", "Manage system users and permissions", NavUsers,
            _serviceProvider.GetRequiredService<UserPage>());
    }

    private void NavAppTypes_Click(object sender, MouseButtonEventArgs e)
    {
        if (!IsAdmin())
            return;

        NavigateToPage("Application Types", "Configure application type settings", NavAppTypes,
            _serviceProvider.GetRequiredService<ManageApplicationTypePage>());
    }

    private void NavTestTypes_Click(object sender, MouseButtonEventArgs e)
    {
        if (!IsAdmin())
            return;

        NavigateToPage("Test Types", "Configure test type settings", NavTestTypes,
            _serviceProvider.GetRequiredService<ManageTestTypePage>());
    }

    private void NavNewLocal_Click(object sender, MouseButtonEventArgs e) =>
        OpenWindow(_serviceProvider.GetRequiredService<NewLocalLicnnse>());

    private void NavNewInternational_Click(object sender, MouseButtonEventArgs e) =>
        OpenWindow(_serviceProvider.GetRequiredService<NewInternationalLicenseApplicationWin>());

    private void NavRenew_Click(object sender, MouseButtonEventArgs e) =>
        OpenWindow(_serviceProvider.GetRequiredService<RenewLicenseApplicationWin>());

    private void NavReplace_Click(object sender, MouseButtonEventArgs e) =>
        OpenWindow(_serviceProvider.GetRequiredService<ReplacementDamagedLicense>());

    private void NavReleaseDetained_Click(object sender, MouseButtonEventArgs e) =>
        OpenWindow(_serviceProvider.GetRequiredService<ReleaseDetainedLicenseWin>());

    private void NavDetainLicense_Click(object sender, MouseButtonEventArgs e) =>
        OpenWindow(_serviceProvider.GetRequiredService<DetainLicenseWin>());

    private async void NavMyProfile_Click(object sender, MouseButtonEventArgs e)
    {
        var vm = _serviceProvider.GetRequiredService<AddEditUserViewModel>();
        await vm.InitializeCurrentProfileAsync();

        var window = _serviceProvider.GetRequiredService<UserDetailsWindow>();
        window.DataContext = vm;
        OpenWindow(window);
    }

    private void NavChangePassword_Click(object sender, MouseButtonEventArgs e)
    {
        var vm = _serviceProvider.GetRequiredService<ChangePasswordViewModel>();
        vm.UserId = _currentUserSession.UserId;
        vm.UserName = _currentUserSession.Username;
        OpenWindow(new ChangePasswordWindow(vm));
    }

    private void NavSignOut_Click(object sender, MouseButtonEventArgs e)
    {
        var result = _userNotifications.ShowConfirmation("Are you sure you want to sign out?", "Sign Out");

        if (result != MessageBoxResult.Yes)
            return;

        _currentUserSession.Clear();
        _serviceProvider.GetRequiredService<LoginWindow>().Show();
        Close();
    }

    private void StartDashboardAnimations()
    {
        StatsRow1.Opacity = 0;
        StatsRow1RT.Y = 24;
        StatsRow2.Opacity = 0;
        StatsRow2RT.Y = 24;
        QuickActionsSection.Opacity = 0;
        QuickActionsRT.Y = 24;
        RecentActivitiesSection.Opacity = 0;
        RecentActivitiesRT.Y = 24;

        ((Storyboard)FindResource("StaggerEnterStoryboard")).Begin(this);
        ((Storyboard)FindResource("PulseGlowStoryboard")).Begin(this);
    }

    private void StartTypewriterEffect()
    {
        StopTypewriterEffect();

        _typewriterIndex = 0;
        TypewriterText.Text = string.Empty;
        TypewriterCursor.Opacity = 1;

        _cursorBlinkStoryboard = (Storyboard)FindResource("CursorBlinkStoryboard");
        _cursorBlinkStoryboard.Begin(TypewriterCursor, true);

        _typewriterTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(25) };
        _typewriterTimer.Tick += TypewriterTimer_Tick;
        _typewriterTimer.Start();
    }

    private void StopTypewriterEffect()
    {
        if (_typewriterTimer is not null)
        {
            _typewriterTimer.Stop();
            _typewriterTimer.Tick -= TypewriterTimer_Tick;
            _typewriterTimer = null;
        }

        if (_cursorBlinkStoryboard is null)
            return;

        try { _cursorBlinkStoryboard.Remove(TypewriterCursor); }
        catch { }

        _cursorBlinkStoryboard = null;
    }

    private void TypewriterTimer_Tick(object? sender, EventArgs e)
    {
        if (_typewriterIndex < TypewriterFullText.Length)
            TypewriterText.Text += TypewriterFullText[_typewriterIndex++];
        else
            StopTypewriterEffect();
    }
}
