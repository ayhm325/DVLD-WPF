using System.Windows;
using System.Windows.Media;

namespace Presentation.Views.Windows
{
    public partial class CustomMessageWindow : Window
    {
        public CustomMessageWindow(string title, string message, bool isSuccess)
        {
            InitializeComponent();

            TxtTitle.Text = title;
            TxtMessage.Text = message;

            if (isSuccess)
            {
                TxtTitle.Foreground = Brushes.SeaGreen;
            }
            else
            {
                TxtTitle.Foreground = Brushes.IndianRed;
            }
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}