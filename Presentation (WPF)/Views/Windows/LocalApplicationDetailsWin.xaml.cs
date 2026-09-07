using Presentation.Services.Api;
using Presentation.ViewModels;
using Presentation.Views.Controls;
using System;
using System.Windows;

namespace Presentation.Views.Windows;

public partial class LocalApplicationDetailsWin : Window
{
    private readonly IPeopleApiClient _peopleApiClient;

    public LocalApplicationDetailsWin(
        LocalApplicationDetailsViewModel vm,
        IPeopleApiClient peopleApiClient)
    {
        InitializeComponent();

        DataContext = vm;

        _peopleApiClient =
            peopleApiClient
            ?? throw new ArgumentNullException(nameof(peopleApiClient));

        ApplicationBasicInfoControl.OpenPersonRequested +=
            OnOpenPersonRequested;

        DrivingLicenseApplicationInfoControl.OpenLicenseRequested +=
            OnOpenLicenseRequested;
    }

    private void OnOpenPersonRequested(int personId)
    {
        var window =
            new PersonDetailsWindow(
                personId,
                _peopleApiClient);

        window.ShowDialog();
    }

    private void OnOpenLicenseRequested(int licenseId)
    {
        if (licenseId <= 0)
            return;

        var window =
            new DriverLicenseInfoWin(licenseId)
            {
                Owner = this
            };

        window.ShowDialog();
    }

    private void CloseButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        Close();
    }
}