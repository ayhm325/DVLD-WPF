using Presentation.ViewModels;
using System.Windows.Controls;
using System.Windows.Input;

namespace Presentation.Views.Pages
{
    public partial class LDLAppPage : Page
    {
        public LDLAppPage(LDLAppViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
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