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

    [RelayCommand]
    private async Task SaveAsync(Window window)
    {
        if (CurrentTestType is null)
            return;

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

    [RelayCommand]
    private static void Close(Window window) => window?.Close();
}