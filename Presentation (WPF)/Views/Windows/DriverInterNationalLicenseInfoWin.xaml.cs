using DVLD.Contracts.InternationalLicense;
using Presentation.Services.Api;
using Presentation.Services.Results;
using Presentation.Services.UI;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace Presentation.Views.Windows;

public partial class DriverInterNationalLicenseInfoWin : Window, INotifyPropertyChanged
{
    private readonly int _internationalLicenseId;
    private readonly IInternationalLicensesApiClient _internationalLicensesApiClient;
    private readonly IApiNotificationService _notifications;
    private InternationalLicenseResponse? _licenseData;

    public InternationalLicenseResponse? LicenseData
    {
        get => _licenseData;
        set
        {
            _licenseData = value;
            OnPropertyChanged();
        }
    }

    public ICommand CloseCommand { get; }

    public DriverInterNationalLicenseInfoWin(
        int internationalLicenseId,
        IInternationalLicensesApiClient internationalLicensesApiClient,
        IApiNotificationService notifications)
    {
        InitializeComponent();

        if (internationalLicenseId <= 0)
            throw new ArgumentOutOfRangeException(nameof(internationalLicenseId));

        _internationalLicenseId = internationalLicenseId;
        _internationalLicensesApiClient = internationalLicensesApiClient
            ?? throw new ArgumentNullException(nameof(internationalLicensesApiClient));
        _notifications = notifications
            ?? throw new ArgumentNullException(nameof(notifications));

        DataContext = this;
        CloseCommand = new RelayCommand(_ => Close());
        Loaded += DriverInterNationalLicenseInfoWin_Loaded;
    }

    private async void DriverInterNationalLicenseInfoWin_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= DriverInterNationalLicenseInfoWin_Loaded;

        var result = await _internationalLicensesApiClient.GetByIdAsync(_internationalLicenseId);

        if (result.IsFailure)
        {
            _notifications.ShowFailure(result, "International License");
            return;
        }

        if (result.Value is null)
        {
            _notifications.ShowFailure(
                ApiResult.Failure("International license information was not returned by the API."),
                "International License");
            return;
        }

        LicenseData = result.Value;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}