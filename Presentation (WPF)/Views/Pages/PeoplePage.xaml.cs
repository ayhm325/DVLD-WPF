using Microsoft.Extensions.DependencyInjection;
using Presentation.ViewModels;
using Presentation.Views.Windows;
using System.Windows;
using System.Windows.Controls;

namespace Presentation.Views;

public partial class PeoplePage : Page
{
    private PeopleViewModel? ViewModel => DataContext as PeopleViewModel;

    private readonly IServiceProvider _serviceProvider;

    public PeoplePage(
        PeopleViewModel viewModel,
        IServiceProvider serviceProvider)
    {
        InitializeComponent();

        DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

        IsVisibleChanged += PeoplePage_IsVisibleChanged;
        Unloaded += PeoplePage_Unloaded;
    }

    private async void PeoplePage_IsVisibleChanged(
        object sender,
        DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is true && ViewModel is not null)
            await ViewModel.LoadPeopleAsync();
    }

    private async void AddPerson_Click(object sender, RoutedEventArgs e)
    {
        var viewModel = _serviceProvider.GetRequiredService<AddEditPersonViewModel>();

        await viewModel.InitializeAsync(null);

        var window = new AddEditPersonWin(viewModel)
        {
            Owner = Window.GetWindow(this)
        };

        window.ShowDialog();
    }

    private void PeoplePage_Unloaded(object sender, RoutedEventArgs e)
    {
        IsVisibleChanged -= PeoplePage_IsVisibleChanged;
        Unloaded -= PeoplePage_Unloaded;
    }
}