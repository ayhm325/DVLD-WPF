using Microsoft.Extensions.DependencyInjection;
using Presentation.Services;
using Presentation.Services.Api;
using Presentation.Views.Windows;

public class WindowService : IWindowService
{
    private readonly IServiceProvider _serviceProvider;

    public WindowService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void ShowPersonDetails(int personId)
    {
        var peopleApiClient =
            _serviceProvider.GetRequiredService<IPeopleApiClient>();

        var window =
            new PersonDetailsWindow(
                personId,
                peopleApiClient);

        window.ShowDialog();
    }
}