using Presentation.ViewModels;
using System.Windows;


namespace Presentation.Views.Windows
{
    /// <summary>
    /// Interaction logic for ScheduleTestWin.xaml
    /// </summary>
    public partial class ScheduleTestWin : Window
    {
        public ScheduleTestWin(ScheduleTestViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
