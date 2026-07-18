using Presentation.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Presentation.Views.Windows
{
    /// <summary>
    /// Interaction logic for DetainLicenseWin.xaml
    /// </summary>
    public partial class DetainLicenseWin : Window
    {
        public DetainLicenseWin(DetainLicenseViewModel viewModel)
        {
            InitializeComponent();

            DataContext = viewModel;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
