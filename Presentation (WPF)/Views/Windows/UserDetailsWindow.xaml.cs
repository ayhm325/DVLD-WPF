using Presentation.ViewModels;
using System.Windows;

namespace Presentation.Views.Windows;

public partial class UserDetailsWindow : Window
{
    public UserDetailsWindow(AddEditUserViewModel userViewModel)
    {
        InitializeComponent();
        DataContext = userViewModel;
    }

    private void Header_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
            DragMove();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}