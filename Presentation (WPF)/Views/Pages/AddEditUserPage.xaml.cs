
using System.Windows.Controls;


namespace Presentation.Views.Pages
{
    public partial class AddEditUserPage : Page
    {
        public AddEditUserPage(AddEditUserViewModel viewModel)
        {
            InitializeComponent();
            this.DataContext = viewModel;

        }
    }
}