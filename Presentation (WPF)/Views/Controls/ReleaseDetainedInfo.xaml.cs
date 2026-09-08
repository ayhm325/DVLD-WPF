using DVLD.Contracts.DetainedLicense;
using System.Windows;
using System.Windows.Controls;

namespace Presentation.Views.Controls
{
    public partial class ReleaseDetainedInfo : UserControl
    {
        public ReleaseDetainedInfo()
        {
            InitializeComponent();
        }

        // =========================================================
        // Release
        // =========================================================

        public static readonly DependencyProperty ReleaseProperty =
        DependencyProperty.Register(
            nameof(Release),
            typeof(DetainedLicenseResponse),
            typeof(ReleaseDetainedInfo),
            new PropertyMetadata(null));

        public DetainedLicenseResponse? Release
        {
            get => (DetainedLicenseResponse?)GetValue(ReleaseProperty);
            set => SetValue(ReleaseProperty, value);
        }

        // =========================================================
        // Application Fees
        // =========================================================

        public static readonly DependencyProperty ApplicationFeesProperty =
            DependencyProperty.Register(
                nameof(ApplicationFees),
                typeof(decimal),
                typeof(ReleaseDetainedInfo),
                new PropertyMetadata(0m));

        public decimal ApplicationFees
        {
            get => (decimal)GetValue(ApplicationFeesProperty);
            set => SetValue(ApplicationFeesProperty, value);
        }

        // =========================================================
        // Total Fees
        // =========================================================

        public static readonly DependencyProperty TotalFeesProperty =
            DependencyProperty.Register(
                nameof(TotalFees),
                typeof(decimal),
                typeof(ReleaseDetainedInfo),
                new PropertyMetadata(0m));

        public decimal TotalFees
        {
            get => (decimal)GetValue(TotalFeesProperty);
            set => SetValue(TotalFeesProperty, value);
        }
    }
}
