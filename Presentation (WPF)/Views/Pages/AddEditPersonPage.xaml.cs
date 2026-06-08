

using System.Windows.Controls;
using Presentation.ViewModels;

namespace Presentation.Views
{
    public partial class AddEditPersonPage : Page
    {
        public AddEditPersonPage(AddEditPersonViewModel viewModel)
        {
            InitializeComponent();
            this.DataContext = viewModel;
        }

    }
}