using Presentation.ViewModels;
using System.Windows.Controls;
using System.Windows.Input;

namespace Presentation.Views.Pages
{
    public partial class ListDetainedLicenses : Page
    {
        private readonly ListDetainedLicensesViewModel _viewModel;


        public ListDetainedLicenses(
            ListDetainedLicensesViewModel viewModel)
        {
            InitializeComponent();

            _viewModel = viewModel;

            DataContext = _viewModel;

            Loaded += ListDetainedLicenses_Loaded;
        }


        private async void ListDetainedLicenses_Loaded(
            object sender,
            System.Windows.RoutedEventArgs e)
        {
            await _viewModel.LoadAsync();
        }


        private void DataGridRow_MouseRightButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            if (sender is DataGridRow row)
            {
                row.IsSelected = true;
                row.Focus();
            }
        }
    }
}