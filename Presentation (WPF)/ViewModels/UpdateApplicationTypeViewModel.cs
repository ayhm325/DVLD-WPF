using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DVLD.Contracts.ApplicationType;
using Presentation.Services.Api;
using Presentation.Services.UI;
using System.Windows;

namespace Presentation.ViewModels;

public partial class UpdateApplicationTypeViewModel : ObservableObject
{
    private readonly IApplicationTypesApiClient _applicationTypesApiClient;
    private readonly IApiNotificationService _notifications;
    private readonly IUserNotificationService _userNotifications;

    [ObservableProperty] private UpdateApplicationTypeRequest? _currentApplicationType = new();
    [ObservableProperty] private bool _isSaving;

    private bool CanSave() => CurrentApplicationType is not null && !IsSaving;
    partial void OnIsSavingChanged(bool value) => SaveCommand.NotifyCanExecuteChanged();
    partial void OnCurrentApplicationTypeChanged(UpdateApplicationTypeRequest? value) => SaveCommand.NotifyCanExecuteChanged();

    public UpdateApplicationTypeViewModel(
        IApplicationTypesApiClient applicationTypesApiClient,
        IApiNotificationService notifications,
        IUserNotificationService userNotifications)
    {
        _applicationTypesApiClient = applicationTypesApiClient ?? throw new ArgumentNullException(nameof(applicationTypesApiClient));
        _notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
        _userNotifications = userNotifications ?? throw new ArgumentNullException(nameof(userNotifications));
    }

    public async Task InitializeAsync(int id, CancellationToken cancellationToken = default)
    {
        var result = await _applicationTypesApiClient.GetByIdAsync(id, cancellationToken);

        if (result.IsFailure)
        {
            CurrentApplicationType = null;
            _notifications.ShowFailure(result);
            return;
        }

        if (result.Value is null)
        {
            CurrentApplicationType = null;
            _userNotifications.ShowWarning("Application Type was not found.", "Error");
            return;
        }

        var applicationType = result.Value;

        CurrentApplicationType = new UpdateApplicationTypeRequest
        {
            ApplicationTypeId = applicationType.ApplicationTypeId,
            ApplicationTypeTitle = applicationType.ApplicationTypeTitle,
            ApplicationTypeFees = applicationType.ApplicationTypeFees
        };
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync(Window window)
    {
        if (!CanSave() || CurrentApplicationType is null)
            return;

        IsSaving = true;
        try
        {
            var result = await _applicationTypesApiClient.UpdateAsync(
                CurrentApplicationType.ApplicationTypeId, CurrentApplicationType);

            if (result.IsFailure)
            {
                _notifications.ShowFailure(result, "Update Failed");
                return;
            }

            _userNotifications.ShowInfo("Application Type updated successfully!", "Success");
            window?.Close();
        }
        catch (Exception ex)
        {
            _userNotifications.ShowError(ex.Message, "Update Application Type");
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    private static void Close(Window window) => window?.Close();
}
