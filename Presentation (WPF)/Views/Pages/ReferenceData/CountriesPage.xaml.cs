using Presentation.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace Presentation.Views.Pages.ReferenceData;

public partial class CountriesPage : Page
{
    private readonly CountriesViewModel _viewModel;

    public CountriesPage(CountriesViewModel viewModel)
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
