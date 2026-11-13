using Presentation.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace Presentation.Views.Pages.ReferenceData;

public partial class LicenseClassesPage : Page
{
    private readonly LicenseClassesViewModel _viewModel;

    public LicenseClassesPage(LicenseClassesViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        DataContext = _viewModel;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        await _viewModel.LoadAsync();
    }
}
