using Presentation.ViewModels;
using System.Windows.Controls;


namespace Presentation.Views.Pages.Applications
{
    /// <summary>
    /// Interaction logic for ManageApplicationTypePage.xaml
    /// </summary>
    public partial class ManageApplicationTypePage : Page
    {
        public ManageApplicationTypePage(ApplicationTypeViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
