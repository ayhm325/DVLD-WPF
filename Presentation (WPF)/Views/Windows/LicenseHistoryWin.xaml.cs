using Presentation.ViewModels;
using System.Windows;

namespace Presentation.Views.Windows
{
    public partial class LicenseHistoryWin : Window
    {
        private readonly LicenseHistoryViewModel _vm;

        public LicenseHistoryWin(
            LicenseHistoryViewModel vm,
            int personId)
        {
            InitializeComponent();

            _vm = vm;
            DataContext = _vm;

            Loaded += async (_, _) =>
            {
                await _vm.LoadAsync(personId);

                ucPersonInfo.Person = _vm.Person;
            };
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}