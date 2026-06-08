using Presentation.ViewModels;
using System.Windows.Controls;

namespace Presentation.Views.Pages
{
    public partial class LDLAppPage : Page
    {
        public LDLAppPage(LDLAppViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

    }
}