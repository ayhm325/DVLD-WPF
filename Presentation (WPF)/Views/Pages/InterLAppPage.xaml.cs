using Presentation.ViewModels;
using System.Windows.Controls;
using System.Windows.Input;

namespace Presentation.Views.Pages;

public partial class InterLAppPage : Page
{
    public InterLAppPage(InternationalViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
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