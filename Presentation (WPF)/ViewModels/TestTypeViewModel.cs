using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DVLD.Contracts.TestType;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Services.Api;
using Presentation.Views.Windows.Tests;
using System.Collections.ObjectModel;
using System.Windows;

namespace Presentation.ViewModels;

public partial class TestTypeViewModel : ObservableObject
{
    private readonly ITestTypesApiClient _testTypesApiClient;
    private readonly IServiceProvider _serviceProvider;

    public ObservableCollection<TestTypeResponse> TestTypes { get; } = [];

    public TestTypeViewModel(
        ITestTypesApiClient testTypesApiClient,
        IServiceProvider serviceProvider)
    {
        _testTypesApiClient =
            testTypesApiClient
            ?? throw new ArgumentNullException(
                nameof(testTypesApiClient));

        _serviceProvider =
            serviceProvider
            ?? throw new ArgumentNullException(
                nameof(serviceProvider));
    }

    public async Task LoadTestTypesAsync(
        CancellationToken cancellationToken = default)
    {
        var result =
            await _testTypesApiClient.GetAllAsync(
                cancellationToken);

        if (result.IsFailure)
        {
            MessageBox.Show(
                result.Error,
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            return;
        }

        TestTypes.Clear();

        if (result.Value is null)
            return;

        foreach (var testType in result.Value)
        {
            TestTypes.Add(testType);
        }
    }

    [RelayCommand]
    private async Task EditTestType(
        TestTypeResponse? selectedType)
    {
        if (selectedType is null)
            return;

        var updateVm =
            _serviceProvider
                .GetRequiredService<UpdateTestTypeViewModel>();

        await updateVm.InitializeAsync(
            selectedType.TestTypeId);

        if (updateVm.CurrentTestType is null)
            return;

        var editWindow =
            new EditTestTypeWindow(updateVm)
            {
                Owner =System.Windows.Application.Current.MainWindow
            };

        editWindow.ShowDialog();

        await LoadTestTypesAsync();
    }
}