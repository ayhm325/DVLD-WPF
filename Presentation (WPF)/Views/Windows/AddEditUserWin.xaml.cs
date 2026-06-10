

using System.Windows;


namespace Presentation.Views.Windows
{
    /// <summary>
    /// Interaction logic for AddEditUser.xaml
    /// </summary>
    public partial class AddEditUserWin : Window
    {
        public AddEditUserWin(AddEditUserViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
