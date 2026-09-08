using DVLD.Contracts.InternationalLicense;
using System.Windows;
using System.Windows.Controls;

namespace Presentation.Views.Controls;

public partial class ApplicationInfo : UserControl
{
    public ApplicationInfo()
    {
        InitializeComponent();
    }

    public InternationalLicenseResponse? Application
    {
        get =>
            (InternationalLicenseResponse?)
            GetValue(ApplicationProperty);

        set =>
            SetValue(ApplicationProperty, value);
    }

    public static readonly DependencyProperty ApplicationProperty =
        DependencyProperty.Register(
            nameof(Application),
            typeof(InternationalLicenseResponse),
            typeof(ApplicationInfo),
            new PropertyMetadata(null));
}