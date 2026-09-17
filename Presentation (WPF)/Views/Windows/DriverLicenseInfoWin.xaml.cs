using DVLD.Contracts.License;
using Presentation.Services.Api;
using Presentation.Services.Results;
using Presentation.Services.UI;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace Presentation.Views.Windows;

public partial class DriverLicenseInfoWin : Window, INotifyPropertyChanged
{
    private readonly int _licenseId;
    private readonly ILicensesApiClient _licensesApiClient;
    private readonly IApiNotificationService _notifications;
    private DriverLicenseInfoResponse? _licenseData;

    public DriverLicenseInfoResponse? LicenseData
    {
        get => _licenseData;
        set
        {
            _licenseData = value;
            OnPropertyChanged();
        }
    }

    public ICommand CloseCommand { get; }

    public DriverLicenseInfoWin(
        int licenseId,
        ILicensesApiClient licensesApiClient,
        IApiNotificationService notifications)
    {
        InitializeComponent();

        if (licenseId <= 0)
            throw new ArgumentOutOfRangeException(nameof(licenseId));

        _licenseId = licenseId;
        _licensesApiClient = licensesApiClient
            ?? throw new ArgumentNullException(nameof(licensesApiClient));
        _notifications = notifications
            ?? throw new ArgumentNullException(nameof(notifications));

        DataContext = this;
        CloseCommand = new RelayCommand(_ => Close());
        Loaded += DriverLicenseInfoWin_Loaded;
    }

    private async void DriverLicenseInfoWin_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= DriverLicenseInfoWin_Loaded;

        var result = await _licensesApiClient.GetDetailsByIdAsync(_licenseId);

        if (result.IsFailure)
        {
            _notifications.ShowFailure(result, "License Information");
            return;
        }

        if (result.Value is null)
        {
            _notifications.ShowFailure(
                ApiResult.Failure(
                    "License information was not returned by the API."),
                "License Information");
            return;
        }

        LicenseData = result.Value;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(name));

    private void CloseButton_Click(object sender, RoutedEventArgs e) =>
        Close();
}

public sealed class RelayCommand(Action<object?> execute) : ICommand
{
    private readonly Action<object?> _execute =
        execute ?? throw new ArgumentNullException(nameof(execute));

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter) => _execute(parameter);

    public event EventHandler? CanExecuteChanged
    {
        add { }
        remove { }
    }
}