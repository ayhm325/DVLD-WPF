using Application.Interfaces;
using Presentation.ViewModels;
using System.Windows;

namespace Presentation.Views.Windows
{
    public partial class UserDetailsWindow : Window
    {
        private readonly ICurrentUserService _currentUser;

        public UserDetailsWindow(
            AddEditUserViewModel userViewModel,
            ICurrentUserService currentUser)
        {
            InitializeComponent();

            DataContext = userViewModel;

            _currentUser = currentUser;

            Loaded += UserDetailsWindow_Loaded;
        }

        private void UserDetailsWindow_Loaded(object sender, RoutedEventArgs e)
        {
           
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}