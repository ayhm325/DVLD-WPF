using DVLD.Contracts.DetainedLicense;
using System.Windows;
using System.Windows.Controls;

namespace Presentation.Views.Controls
{
    public partial class DetainInfo : UserControl
    {
        public DetainInfo()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty DetainProperty =
        DependencyProperty.Register(
            nameof(Detain),
            typeof(DetainedLicenseResponse),
            typeof(DetainInfo),
            new PropertyMetadata(null));

        public DetainedLicenseResponse? Detain
        {
            get => (DetainedLicenseResponse?)GetValue(DetainProperty);
            set => SetValue(DetainProperty, value);
        }

        public static readonly DependencyProperty FineFeesProperty =
            DependencyProperty.Register(
                nameof(FineFees),
                typeof(decimal),
                typeof(DetainInfo),
                new PropertyMetadata(0m));

        public decimal FineFees
        {
            get => (decimal)GetValue(FineFeesProperty);
            set => SetValue(FineFeesProperty, value);
        }
    }
}
