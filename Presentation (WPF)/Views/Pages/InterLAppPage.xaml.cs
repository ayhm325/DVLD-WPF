using DVLD_WPF;
using Presentation.ViewModels;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;

namespace Presentation.Views.Pages
{
    /// <summary>
    /// Interaction logic for InterLAppPage.xaml
    /// </summary>
    public partial class InterLAppPage : Page
    {
        public InterLAppPage()
        {
            InitializeComponent();
            DataContext = App.ServiceProvider.GetRequiredService<InternationalViewModel>();
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
