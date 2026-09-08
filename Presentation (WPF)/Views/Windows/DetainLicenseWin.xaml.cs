using Presentation.ViewModels;
using System.Windows;


namespace Presentation.Views.Windows
{
    /// <summary>
    /// Interaction logic for DetainLicenseWin.xaml
    /// </summary>
    public partial class DetainLicenseWin : Window
    {
        public DetainLicenseWin(DetainLicenseViewModel viewModel)
        {
            InitializeComponent();

            DataContext = viewModel;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
