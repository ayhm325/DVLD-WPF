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
using Presentation.Views.Pages.ReferenceData;
using Presentation.Views.Windows;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
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
    private readonly ShellPageNavigationService _pageNavigation;
    private readonly List<Button> _allNavItems;

    private Button? _activeNavItem;
    private DispatcherTimer? _typewriterTimer;
    private Storyboard? _cursorBlinkStoryboard;
    private DispatcherTimer? _clockTimer;
    private int _typewriterIndex;
    private bool _isSidebarCollapsed;
    private bool _isSettingsExpanded;

    private const string TypewriterFullText =
        "Welcome back. Use the sidebar to navigate, or choose a quick action below to get started.";

    public string CurrentUserFullName => _currentUserSession.FullName;
    public string CurrentUserRole => _currentUserSession.Role;
    public int CurrentUserPersonId => _currentUserSession.PersonId;

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
        _pageNavigation = new ShellPageNavigationService(_serviceProvider);

        _allNavItems =
        [
            NavDashboard, NavPeople, NavDrivers, NavRenew, NavReplace,
            NavLocalApps, NavIntlApps, NavDetained, NavDetainLicense, NavUsers, NavAppTypes,
            NavTestTypes, NavLicenseClasses, NavCountries, NavMyProfile,
            NavChangePassword, NavSignOut
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
            NavRenew,
            NavReplace,
            NavDetainLicense,
            NavLocalApps,
            NavIntlApps,
            NavDrivers,
            NavDetained,
            NavPeopleHeader,
            NavLicenseServicesHeader,
            NavApplicationsHeader,
            QuickActionsSection);

        SetVisibility(
            isAdmin ? Visibility.Visible : Visibility.Collapsed,
            NavUsers,
            NavAppTypes,
            NavTestTypes);

        SetVisibility(
            isAdmin || isStaff ? Visibility.Visible : Visibility.Collapsed,
            NavSettingsHeader,
            NavSettings,
            NavLicenseClasses,
            NavCountries);
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

    private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (!_isSidebarCollapsed)
        {
            var preferredWidth = ActualWidth < 1120 ? 216 : 264;
            if (SidebarColumn.Width.Value != preferredWidth)
                SidebarColumn.Width = new GridLength(preferredWidth);
        }

        var statisticColumns = ActualWidth < 1180 ? 2 : 4;
        StatsPrimaryGrid.Columns = statisticColumns;
        StatsSecondaryGrid.Columns = statisticColumns;
    }
    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            return;
        }

        if (e.ChangedButton == MouseButton.Left && WindowState != WindowState.Maximized)
            DragMove();
    }

    private void MinBtn_Click(object sender, MouseButtonEventArgs e) { e.Handled = true; WindowState = WindowState.Minimized; }



    private void MaxBtn_Click(object sender, MouseButtonEventArgs e) { e.Handled = true; WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized; }



    private void CloseBtn_Click(object sender, MouseButtonEventArgs e) { e.Handled = true; Close(); }

    private void UpdateClock(object? sender, EventArgs? e)
    {
        var now = DateTime.Now;
        ClockText.Text = now.ToString("hh:mm tt");
        DateText.Text = now.ToString("ddd, MMM dd");
        PeriodText.Text = now.ToString("tt");
    }

    private void SetActiveNav(Button item)
    {
        var defaultStyle = (Style)FindResource("NavItemStyle");
        var activeStyle = (Style)FindResource("NavItemActiveStyle");

        foreach (var nav in _allNavItems)
            if (nav != NavSignOut)
                nav.Style = defaultStyle;

        if (item != NavSignOut)
            item.Style = activeStyle;

        if (IsSettingsItem(item))
        {
            NavSettings.Style = activeStyle;
            _isSettingsExpanded = true;
            UpdateSettingsSubmenu();
        }

        _activeNavItem = item;
    }

    private bool IsSettingsItem(Button item) =>
        item == NavAppTypes || item == NavLicenseClasses || item == NavTestTypes || item == NavCountries;

    private void SidebarCollapseButton_Click(object sender, RoutedEventArgs e)
    {
        _isSidebarCollapsed = !_isSidebarCollapsed;
        SidebarColumn.Width = new GridLength(_isSidebarCollapsed ? 72 : ActualWidth < 1120 ? 216 : 264);
        SidebarCollapseGlyph.Text = _isSidebarCollapsed ? "›" : "‹";
        SidebarCollapseButton.ToolTip = _isSidebarCollapsed ? "Expand sidebar" : "Collapse sidebar";

        foreach (var textBlock in FindVisualChildren<TextBlock>(this))
        {
            if (Equals(textBlock.Tag, "SidebarLabel") || Equals(textBlock.Tag, "SidebarSection"))
                textBlock.Visibility = _isSidebarCollapsed ? Visibility.Collapsed : Visibility.Visible;
        }

        foreach (var button in _allNavItems)
            button.HorizontalContentAlignment = _isSidebarCollapsed ? HorizontalAlignment.Center : HorizontalAlignment.Stretch;

        NavSettings.HorizontalContentAlignment = _isSidebarCollapsed ? HorizontalAlignment.Center : HorizontalAlignment.Stretch;
        UpdateSettingsSubmenu();
    }

    private void NavSettings_Click(object sender, RoutedEventArgs e)
    {
        if (_isSidebarCollapsed)
            SidebarCollapseButton_Click(SidebarCollapseButton, e);

        _isSettingsExpanded = !_isSettingsExpanded;
        UpdateSettingsSubmenu();
    }

    private void UpdateSettingsSubmenu()
    {
        SettingsSubmenu.Visibility = !_isSidebarCollapsed && _isSettingsExpanded
            ? Visibility.Visible
            : Visibility.Collapsed;
        SettingsChevron.Text = _isSettingsExpanded ? "⌄" : "›";
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            if (child is T result)
                yield return result;

            foreach (var descendant in FindVisualChildren<T>(child))
                yield return descendant;
        }
    }

    private void NavigateToPage(string title, string subtitle, Button navItem, Page page)
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

    private void NavDashboard_Click(object sender, RoutedEventArgs e) => ShowDashboard();

    private void NavPage_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string navigationKey } button)
            return;

        if (!_pageNavigation.TryResolve(navigationKey, _currentUserSession.Role, out var destination) || destination is null)
        {
            _userNotifications.ShowWarning(
                "This destination is unavailable for the current account.",
                "Navigation Unavailable");
            return;
        }

        NavigateToPage(destination.Title, destination.Subtitle, button, destination.Page);
    }
    private void QuickNewLocal_Click(object sender, RoutedEventArgs e) => NavNewLocal_Click(sender, e);

    private void QuickRenew_Click(object sender, RoutedEventArgs e) => NavRenew_Click(sender, e);

    private void QuickNewInternational_Click(object sender, RoutedEventArgs e) => NavNewInternational_Click(sender, e);

    private void NavNewLocal_Click(object sender, RoutedEventArgs e) =>
        OpenWindow(_serviceProvider.GetRequiredService<NewLocalLicnnse>());

    private void NavNewInternational_Click(object sender, RoutedEventArgs e) =>
        OpenWindow(_serviceProvider.GetRequiredService<NewInternationalLicenseApplicationWin>());

    private void NavRenew_Click(object sender, RoutedEventArgs e) =>
        OpenWindow(_serviceProvider.GetRequiredService<RenewLicenseApplicationWin>());

    private void NavReplace_Click(object sender, RoutedEventArgs e) =>
        OpenWindow(_serviceProvider.GetRequiredService<ReplacementDamagedLicense>());

    private void NavReleaseDetained_Click(object sender, RoutedEventArgs e) =>
        OpenWindow(_serviceProvider.GetRequiredService<ReleaseDetainedLicenseWin>());

    private void NavDetainLicense_Click(object sender, RoutedEventArgs e) =>
        OpenWindow(_serviceProvider.GetRequiredService<DetainLicenseWin>());

    private async void NavMyProfile_Click(object sender, RoutedEventArgs e)
    {
        var vm = _serviceProvider.GetRequiredService<AddEditUserViewModel>();
        await vm.InitializeCurrentProfileAsync();

        var window = _serviceProvider.GetRequiredService<UserDetailsWindow>();
        window.DataContext = vm;
        OpenWindow(window);
    }

    private void NavChangePassword_Click(object sender, RoutedEventArgs e)
    {
        var vm = _serviceProvider.GetRequiredService<ChangePasswordViewModel>();
        vm.UserId = _currentUserSession.UserId;
        vm.UserName = _currentUserSession.Username;
        OpenWindow(new ChangePasswordWindow(vm));
    }

    private void NavSignOut_Click(object sender, RoutedEventArgs e)
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






