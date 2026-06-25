using Presentation.ViewModels;
using System.Windows;

namespace Presentation.Views.Windows
{
    public partial class TakeTestWin : Window
    {
        public TakeTestWin(TakeTestViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;            
        }

        
    }
}