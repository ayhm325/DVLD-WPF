using Domain.Entities;
using Domain.Enums;
using Presentation.ViewModels;
using Presentation.Views.Windows;
using System.Windows;

namespace Presentation.Views.Windows
{
    public partial class TestAppointmentWin : Window
    {
        public TestAppointmentWin(TestAppointmentViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;

            ApplicationBasicInfoControl.OpenPersonRequested += OnOpenPersonRequested;
            DrivingLicenseApplicationInfoControl.OpenLicenseRequested += OnOpenLicenseRequested;
        }

        private void OnOpenPersonRequested(int personId)
        {
            var window = new PersonDetailsWindow(personId);
            window.ShowDialog();
        }

        private void OnOpenLicenseRequested(int applicationId)
        {
            var window = new DriverLicenseInfoWin(applicationId);
            window.ShowDialog();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        
    }
}