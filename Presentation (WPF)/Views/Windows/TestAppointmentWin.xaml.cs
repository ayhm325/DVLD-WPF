
using Presentation.Services.Api;
using Presentation.ViewModels;
using System.Windows;

namespace Presentation.Views.Windows;

public partial class TestAppointmentWin : Window
{
    private readonly IPeopleApiClient _peopleApiClient;

    public TestAppointmentWin(
        TestAppointmentViewModel vm,
        IPeopleApiClient peopleApiClient)
    {
        InitializeComponent();

        DataContext = vm;

        _peopleApiClient = peopleApiClient
            ?? throw new ArgumentNullException(nameof(peopleApiClient));

        ApplicationBasicInfoControl.OpenPersonRequested +=
            OnOpenPersonRequested;

        DrivingLicenseApplicationInfoControl.OpenLicenseRequested +=
            OnOpenLicenseRequested;
    }

    private void OnOpenPersonRequested(int personId)
    {
        var window = new PersonDetailsWindow(
            personId,
            _peopleApiClient);

        window.ShowDialog();
    }

    private void OnOpenLicenseRequested(int applicationId)
    {
        var window = new DriverLicenseInfoWin(applicationId);
        window.ShowDialog();
    }

    private void CloseButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        Close();
    }
}