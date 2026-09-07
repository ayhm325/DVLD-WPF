using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DVLD.Contracts.Auth;
using Presentation.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Presentation.ViewModels;

public partial class ChangePasswordViewModel : ObservableObject
{
    private readonly IAuthApiClient _authApiClient;

    // =========================
    // USER
    // =========================

    [ObservableProperty]
    private int _userId;

    [ObservableProperty]
    private string _userName = string.Empty;

    // =========================
    // PASSWORDS
    // =========================

    [ObservableProperty]
    private string _currentPassword = string.Empty;

    [ObservableProperty]
    private string _newPassword = string.Empty;

    [ObservableProperty]
    private string _confirmNewPassword = string.Empty;

    // =========================
    // PASSWORD VISIBILITY
    // =========================

    [ObservableProperty]
    private bool _isCurrentPasswordVisible;

    [ObservableProperty]
    private bool _isNewPasswordVisible;

    [ObservableProperty]
    private bool _isConfirmNewPasswordVisible;

    // =========================
    // CONSTRUCTOR
    // =========================

    public ChangePasswordViewModel(
        IAuthApiClient authApiClient)
    {
        _authApiClient = authApiClient
            ?? throw new ArgumentNullException(
                nameof(authApiClient));
    }

    // =========================
    // TOGGLE PASSWORD VISIBILITY
    // =========================

    [RelayCommand]
    private void ToggleCurrentPassword()
    {
        IsCurrentPasswordVisible =
            !IsCurrentPasswordVisible;
    }

    [RelayCommand]
    private void ToggleNewPassword()
    {
        IsNewPasswordVisible =
            !IsNewPasswordVisible;
    }

    [RelayCommand]
    private void ToggleConfirmPassword()
    {
        IsConfirmNewPasswordVisible =
            !IsConfirmNewPasswordVisible;
    }

    // =========================
    // CHANGE PASSWORD
    // =========================

    [RelayCommand]
    private async Task ChangePassword()
    {
        // =========================
        // BASIC VALIDATION
        // =========================

        if (string.IsNullOrWhiteSpace(CurrentPassword) ||
            string.IsNullOrWhiteSpace(NewPassword) ||
            string.IsNullOrWhiteSpace(ConfirmNewPassword))
        {
            MessageBox.Show(
                "Please fill in all fields.",
                "Validation Error",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        // =========================
        // CONFIRM NEW PASSWORD
        // =========================

        if (NewPassword != ConfirmNewPassword)
        {
            MessageBox.Show(
                "Passwords do not match.",
                "Validation Error",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        // =========================
        // SAME PASSWORD CHECK
        // =========================

        if (CurrentPassword == NewPassword)
        {
            MessageBox.Show(
                "New password must be different from current.",
                "Validation Error",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        try
        {
            var request = new ChangePasswordRequest
            {
                CurrentPassword = CurrentPassword,
                NewPassword = NewPassword
            };

            var result =
                await _authApiClient.ChangePasswordAsync(request);

            // =========================
            // SUCCESS
            // =========================

            if (result.IsSuccess)
            {
                MessageBox.Show(
                    "Password changed successfully.",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                var window =
                    System.Windows.Application.Current.Windows
                        .OfType<Window>()
                        .FirstOrDefault(
                            w => w.DataContext == this);

                window?.Close();

                return;
            }

            // =========================
            // FAILURE
            // =========================

            MessageBox.Show(
                result.Error,
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"An error occurred: {ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}