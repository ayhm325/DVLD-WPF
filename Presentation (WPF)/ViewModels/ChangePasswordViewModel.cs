using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DVLD.Contracts.Auth;
using Presentation.Services;
using Presentation.Services.UI;
using System.Windows;

namespace Presentation.ViewModels;

public partial class ChangePasswordViewModel : ObservableObject
{
    private readonly IAuthApiClient _authApiClient;
    private readonly IApiNotificationService _notifications;
    private readonly IUserNotificationService _userNotifications;

    [ObservableProperty] private int _userId;
    [ObservableProperty] private string _userName = string.Empty;
    [ObservableProperty] private string _currentPassword = string.Empty;
    [ObservableProperty] private string _newPassword = string.Empty;
    [ObservableProperty] private string _confirmNewPassword = string.Empty;
    [ObservableProperty] private bool _isCurrentPasswordVisible;
    [ObservableProperty] private bool _isNewPasswordVisible;
    [ObservableProperty] private bool _isConfirmNewPasswordVisible;

    public ChangePasswordViewModel(
        IAuthApiClient authApiClient,
        IApiNotificationService notifications,
        IUserNotificationService userNotifications)
    {
        _authApiClient = authApiClient ?? throw new ArgumentNullException(nameof(authApiClient));
        _notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
        _userNotifications = userNotifications ?? throw new ArgumentNullException(nameof(userNotifications));
    }

    [RelayCommand]
    private void ToggleCurrentPassword() =>
        IsCurrentPasswordVisible = !IsCurrentPasswordVisible;

    [RelayCommand]
    private void ToggleNewPassword() =>
        IsNewPasswordVisible = !IsNewPasswordVisible;

    [RelayCommand]
    private void ToggleConfirmPassword() =>
        IsConfirmNewPasswordVisible = !IsConfirmNewPasswordVisible;

    [RelayCommand]
    private async Task ChangePassword()
    {
        if (string.IsNullOrWhiteSpace(CurrentPassword) ||
            string.IsNullOrWhiteSpace(NewPassword) ||
            string.IsNullOrWhiteSpace(ConfirmNewPassword))
        {
            _userNotifications.ShowWarning(
                "Please fill in all fields.",
                "Validation Error");
            return;
        }

        if (NewPassword != ConfirmNewPassword)
        {
            _userNotifications.ShowWarning(
                "Passwords do not match.",
                "Validation Error");
            return;
        }

        if (CurrentPassword == NewPassword)
        {
            _userNotifications.ShowWarning(
                "New password must be different from current.",
                "Validation Error");
            return;
        }

        try
        {
            var result = await _authApiClient.ChangePasswordAsync(
                new ChangePasswordRequest
                {
                    CurrentPassword = CurrentPassword,
                    NewPassword = NewPassword
                });

            if (result.IsFailure)
            {
                _notifications.ShowFailure(
                    result,
                    "Change Password");
                return;
            }

            _userNotifications.ShowInfo(
                "Password changed successfully.",
                "Success");

            CloseWindow();
        }
        catch (Exception ex)
        {
            _userNotifications.ShowError(
                GetExceptionMessage(ex),
                "Change Password");
        }
    }

    private void CloseWindow()
    {
        var window = System.Windows.Application.Current.Windows
            .OfType<Window>()
            .FirstOrDefault(w => w.DataContext == this);

        window?.Close();
    }

    private static string GetExceptionMessage(Exception ex) =>
        ex.InnerException is null
            ? ex.Message
            : $"{ex.Message}{Environment.NewLine}{Environment.NewLine}" +
              $"Inner Exception:{Environment.NewLine}{ex.InnerException.Message}";
}