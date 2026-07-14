using Application.DTOs;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Presentation.Views.Controls
{
    public partial class DriverLicenseInfo : UserControl
    {
        public DriverLicenseInfo()
        {
            InitializeComponent();
        }

        public DriverLicenseInfoDto? License
        {
            get => (DriverLicenseInfoDto?)GetValue(LicenseProperty);
            set => SetValue(LicenseProperty, value);
        }

        public static readonly DependencyProperty LicenseProperty =
            DependencyProperty.Register(
                nameof(License),
                typeof(DriverLicenseInfoDto),
                typeof(DriverLicenseInfo),
                new PropertyMetadata(null, OnLicenseChanged)); // أضفنا الـ Callback

        // هذه الدالة هي السحر الذي يربط الـ DTO بالـ XAML
        private static void OnLicenseChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (DriverLicenseInfo)d;

            // تحديث الـ DataContext كلما تم تغيير قيمة الـ License
            control.DataContext = e.NewValue;
        }
        private void BtnSearch_Click(object sender, MouseButtonEventArgs e)
        {

        }

        private void TxtLicenseID_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}