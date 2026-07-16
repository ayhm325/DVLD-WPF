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
                new PropertyMetadata(null));


        public string LicenseIdText
        {
            get => (string)GetValue(LicenseIdTextProperty);
            set => SetValue(LicenseIdTextProperty, value);
        }

        public static readonly DependencyProperty LicenseIdTextProperty =
            DependencyProperty.Register(
                nameof(LicenseIdText),
                typeof(string),
                typeof(DriverLicenseInfo));


        public ICommand? SearchCommand
        {
            get => (ICommand?)GetValue(SearchCommandProperty);
            set => SetValue(SearchCommandProperty, value);
        }

        public static readonly DependencyProperty SearchCommandProperty =
            DependencyProperty.Register(
                nameof(SearchCommand),
                typeof(ICommand),
                typeof(DriverLicenseInfo),
                new PropertyMetadata(null));
    }
}