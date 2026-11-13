using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DVLD.Contracts.TestType;
using Presentation.Services.Api;
using Presentation.Services.UI;
using System.Windows;

namespace Presentation.ViewModels;

public partial class UpdateTestTypeViewModel : ObservableObject
{
    private readonly ITestTypesApiClient _testTypesApiClient;
    private readonly IApiNotificationService _notifications;
    private readonly IUserNotificationService _userNotifications;

    [ObservableProperty] private UpdateTestTypeRequest? _currentTestType = new();
    [ObservableProperty] private bool _isSaving;

    private bool CanSave() => CurrentTestType is not null && !IsSaving;
    partial void OnIsSavingChanged(bool value) => SaveCommand.NotifyCanExecuteChanged();
    partial void OnCurrentTestTypeChanged(UpdateTestTypeRequest? value) => SaveCommand.NotifyCanExecuteChanged();

    public UpdateTestTypeViewModel(
        ITestTypesApiClient testTypesApiClient,
        IApiNotificationService notifications,
        IUserNotificationService userNotifications)
    {
        _testTypesApiClient = testTypesApiClient ?? throw new ArgumentNullException(nameof(testTypesApiClient));
        _notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
        _userNotifications = userNotifications ?? throw new ArgumentNullException(nameof(userNotifications));
    }

    public async Task InitializeAsync(int id, CancellationToken cancellationToken = default)
    {
        var result = await _testTypesApiClient.GetByIdAsync(id, cancellationToken);

        if (result.IsFailure)
        {
            CurrentTestType = null;
            _notifications.ShowFailure(result);
            return;
        }

        if (result.Value is null)
        {
            CurrentTestType = null;
            _userNotifications.ShowWarning("Test Type was not found.", "Error");
            return;
        }

        var testType = result.Value;
        CurrentTestType = new UpdateTestTypeRequest
        {
            TestTypeId = testType.TestTypeId,
            TestTypeTitle = testType.TestTypeTitle,
            TestTypeDescription = testType.TestTypeDescription,
            TestTypeFees = testType.TestTypeFees
        };
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync(Window window)
    {
        if (!CanSave() || CurrentTestType is null)
            return;

        IsSaving = true;
        try
        {
            var result = await _testTypesApiClient.UpdateAsync(
                CurrentTestType.TestTypeId, CurrentTestType);

            if (result.IsFailure)
            {
                _notifications.ShowFailure(result, "Update Failed");
                return;
            }

            _userNotifications.ShowInfo("Test Type updated successfully!", "Success");
            window?.Close();
        }
        catch (Exception ex)
        {
            _userNotifications.ShowError(ex.Message, "Update Test Type");
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    private static void Close(Window window) => window?.Close();
}
