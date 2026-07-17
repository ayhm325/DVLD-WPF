using System.Windows;
using System.Windows.Controls;
using Application.DTOs;

namespace Presentation.Views.Controls
{
    public partial class ApplicationInfoForLicenseReplacement : UserControl
    {
        public ApplicationInfoForLicenseReplacement()
        {
            InitializeComponent();
        }

        public ApplicationReplacementInfoDto? Application
        {
            get => (ApplicationReplacementInfoDto?)GetValue(ApplicationProperty);
            set => SetValue(ApplicationProperty, value);
        }


        public static readonly DependencyProperty ApplicationProperty =
            DependencyProperty.Register(
                nameof(Application),
                typeof(ApplicationReplacementInfoDto),
                typeof(ApplicationInfoForLicenseReplacement),
                new PropertyMetadata(null));
    }
}