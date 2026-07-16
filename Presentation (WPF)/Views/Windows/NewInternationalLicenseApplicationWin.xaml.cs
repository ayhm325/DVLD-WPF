

using Presentation.ViewModels;
using System.Windows;

namespace Presentation.Views.Windows
{
    public partial class NewInternationalLicenseApplicationWin : Window
    {
        public NewInternationalLicenseApplicationWin(
            NewInternationalLicenseApplicationViewModel viewModel)
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