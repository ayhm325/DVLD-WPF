using Presentation.ViewModels;
using System.Windows;

namespace Presentation.Views.Windows.Applications;

public partial class EditApplicationTypeWindow : Window
{
    public EditApplicationTypeWindow(UpdateApplicationTypeViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}