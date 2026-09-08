using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DVLD.Contracts.ApplicationType;
using Presentation.Services.Api;
using System.Windows;

namespace Presentation.ViewModels;

public partial class UpdateApplicationTypeViewModel : ObservableObject
{
    private readonly IApplicationTypesApiClient _applicationTypesApiClient;

    [ObservableProperty]
    private UpdateApplicationTypeRequest? currentApplicationType = new();

    public UpdateApplicationTypeViewModel(
        IApplicationTypesApiClient applicationTypesApiClient)
    {
        _applicationTypesApiClient =
            applicationTypesApiClient
            ?? throw new ArgumentNullException(
                nameof(applicationTypesApiClient));
    }

    public async Task InitializeAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var result =
            await _applicationTypesApiClient.GetByIdAsync(
                id,
                cancellationToken);

        if (result.IsFailure)
        {
            CurrentApplicationType = null;

            MessageBox.Show(
                result.Error,
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        if (result.Value is null)
        {
            CurrentApplicationType = null;

            MessageBox.Show(
                "Application Type was not found.",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        CurrentApplicationType =
            new UpdateApplicationTypeRequest
            {
                ApplicationTypeId =
                    result.Value.ApplicationTypeId,

                ApplicationTypeTitle =
                    result.Value.ApplicationTypeTitle,

                ApplicationTypeFees =
                    result.Value.ApplicationTypeFees
            };
    }

    [RelayCommand]
    private async Task SaveAsync(Window window)
    {
        if (CurrentApplicationType is null)
            return;

        var result =
            await _applicationTypesApiClient.UpdateAsync(
                CurrentApplicationType.ApplicationTypeId,
                CurrentApplicationType);

        if (result.IsSuccess)
        {
            MessageBox.Show(
                "Application Type updated successfully!",
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