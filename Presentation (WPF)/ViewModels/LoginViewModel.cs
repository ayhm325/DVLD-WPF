using Application.DTOs.AuthDTO;
using Application.DTOs.UserDTO;
using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DVLD_WPF;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using Presentation.Services;

namespace Presentation.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IAuthApiClient _authApiClient;
    private readonly ICurrentUserService _currentUser;
    private readonly IServiceProvider _serviceProvider;

    public LoginViewModel(
        IAuthApiClient authApiClient,
        ICurrentUserService currentUser,
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

        try
        {
            var loginDto = new LoginRequestDto
            {
                UserName = Username.Trim(),
                Password = Password
            };

            var loginResult =
                await _authApiClient.LoginAsync(loginDto);

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

            _currentUser.UserId = user.UserId;
            _currentUser.Username = user.UserName;
            _currentUser.FullName = user.FullName;

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

            var mainWindow =
                _serviceProvider.GetRequiredService<MainWindow>();

            mainWindow.Show();

            System.Windows.Application.Current.Windows
                .OfType<LoginWindow>()
                .FirstOrDefault()
                ?.Close();
        }
        catch (HttpRequestException)
        {
            MessageBox.Show(
                "Unable to connect to the DVLD API.",
                "Connection Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        catch (TaskCanceledException)
        {
            MessageBox.Show(
                "The request to the DVLD API timed out.",
                "Connection Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"An error occurred during login: {ex.Message}",
                "Login Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}