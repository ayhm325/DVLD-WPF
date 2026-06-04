using Presentation.ViewModels;
using System.Windows;
using Presentation.Models;

namespace Presentation.Views.Windows
{
    public partial class ChangePasswordWindow : Window
    {
        public ChangePasswordWindow(ChangePasswordViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            // ربط قيم PasswordBox بالـ ViewModel
            viewModel.GetPasswordValues = () => new PasswordData
            {
                Current = txtCurrentPassword.Password,
                New = txtNewPassword.Password,
                Confirm = txtConfirmPassword.Password
            };
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}