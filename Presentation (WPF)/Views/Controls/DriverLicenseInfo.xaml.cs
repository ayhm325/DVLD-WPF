using DVLD.Contracts.License;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Presentation.Views.Controls;

public partial class DriverLicenseInfo : UserControl
{
    public DriverLicenseInfo()
    {
        InitializeComponent();
    }

    public DriverLicenseInfoResponse? License
    {
        get => (DriverLicenseInfoResponse?)GetValue(LicenseProperty);
        set => SetValue(LicenseProperty, value);
    }

    public static readonly DependencyProperty LicenseProperty =
        DependencyProperty.Register(
            nameof(License),
            typeof(DriverLicenseInfoResponse),
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

    public bool IsLicenseIdReadOnly
    {
        get => (bool)GetValue(IsLicenseIdReadOnlyProperty);
        set => SetValue(IsLicenseIdReadOnlyProperty, value);
    }

    public static readonly DependencyProperty IsLicenseIdReadOnlyProperty =
        DependencyProperty.Register(
            nameof(IsLicenseIdReadOnly),
            typeof(bool),
            typeof(DriverLicenseInfo),
            new PropertyMetadata(false));
}