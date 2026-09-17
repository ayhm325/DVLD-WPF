using Microsoft.Extensions.DependencyInjection;
using Presentation.Services;
using Presentation.Services.Api;
using Presentation.Services.UI;
using Presentation.Views.Windows;

public class WindowService : IWindowService
{
    private readonly IServiceProvider _serviceProvider;

    public WindowService(IServiceProvider serviceProvider) =>
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

    public void ShowPersonDetails(int personId)
    {
        var peopleApiClient = _serviceProvider.GetRequiredService<IPeopleApiClient>();
        var notifications = _serviceProvider.GetRequiredService<IApiNotificationService>();

        var window = new PersonDetailsWindow(
            personId,
            peopleApiClient,
            notifications);

        window.ShowDialog();
    }
}