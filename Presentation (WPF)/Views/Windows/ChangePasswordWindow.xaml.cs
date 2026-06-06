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
            
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}