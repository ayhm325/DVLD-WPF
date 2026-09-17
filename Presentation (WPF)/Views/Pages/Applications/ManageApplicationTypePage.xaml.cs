using Presentation.ViewModels;
using System.Windows.Controls;

namespace Presentation.Views.Pages.Applications;

public partial class ManageApplicationTypePage : Page
{
    public ManageApplicationTypePage(ApplicationTypeViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
    }
}