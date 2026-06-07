using Presentation.ViewModels;
using System.Windows;


namespace Presentation.Views.Windows
{
    /// <summary>
    /// Interaction logic for NewLocalLicnnse.xaml
    /// </summary>
    public partial class NewLocalLicnnse : Window
    {
        public NewLocalLicnnse(AddEditLDLAppViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private void NextButton_Click(object sender, RoutedEventArgs e) => MainTabControl.SelectedIndex = 1;
        private void CloseButton_Click(object sender, RoutedEventArgs e) => this.Close();
    }
}
