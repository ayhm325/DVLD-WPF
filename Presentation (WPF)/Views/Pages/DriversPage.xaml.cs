using Presentation.ViewModels;
using System.Windows.Controls;
using System.Windows.Input;

namespace Presentation.Views.Pages
{
    public partial class DriversPage : Page
    {
        private readonly DriversViewModel _viewModel;

        public DriversPage(DriversViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;

           
            txtFilterValue.TextChanged += Filter_Changed;
            cmbFilterBy.SelectionChanged += Filter_Changed;

            Loaded += DriversPage_Loaded;
        }

        private void Filter_Changed(object sender, System.EventArgs e)
        {
            string filterValue = txtFilterValue.Text;
            string filterBy = (cmbFilterBy.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "None";

            _viewModel.FilterDrivers(filterValue, filterBy);
        }

        private async void DriversPage_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            await _viewModel.LoadAsync();
        }

        private void DataGridRow_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var row = sender as DataGridRow;
            if (row != null && !row.IsSelected)
            {
                row.Focus();
                row.IsSelected = true;
            }
        }
    }
}