using System;
using System.Windows;
using System.Windows.Controls;

namespace Presentation.Views.Controls
{
    public partial class DrivingLicenseApplicationInfo : UserControl
    {
        public event Action<int>? OpenLicenseRequested;

        public DrivingLicenseApplicationInfo()
        {
            InitializeComponent();
        }

        #region DrivingLicenseApplicationId

        public int DrivingLicenseApplicationId
        {
            get => (int)GetValue(DrivingLicenseApplicationIdProperty);
            set => SetValue(DrivingLicenseApplicationIdProperty, value);
        }

        public static readonly DependencyProperty DrivingLicenseApplicationIdProperty =
            DependencyProperty.Register(
                nameof(DrivingLicenseApplicationId),
                typeof(int),
                typeof(DrivingLicenseApplicationInfo),
                new PropertyMetadata(0));

        #endregion

        #region PassedTests

        public int PassedTests
        {
            get => (int)GetValue(PassedTestsProperty);
            set => SetValue(PassedTestsProperty, value);
        }

        public static readonly DependencyProperty PassedTestsProperty =
            DependencyProperty.Register(
                nameof(PassedTests),
                typeof(int),
                typeof(DrivingLicenseApplicationInfo),
                new PropertyMetadata(0));

        #endregion

        #region TotalTests

        public int TotalTests
        {
            get => (int)GetValue(TotalTestsProperty);
            set => SetValue(TotalTestsProperty, value);
        }

        public static readonly DependencyProperty TotalTestsProperty =
            DependencyProperty.Register(
                nameof(TotalTests),
                typeof(int),
                typeof(DrivingLicenseApplicationInfo),
                new PropertyMetadata(0));

        #endregion

        #region LicenseClassName

        public string LicenseClassName
        {
            get => (string)GetValue(LicenseClassNameProperty);
            set => SetValue(LicenseClassNameProperty, value);
        }

        public static readonly DependencyProperty LicenseClassNameProperty =
            DependencyProperty.Register(
                nameof(LicenseClassName),
                typeof(string),
                typeof(DrivingLicenseApplicationInfo),
                new PropertyMetadata(string.Empty));

        #endregion

        private void LicenseInfoButton_Click(object sender, RoutedEventArgs e)
        {
            if (DrivingLicenseApplicationId == 0)
                return;

            OpenLicenseRequested?.Invoke(DrivingLicenseApplicationId);
        }
    }
}