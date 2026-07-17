using Presentation.ViewModels;
using System.Windows;


namespace Presentation.Views.Windows
{
    /// <summary>
    /// Interaction logic for ReplacementDamagedLicense.xaml
    /// </summary>
    public partial class ReplacementDamagedLicense : Window
    {
        public ReplacementDamagedLicense(ReplacementDamagedLicenseViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
