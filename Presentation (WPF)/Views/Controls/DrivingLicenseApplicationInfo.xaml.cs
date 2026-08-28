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

        // =====================================================
        // LOCAL DRIVING LICENSE APPLICATION ID
        // =====================================================

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

        // =====================================================
        // LICENSE ID
        // =====================================================

        #region LicenseId

        public int LicenseId
        {
            get => (int)GetValue(LicenseIdProperty);
            set => SetValue(LicenseIdProperty, value);
        }

        public static readonly DependencyProperty LicenseIdProperty =
            DependencyProperty.Register(
                nameof(LicenseId),
                typeof(int),
                typeof(DrivingLicenseApplicationInfo),
                new PropertyMetadata(0));

        #endregion

        // =====================================================
        // PASSED TESTS
        // =====================================================

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

        // =====================================================
        // TOTAL TESTS
        // =====================================================

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

        // =====================================================
        // LICENSE CLASS NAME
        // =====================================================

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

        // =====================================================
        // LICENSE INFO BUTTON
        // =====================================================

        private void LicenseInfoButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            // مهم:
            // لا نرسل LocalApplicationId
            // نرسل LicenseId الحقيقي.

            if (LicenseId <= 0)
                return;

            OpenLicenseRequested?.Invoke(LicenseId);
        }
    }
}