using Application.DTOs;
using System.Windows;
using System.Windows.Controls;

namespace Presentation.Views.Controls
{
    public partial class ApplicationNewLicenseInfo : UserControl
    {
        public ApplicationNewLicenseInfo()
        {
            InitializeComponent();
        }


        public ApplicationNewLicenseInfoDto? Application
        {
            get => (ApplicationNewLicenseInfoDto?)GetValue(ApplicationProperty);
            set => SetValue(ApplicationProperty, value);
        }


        public static readonly DependencyProperty ApplicationProperty =
            DependencyProperty.Register(
                nameof(Application),
                typeof(ApplicationNewLicenseInfoDto),
                typeof(ApplicationNewLicenseInfo),
                new PropertyMetadata(null));
    }
}