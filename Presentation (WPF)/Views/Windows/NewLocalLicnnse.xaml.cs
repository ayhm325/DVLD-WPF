using Presentation.ViewModels;
using System.Windows;

namespace Presentation.Views.Windows
{
    public partial class NewLocalLicnnse : Window
    {
        private readonly AddEditLDLAppViewModel _viewModel;

        public NewLocalLicnnse(AddEditLDLAppViewModel viewModel)
        {
            InitializeComponent();

            _viewModel = viewModel;
            DataContext = _viewModel;

            Loaded += NewLocalLicnnse_Loaded;
        }

        private async void NewLocalLicnnse_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= NewLocalLicnnse_Loaded;
            await _viewModel.InitializeAsync();
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            MainTabControl.SelectedIndex = 1;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}