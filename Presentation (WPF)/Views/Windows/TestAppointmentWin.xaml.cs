using Presentation.Services.Api;
using Presentation.Services.UI;
using Presentation.ViewModels;
using System.Windows;

namespace Presentation.Views.Windows;

public partial class TestAppointmentWin : Window
{
    private readonly IPeopleApiClient _peopleApiClient;
    private readonly ILicensesApiClient _licensesApiClient;
    private readonly IApiNotificationService _notifications;

    public TestAppointmentWin(
        TestAppointmentViewModel vm,
        IPeopleApiClient peopleApiClient,
        ILicensesApiClient licensesApiClient,
        IApiNotificationService notifications)
    {
        InitializeComponent();

        DataContext = vm ?? throw new ArgumentNullException(nameof(vm));
        _peopleApiClient = peopleApiClient ?? throw new ArgumentNullException(nameof(peopleApiClient));
        _licensesApiClient = licensesApiClient ?? throw new ArgumentNullException(nameof(licensesApiClient));
        _notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));

        ApplicationBasicInfoControl.OpenPersonRequested += OnOpenPersonRequested;
        DrivingLicenseApplicationInfoControl.OpenLicenseRequested += OnOpenLicenseRequested;
    }

    private void OnOpenPersonRequested(int personId)
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

    private void OnOpenLicenseRequested(int applicationId)
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
        ApplicationBasicInfoControl.OpenPersonRequested -= OnOpenPersonRequested;
        DrivingLicenseApplicationInfoControl.OpenLicenseRequested -= OnOpenLicenseRequested;
        base.OnClosed(e);
    }
}