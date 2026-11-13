using Presentation.ViewModels;
using System.Windows;

namespace Presentation.Views.Windows;

public partial class AddEditPersonWin : Window
{
    private readonly AddEditPersonViewModel _viewModel;

    public AddEditPersonWin(AddEditPersonViewModel vm)
    {
        InitializeComponent();
        _viewModel = vm ?? throw new ArgumentNullException(nameof(vm));
        DataContext = _viewModel;
        _viewModel.SaveCompleted += ViewModel_SaveCompleted;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    private void ViewModel_SaveCompleted(bool success)
    {
        if (success)
            Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.SaveCompleted -= ViewModel_SaveCompleted;
        base.OnClosed(e);
    }
}
