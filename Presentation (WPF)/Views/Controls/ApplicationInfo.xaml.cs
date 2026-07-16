using Application.DTOs;
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


        public ApplicationDto? Application
        {
            get => (ApplicationDto?)GetValue(ApplicationProperty);
            set => SetValue(ApplicationProperty, value);
        }


        public static readonly DependencyProperty ApplicationProperty =
            DependencyProperty.Register(
                nameof(Application),
                typeof(ApplicationDto),
                typeof(ApplicationInfo),
                new PropertyMetadata(null));
    }
}