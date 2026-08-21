using Application.DTOs.InternationalLicenseDTO;
using System.Windows;
using System.Windows.Controls;

namespace Presentation.Views.Controls
{
    public partial class ApplicationInfo : UserControl
    {
        public ApplicationInfo()
        {
            InitializeComponent();
        }

        public InternationalLicenseApplicationInfoDto? Application
        {
            get => (InternationalLicenseApplicationInfoDto?)GetValue(ApplicationProperty);
            set => SetValue(ApplicationProperty, value);
        }

        public static readonly DependencyProperty ApplicationProperty =
            DependencyProperty.Register(
                nameof(Application),
                typeof(InternationalLicenseApplicationInfoDto),
                typeof(ApplicationInfo),
                new PropertyMetadata(null));
    }
}



//using Application.DTOs.ApplicationDTO;
//using System.Windows;
//using System.Windows.Controls;

//namespace Presentation.Views.Controls
//{
//    public partial class ApplicationInfo : UserControl
//    {
//        public ApplicationInfo()
//        {
//            InitializeComponent();
//        }


//        public ApplicationDto? Application
//        {
//            get => (ApplicationDto?)GetValue(ApplicationProperty);
//            set => SetValue(ApplicationProperty, value);
//        }


//        public static readonly DependencyProperty ApplicationProperty =
//            DependencyProperty.Register(
//                nameof(Application),
//                typeof(ApplicationDto),
//                typeof(ApplicationInfo),
//                new PropertyMetadata(null));
//    }
//}