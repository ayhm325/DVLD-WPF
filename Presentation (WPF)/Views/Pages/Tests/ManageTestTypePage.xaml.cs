using Presentation.ViewModels;
using System.Windows.Controls;


namespace Presentation.Views.Pages.Tests
{
    /// <summary>
    /// Interaction logic for ManageTestTypePage.xaml
    /// </summary>
    public partial class ManageTestTypePage : Page
    {
        public ManageTestTypePage(TestTypeViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }
    }
}
