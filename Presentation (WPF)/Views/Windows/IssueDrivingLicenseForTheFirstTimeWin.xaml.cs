using Presentation.Services.Api;
using Presentation.Services.UI;
using Presentation.ViewModels;
using System.Windows;

namespace Presentation.Views.Windows;

public partial class IssueDrivingLicenseForTheFirstTimeWin : Window
{
    private readonly IPeopleApiClient _peopleApiClient;
    private readonly ILicensesApiClient _licensesApiClient;
    private readonly IApiNotificationService _notifications;

    public IssueDrivingLicenseForTheFirstTimeWin(
        IssueDrivingLicenseForTheFirstTimeViewModel? vm,
        IPeopleApiClient peopleApiClient,
        ILicensesApiClient licensesApiClient,
        IApiNotificationService notifications)
    {
        InitializeComponent();

        if (vm is not null)
            DataContext = vm;

        _peopleApiClient = peopleApiClient ?? throw new ArgumentNullException(nameof(peopleApiClient));
        _licensesApiClient = licensesApiClient ?? throw new ArgumentNullException(nameof(licensesApiClient));
        _notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));

        applicationBasicInfo.OpenPersonRequested += OpenPerson;
        drivingLicenseInfo.OpenLicenseRequested += OpenLicense;
    }

    private void OpenPerson(int personId)
    {
        var window = new PersonDetailsWindow(
            personId,
            _peopleApiClient,
            _notifications)
        {
            Owner = this
        };

        window.ShowDialog();
    }

    private void OpenLicense(int applicationId)
    {
        var window = new DriverLicenseInfoWin(
            applicationId,
            _licensesApiClient,
            _notifications)
        {
            Owner = this
        };

        window.ShowDialog();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    protected override void OnClosed(EventArgs e)
    {
        applicationBasicInfo.OpenPersonRequested -= OpenPerson;
        drivingLicenseInfo.OpenLicenseRequested -= OpenLicense;
        base.OnClosed(e);
    }
}