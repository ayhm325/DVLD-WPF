using Presentation.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace Presentation.Views.Pages.Tests;

public partial class ManageTestTypePage : Page
{
    private readonly TestTypeViewModel _viewModel;

    public ManageTestTypePage(TestTypeViewModel viewModel)
    {
        InitializeComponent();

        _viewModel =
            viewModel
            ?? throw new ArgumentNullException(nameof(viewModel));

        DataContext = _viewModel;

        Loaded += ManageTestTypePage_Loaded;
    }

    private async void ManageTestTypePage_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        Loaded -= ManageTestTypePage_Loaded;

        await _viewModel.LoadTestTypesAsync();
    }
}