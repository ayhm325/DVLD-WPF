using Presentation.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace Presentation.Views.Windows;

public partial class NewLocalLicnnse : Window
{
    private readonly AddEditLDLAppViewModel _viewModel;

    public NewLocalLicnnse(AddEditLDLAppViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        DataContext = _viewModel;
        _viewModel.RequestClose += () => Close();
        _viewModel.SaveCompleted += (success) => { if (success) Close(); };
        Loaded += NewLocalLicnnse_Loaded;
    }

    private async void NewLocalLicnnse_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= NewLocalLicnnse_Loaded;
        if (!_viewModel.IsInitialized)
            await _viewModel.InitializeAsync();
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }

    private void NextButton_Click(object sender, RoutedEventArgs e) =>
        MainTabControl.SelectedIndex = 1;

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}
