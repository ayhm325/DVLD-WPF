using Microsoft.Extensions.DependencyInjection;
using Presentation.Views;
using Presentation.Views.Pages;
using Presentation.Views.Pages.Applications;
using Presentation.Views.Pages.ReferenceData;
using Presentation.Views.Pages.Tests;
using System;
using System.Windows.Controls;

namespace Presentation.Services;

public sealed record ShellPageDestination(string Title, string Subtitle, Page Page);

public sealed class ShellPageNavigationService(IServiceProvider serviceProvider)
{
    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

    public bool TryResolve(string navigationKey, string role, out ShellPageDestination? destination)
    {
        destination = navigationKey switch
        {
            "Manage People" when IsStaff(role) => Create<PeoplePage>("Manage People", "View and manage all registered people"),
            "Manage Drivers" when IsStaff(role) => Create<DriversPage>("Manage Drivers", "View and manage all licensed drivers"),
            "Local Applications" when IsStaff(role) => Create<LDLAppPage>("Local Applications", "Manage local driving license applications"),
            "International Apps" when IsStaff(role) => Create<InterLAppPage>("International Applications", "Manage international license applications"),
            "Detained Licenses" when IsStaff(role) => Create<ListDetainedLicenses>("Detained Licenses", "View and manage all detained licenses"),
            "Users" when IsAdmin(role) => Create<UserPage>("Users", "Manage system users and permissions"),
            "Application Types" when IsAdmin(role) => Create<ManageApplicationTypePage>("Application Types", "Configure application type settings"),
            "Test Types" when IsAdmin(role) => Create<ManageTestTypePage>("Test Types", "Configure test type settings"),
            "License Classes" when IsAuthorizedRole(role) => Create<LicenseClassesPage>("License Classes", "Review license class rules and fees"),
            "Countries" when IsAuthorizedRole(role) => Create<CountriesPage>("Countries", "Browse the read-only country reference list"),
            _ => null
        };

        return destination is not null;
    }

    private ShellPageDestination Create<TPage>(string title, string subtitle) where TPage : Page =>
        new(title, subtitle, _serviceProvider.GetRequiredService<TPage>());

    private static bool IsAdmin(string role) =>
        string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);

    private static bool IsStaff(string role) =>
        string.Equals(role, "Staff", StringComparison.OrdinalIgnoreCase);

    private static bool IsAuthorizedRole(string role) => IsAdmin(role) || IsStaff(role);
}



