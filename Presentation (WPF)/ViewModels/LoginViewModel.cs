using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DVLD.Contracts.Auth;
using DVLD_WPF;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Services;
using Presentation.Services.Api;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Presentation.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IAuthApiClient _authApiClient;
    private readonly ICurrentUserSession _currentUser;
    private readonly IServiceProvider _serviceProvider;

    public LoginViewModel(
        IAuthApiClient authApiClient,
        ICurrentUserSession currentUser,
        IServiceProvider serviceProvider)
    {
        _authApiClient = authApiClient
            ?? throw new ArgumentNullException(nameof(authApiClient));

        _currentUser = currentUser
            ?? throw new ArgumentNullException(nameof(currentUser));

        _serviceProvider = serviceProvider
            ?? throw new ArgumentNullException(nameof(serviceProvider));

        RememberMe = Properties.Settings.Default.RememberMe;

        if (RememberMe)
        {
            Username = Properties.Settings.Default.Username;
            Password = Properties.Settings.Default.Password;
        }
    }

    [ObservableProperty]
    private bool rememberMe;

    [ObservableProperty]
    private string username = string.Empty;

    [ObservableProperty]
    private string password = string.Empty;

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) ||
            string.IsNullOrWhiteSpace(Password))
        {
            MessageBox.Show(
                "Username and password are required.",
                "Login Failed",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        var request = new LoginRequest
        {
            UserName = Username.Trim(),
            Password = Password
        };

        var loginResult =
            await _authApiClient.LoginAsync(request);

        if (loginResult.IsFailure)
        {
            MessageBox.Show(
                loginResult.Error,
                "Login Failed",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        var user = loginResult.Value!;

        _currentUser.SetSession(
            user.UserId,
            user.UserName,
            user.FullName,
            user.Role,
            user.AccessToken);

        SaveRememberMeSettings();

        var mainWindow =
            _serviceProvider.GetRequiredService<MainWindow>();

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