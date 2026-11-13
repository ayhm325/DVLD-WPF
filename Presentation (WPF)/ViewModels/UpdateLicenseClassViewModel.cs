using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DVLD.Contracts.LicenseClass;
using Presentation.Services.Api;
using Presentation.Services.UI;
using System.Windows;

namespace Presentation.ViewModels;

public partial class UpdateLicenseClassViewModel : ObservableObject
{
    private readonly ILicenseClassesApiClient _licenseClassesApiClient;
    private readonly IApiNotificationService _notifications;
    private readonly IUserNotificationService _userNotifications;

    [ObservableProperty] private UpdateLicenseClassRequest? _currentLicenseClass = new();
    [ObservableProperty] private bool _isSaving;

    public UpdateLicenseClassViewModel(
        ILicenseClassesApiClient licenseClassesApiClient,
        IApiNotificationService notifications,
        IUserNotificationService userNotifications)
    {
        _licenseClassesApiClient = licenseClassesApiClient ?? throw new ArgumentNullException(nameof(licenseClassesApiClient));
        _notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
        _userNotifications = userNotifications ?? throw new ArgumentNullException(nameof(userNotifications));
    }

    public async Task InitializeAsync(int id, CancellationToken cancellationToken = default)
    {
        var result = await _licenseClassesApiClient.GetByIdAsync(id, cancellationToken);
        if (_notifications.ShowFailure(result, "Load License Class Failed"))
        {
            CurrentLicenseClass = null;
            return;
        }

        if (result.Value is null)
        {
            CurrentLicenseClass = null;
            _userNotifications.ShowWarning("License class was not found.", "Not Found");
            return;
        }

        var licenseClass = result.Value;
        CurrentLicenseClass = new UpdateLicenseClassRequest
        {
            LicenseClassId = licenseClass.LicenseClassId,
            LicenseClassName = licenseClass.LicenseClassName,
            LicenseClassDescription = licenseClass.LicenseClassDescription,
            MinAllowedAge = licenseClass.MinAllowedAge,
            DefaultValidityLength = licenseClass.DefaultValidityLength,
            LicenseClassFees = licenseClass.LicenseClassFees
        };
    }

    partial void OnIsSavingChanged(bool value) => SaveCommand.NotifyCanExecuteChanged();

    partial void OnCurrentLicenseClassChanged(UpdateLicenseClassRequest? value) => SaveCommand.NotifyCanExecuteChanged();

    private bool CanSave() => CurrentLicenseClass is not null && !IsSaving;

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync(Window window)
    {
        if (CurrentLicenseClass is null || IsSaving)
            return;

        IsSaving = true;
        try
        {
            var result = await _licenseClassesApiClient.UpdateAsync(
                CurrentLicenseClass.LicenseClassId,
                CurrentLicenseClass);

            if (_notifications.ShowFailure(result, "Update License Class Failed"))
                return;

            _userNotifications.ShowInfo("License class updated successfully.", "Saved");
            window.DialogResult = true;
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    private static void Close(Window window) => window?.Close();
}
