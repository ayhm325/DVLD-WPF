using Presentation.ViewModels;
using System.Windows;

namespace Presentation.Views.Windows.ReferenceData;

public partial class EditLicenseClassWindow : Window
{
    public EditLicenseClassWindow(UpdateLicenseClassViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
    }
}
