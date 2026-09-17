using Presentation.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace Presentation.Views;

public partial class UserPage : Page
{
    private UsersViewModel? ViewModel => DataContext as UsersViewModel;

    public UserPage(UsersViewModel viewModel)
    {
        InitializeComponent();

        DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

        IsVisibleChanged += UserPage_IsVisibleChanged;
        Unloaded += UserPage_Unloaded;
    }

    private async void UserPage_IsVisibleChanged(
        object sender,
        DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is true && ViewModel is not null)
            await ViewModel.LoadUsersAsync();
    }

    private void UserPage_Unloaded(object sender, RoutedEventArgs e)
    {
        IsVisibleChanged -= UserPage_IsVisibleChanged;
        Unloaded -= UserPage_Unloaded;
    }
}