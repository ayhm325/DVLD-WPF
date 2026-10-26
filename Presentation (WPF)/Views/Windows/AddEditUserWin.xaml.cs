using Presentation.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace Presentation.Views.Windows;

public partial class AddEditUserWin : Window
{
    public AddEditUserWin(AddEditUserViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.RequestClose += () => Close();
        vm.SaveCompleted += (success) => { if (success) Close(); };
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}
