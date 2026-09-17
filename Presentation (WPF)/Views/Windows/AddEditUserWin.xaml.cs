using Presentation.ViewModels;
using System.Windows;

namespace Presentation.Views.Windows;

public partial class AddEditUserWin : Window
{
    public AddEditUserWin(AddEditUserViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}