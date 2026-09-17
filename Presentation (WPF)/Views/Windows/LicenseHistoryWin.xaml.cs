using Presentation.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Presentation.Views.Windows;

public partial class LicenseHistoryWin : Window
{
    private readonly LicenseHistoryViewModel _vm;
    private readonly int _personId;

    public LicenseHistoryWin(LicenseHistoryViewModel vm, int personId)
    {
        InitializeComponent();

        _vm = vm ?? throw new ArgumentNullException(nameof(vm));
        _personId = personId > 0
            ? personId
            : throw new ArgumentOutOfRangeException(nameof(personId));

        DataContext = _vm;
        Loaded += LicenseHistoryWin_Loaded;
    }

    private async void LicenseHistoryWin_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= LicenseHistoryWin_Loaded;
        await _vm.LoadAsync(_personId);
        ucPersonInfo.Person = _vm.Person;
    }

    private void Row_RightClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is DataGridRow row)
            row.IsSelected = true;
    }

    private void btnClose_Click(object sender, RoutedEventArgs e) => Close();
}