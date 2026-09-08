using Presentation.ViewModels;
using System.Windows;

namespace Presentation.Views.Windows
{
    /// <summary>
    /// Interaction logic for ReleaseDetainedLicense.xaml
    /// </summary>
    public partial class ReleaseDetainedLicenseWin : Window
    {
        public ReleaseDetainedLicenseWin(ReleaseDetainedViewModel viewModel)
        {
            InitializeComponent();
            this.DataContext = viewModel;
        }

        public async Task LoadAsync(int licenseId) { if (DataContext is ReleaseDetainedViewModel viewModel) { await viewModel.LoadAsync(licenseId); } }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
