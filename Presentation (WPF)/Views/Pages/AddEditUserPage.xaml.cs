using System.Windows;
using System.Windows.Controls;
using Presentation.ViewModels;

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