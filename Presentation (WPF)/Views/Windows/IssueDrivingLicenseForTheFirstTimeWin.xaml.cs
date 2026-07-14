using Presentation.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Presentation.Views.Windows
{
    /// <summary>
    /// Interaction logic for IssueDrivingLicenseForTheFirstTimeWin.xaml
    /// </summary>
    public partial class IssueDrivingLicenseForTheFirstTimeWin : Window
    {
        public IssueDrivingLicenseForTheFirstTimeWin(
        IssueDrivingLicenseForTheFirstTimeViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;

            applicationBasicInfo.OpenPersonRequested += OpenPerson;


            drivingLicenseInfo.OpenLicenseRequested += OpenLicense;
        }

        private void OpenPerson(int personId)
        {
            var window = new PersonDetailsWindow(personId);
            window.ShowDialog();
        }



        private void OpenLicense(int applicationId)
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
