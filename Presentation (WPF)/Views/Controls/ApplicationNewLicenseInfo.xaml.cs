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

        public object? Application
        {
            get => GetValue(ApplicationProperty);
            set => SetValue(ApplicationProperty, value);
        }

        public static readonly DependencyProperty ApplicationProperty =
            DependencyProperty.Register(
                nameof(Application),
                typeof(object),
                typeof(ApplicationNewLicenseInfo),
                new PropertyMetadata(null));

        public string? Notes
        {
            get => (string?)GetValue(NotesProperty);
            set => SetValue(NotesProperty, value);
        }

        public static readonly DependencyProperty NotesProperty =
            DependencyProperty.Register(
                nameof(Notes),
                typeof(string),
                typeof(ApplicationNewLicenseInfo),
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
    }
}