using Application.DTOs;
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


        public static readonly DependencyProperty ReleaseProperty =
            DependencyProperty.Register(
                nameof(Release),
                typeof(DetainedLicenseDto),
                typeof(ReleaseDetainedInfo),
                new PropertyMetadata(null));


        public DetainedLicenseDto? Release
        {
            get => (DetainedLicenseDto?)GetValue(ReleaseProperty);
            set => SetValue(ReleaseProperty, value);
        }



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