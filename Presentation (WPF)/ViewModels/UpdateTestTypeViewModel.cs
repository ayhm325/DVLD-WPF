using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DVLD.Contracts.TestType;
using Presentation.Services.Api;
using System.Windows;

namespace Presentation.ViewModels;

public partial class UpdateTestTypeViewModel : ObservableObject
{
    private readonly ITestTypesApiClient _testTypesApiClient;

    [ObservableProperty]
    private UpdateTestTypeRequest? currentTestType = new();

    public UpdateTestTypeViewModel(
        ITestTypesApiClient testTypesApiClient)
    {
        _testTypesApiClient =
            testTypesApiClient
            ?? throw new ArgumentNullException(
                nameof(testTypesApiClient));
    }

    public async Task InitializeAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var result =
            await _testTypesApiClient.GetByIdAsync(
                id,
                cancellationToken);

        if (result.IsFailure)
        {
            CurrentTestType = null;

            MessageBox.Show(
                result.Error,
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        if (result.Value is null)
        {
            CurrentTestType = null;

            MessageBox.Show(
                "Test Type was not found.",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        CurrentTestType =
            new UpdateTestTypeRequest
            {
                TestTypeId =
                    result.Value.TestTypeId,

                TestTypeTitle =
                    result.Value.TestTypeTitle,

                TestTypeDescription =
                    result.Value.TestTypeDescription,

                TestTypeFees =
                    result.Value.TestTypeFees
            };
    }

    [RelayCommand]
    private async Task SaveAsync(Window window)
    {
        if (CurrentTestType is null)
            return;

        var result =
            await _testTypesApiClient.UpdateAsync(
                CurrentTestType.TestTypeId,
                CurrentTestType);

        if (result.IsSuccess)
        {
            MessageBox.Show(
                "Test Type updated successfully!",
                "Success",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            window?.Close();

            return;
        }

        MessageBox.Show(
            result.Error,
            "Update Failed",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }

    [RelayCommand]
    private void Close(Window window)
    {
        window?.Close();
    }
}