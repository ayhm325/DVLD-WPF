using Presentation.ViewModels;
using System.Windows;

namespace Presentation.Views.Windows
{
    public partial class UserDetailsWindow : Window
    {
        public UserDetailsWindow(
            AddEditUserViewModel userViewModel)
        {
            InitializeComponent();

            DataContext = userViewModel;

            Loaded += UserDetailsWindow_Loaded;
        }

        private void UserDetailsWindow_Loaded(
            object sender,
            RoutedEventArgs e)
        {
        }

        private void CloseButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            Close();
        }
    }
}