using Presentation.Services.Api;
using Presentation.ViewModels;
using System.Windows;

namespace Presentation.Views.Windows;

public partial class IssueDrivingLicenseForTheFirstTimeWin : Window
{
    private readonly IPeopleApiClient _peopleApiClient;

    public IssueDrivingLicenseForTheFirstTimeWin(
        IssueDrivingLicenseForTheFirstTimeViewModel vm,
        IPeopleApiClient peopleApiClient)
    {
        InitializeComponent();

        DataContext = vm;

        _peopleApiClient =
            peopleApiClient
            ?? throw new ArgumentNullException(nameof(peopleApiClient));

        applicationBasicInfo.OpenPersonRequested += OpenPerson;

        drivingLicenseInfo.OpenLicenseRequested += OpenLicense;
    }

    private void OpenPerson(int personId)
    {
        var window =
            new PersonDetailsWindow(
                personId,
                _peopleApiClient);

        window.ShowDialog();
    }

    private void OpenLicense(int applicationId)
    {
        var window =
            new DriverLicenseInfoWin(applicationId);

        window.ShowDialog();
    }

    private void CloseButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        Close();
    }

    private void drivingLicenseInfo_Loaded(
        object sender,
        RoutedEventArgs e)
    {
    }
}