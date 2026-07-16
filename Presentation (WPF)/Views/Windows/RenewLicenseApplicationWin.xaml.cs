using Presentation.ViewModels;
using System.Windows;

namespace Presentation.Views.Windows
{
    public partial class RenewLicenseApplicationWin : Window
    {
        public RenewLicenseApplicationWin(RenewLicenseViewModel vm)
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