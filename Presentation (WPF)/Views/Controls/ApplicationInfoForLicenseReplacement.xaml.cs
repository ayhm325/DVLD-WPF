using Presentation.ViewModels;
using System.Windows;
using System.Windows.Controls;
using static Presentation.ViewModels.ReplacementDamagedLicenseViewModel;

namespace Presentation.Views.Controls;

public partial class ApplicationInfoForLicenseReplacement
    : UserControl
{
    public ApplicationInfoForLicenseReplacement()
    {
        InitializeComponent();
    }

    public ReplacementApplicationInfo? Application
    {
        get =>
            (ReplacementApplicationInfo?)GetValue(
                ApplicationProperty);

        set =>
            SetValue(
                ApplicationProperty,
                value);
    }

    public static readonly DependencyProperty ApplicationProperty =
        DependencyProperty.Register(
            nameof(Application),
            typeof(ReplacementApplicationInfo),
            typeof(ApplicationInfoForLicenseReplacement),
            new PropertyMetadata(null));
}