using Presentation.ViewModels;
using System.Windows;

namespace Presentation.Views.Windows;

public partial class ReleaseDetainedLicenseWin : Window
{
    public ReleaseDetainedLicenseWin(ReleaseDetainedViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    public async Task LoadAsync(int licenseId)
    {
        if (DataContext is ReleaseDetainedViewModel viewModel)
            await viewModel.LoadAsync(licenseId);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}