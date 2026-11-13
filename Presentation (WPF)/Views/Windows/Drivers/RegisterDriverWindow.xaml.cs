using Presentation.ViewModels;
using System.Windows;

namespace Presentation.Views.Windows.Drivers;

public partial class RegisterDriverWindow : Window
{
    private readonly RegisterDriverViewModel _viewModel;

    public RegisterDriverWindow(RegisterDriverViewModel viewModel)
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
