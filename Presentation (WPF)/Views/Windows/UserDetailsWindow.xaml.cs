
using Presentation.ViewModels;
using System.Windows;


namespace Presentation.Views.Windows
{
    /// <summary>
    /// Interaction logic for UserDetailsWindow.xaml
    /// </summary>
    public partial class UserDetailsWindow : Window
    {
        public UserDetailsWindow(AddEditUserViewModel userViewModel)
        {
            InitializeComponent();
            DataContext = userViewModel;

            InformationPerson.DataContext = userViewModel.Person;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

    }
}
