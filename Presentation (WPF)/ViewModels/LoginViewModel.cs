using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DVLD.Contracts.Auth;
using DVLD_WPF;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Services;
using Presentation.Services.Api;
using Presentation.Services.UI;
using System.Windows;

namespace Presentation.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IAuthApiClient _authApiClient;
    private readonly ICurrentUserSession _currentUser;
    private readonly IServiceProvider _serviceProvider;
    private readonly IApiNotificationService _notifications;
    private readonly IUserNotificationService _userNotifications;

    [ObservableProperty] private bool _rememberMe;
    [ObservableProperty] private string _username = string.Empty;
    [ObservableProperty] private string _password = string.Empty;

    public LoginViewModel(
        IAuthApiClient authApiClient,
        ICurrentUserSession currentUser,
        IServiceProvider serviceProvider,
        IApiNotificationService notifications,
        IUserNotificationService userNotifications)
    {
        _authApiClient = authApiClient ?? throw new ArgumentNullException(nameof(authApiClient));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
        _userNotifications = userNotifications ?? throw new ArgumentNullException(nameof(userNotifications));

        RememberMe = Properties.Settings.Default.RememberMe;

        if (RememberMe)
        {
            Username = Properties.Settings.Default.Username;
            Password = Properties.Settings.Default.Password;
        }
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) ||
            string.IsNullOrWhiteSpace(Password))
        {
            _userNotifications.ShowWarning(
                "Username and password are required.",
                "Login Failed");
            return;
        }

        var result = await _authApiClient.LoginAsync(new LoginRequest
        {
            UserName = Username.Trim(),
            Password = Password
        });

        if (result.IsFailure)
        {
            _notifications.ShowFailure(result, "Login Failed");
            return;
        }

        if (result.Value is null)
        {
            _userNotifications.ShowError(
                "The API did not return user authentication data.",
                "Login Failed");
            return;
        }

        var user = result.Value;

        _currentUser.SetSession(
            user.UserId,
            user.UserName,
            user.FullName,
            user.Role,
            user.AccessToken);

        SaveRememberMeSettings();

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();

        System.Windows.Application.Current.Windows
            .OfType<LoginWindow>()
            .FirstOrDefault()
            ?.Close();
    }

    private void SaveRememberMeSettings()
    {
        if (RememberMe)
        {
            Properties.Settings.Default.Username = Username;
            Properties.Settings.Default.Password = Password;
            Properties.Settings.Default.RememberMe = true;
        }
        else
        {
            Properties.Settings.Default.Username = string.Empty;
            Properties.Settings.Default.Password = string.Empty;
            Properties.Settings.Default.RememberMe = false;
        }

        Properties.Settings.Default.Save();
    }
}