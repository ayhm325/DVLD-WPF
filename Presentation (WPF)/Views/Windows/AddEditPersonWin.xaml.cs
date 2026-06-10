using Presentation.ViewModels;
using System.Windows;


namespace Presentation.Views.Windows
{
    /// <summary>
    /// Interaction logic for AddeditPersonWin.xaml
    /// </summary>
    public partial class AddEditPersonWin : Window
    {
        public AddEditPersonWin(AddEditPersonViewModel vm)
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
