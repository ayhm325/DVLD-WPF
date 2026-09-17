using Presentation.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Presentation.Views.Pages;

public partial class DriversPage : Page
{
    private readonly DriversViewModel _viewModel;

    public DriversPage(DriversViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        DataContext = _viewModel;

        txtFilterValue.TextChanged += Filter_Changed;
        cmbFilterBy.SelectionChanged += Filter_Changed;
        Loaded += DriversPage_Loaded;
        Unloaded += DriversPage_Unloaded;
    }

    private void Filter_Changed(object sender, EventArgs e)
    {
        var filterValue = txtFilterValue.Text;
        var filterBy = (cmbFilterBy.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "None";

        _viewModel.FilterDrivers(filterValue, filterBy);
    }

    private async void DriversPage_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= DriversPage_Loaded;
        await _viewModel.LoadAsync();
    }

    private void DriversPage_Unloaded(object sender, RoutedEventArgs e)
    {
        txtFilterValue.TextChanged -= Filter_Changed;
        cmbFilterBy.SelectionChanged -= Filter_Changed;
        Unloaded -= DriversPage_Unloaded;
    }

    private void DataGridRow_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is DataGridRow row)
        {
            row.Focus();
            row.IsSelected = true;
        }
    }
}