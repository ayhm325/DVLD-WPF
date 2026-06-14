using Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Services;
using Presentation.Views.Windows;
using System;

public class WindowService : IWindowService
{
    private readonly IServiceProvider _serviceProvider;

    public WindowService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void ShowPersonDetails(int personId)
    {
        var personService =
            _serviceProvider.GetRequiredService<IPersonService>();

        var window = new PersonDetailsWindow(personId);

        window.ShowDialog();
    }
}