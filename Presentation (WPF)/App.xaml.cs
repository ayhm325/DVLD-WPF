using Microsoft.Extensions.DependencyInjection;
using Presentation;
using Presentation.Services;
using Presentation.Services.Api;
using Presentation.ViewModels;
using Presentation.Views;
using Presentation.Views.Pages;
using Presentation.Views.Pages.Applications;
using Presentation.Views.Pages.Tests;
using Presentation.Views.Windows;
using Presentation.Views.Windows.Applications;
using Presentation.Views.Windows.Tests;
using System;
using System.Windows;

namespace DVLD_WPF;

public partial class App : System.Windows.Application
{
    private IServiceProvider _rootServiceProvider = null!;
    private IServiceScope _applicationScope = null!;

    public static IServiceProvider ServiceProvider { get; private set; } = null!;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            var services = new ServiceCollection();

            ConfigureServices(services);

            _rootServiceProvider =
                services.BuildServiceProvider(
                    new ServiceProviderOptions
                    {
                        ValidateScopes = true,
                        ValidateOnBuild = true
                    });

            _applicationScope =
                _rootServiceProvider.CreateScope();

            ServiceProvider =
                _applicationScope.ServiceProvider;

            var apiHostService =
                ServiceProvider.GetRequiredService<IApiHostService>();

            await apiHostService.EnsureApiRunningAsync();

            var loginWindow =
                ServiceProvider.GetRequiredService<LoginWindow>();

            loginWindow.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to start the application.\n\n{ex.Message}",
                "DVLD",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            Shutdown();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _applicationScope?.Dispose();
        (_rootServiceProvider as IDisposable)?.Dispose();

        base.OnExit(e);
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // =====================================================
        // Presentation Services
        // =====================================================

        services.AddSingleton<ICurrentUserSession, CurrentUserService>();
        services.AddSingleton<IApiHostService, ApiHostService>();
        services.AddSingleton<IWindowService, WindowService>();

        // =====================================================
        // API Clients
        // =====================================================

        services.AddHttpClient<IApiClient, ApiClient>(
            client =>
            {
                client.BaseAddress =
                    new Uri("http://localhost:5260/");

                client.Timeout =
                    TimeSpan.FromSeconds(30);
            });

        services.AddHttpClient<IAuthApiClient, AuthApiClient>(
            client =>
            {
                client.BaseAddress =
                    new Uri("http://localhost:5260/");

                client.Timeout =
                    TimeSpan.FromSeconds(30);
            });

        services.AddScoped<IApplicationsApiClient, ApplicationsApiClient>();
        services.AddScoped<IApplicationTypesApiClient, ApplicationTypesApiClient>();
        services.AddScoped<ICountriesApiClient, CountriesApiClient>();
        services.AddScoped<IDashboardApiClient, DashboardApiClient>();
        services.AddScoped<IDetainedLicensesApiClient, DetainedLicensesApiClient>();
        services.AddScoped<IDriversApiClient, DriversApiClient>();
        services.AddScoped<IInternationalLicensesApiClient, InternationalLicensesApiClient>();
        services.AddScoped<ILicenseClassesApiClient, LicenseClassesApiClient>();
        services.AddScoped<ILicenseIssuanceApiClient, LicenseIssuanceApiClient>();
        services.AddScoped<ILicenseRenewalApiClient, LicenseRenewalApiClient>();
        services.AddScoped<ILicenseReplacementApiClient, LicenseReplacementApiClient>();
        services.AddScoped<ILicensesApiClient, LicensesApiClient>();
        services.AddScoped<ILocalDrivingLicenseApplicationsApiClient, LocalDrivingLicenseApplicationsApiClient>();
        services.AddScoped<IPeopleApiClient, PeopleApiClient>();
        services.AddScoped<ITestAppointmentsApiClient, TestAppointmentsApiClient>();
        services.AddScoped<ITestTypesApiClient, TestTypesApiClient>();
        services.AddScoped<ITestsApiClient, TestsApiClient>();
        services.AddScoped<ITestWorkflowApiClient, TestWorkflowApiClient>();
        services.AddScoped<IUsersApiClient, UsersApiClient>();

        // =====================================================
        // ViewModels
        // =====================================================

        services.AddTransient<AddEditLDLAppViewModel>();
        services.AddTransient<AddEditPersonViewModel>();
        services.AddTransient<AddEditUserViewModel>();
        services.AddTransient<ApplicationTypeViewModel>();
        services.AddTransient<ChangePasswordViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<DetainLicenseViewModel>();
        services.AddTransient<DriversViewModel>();
        services.AddTransient<InternationalViewModel>();
        services.AddTransient<LDLAppViewModel>();
        services.AddTransient<LicenseHistoryViewModel>();
        services.AddTransient<ListDetainedLicensesViewModel>();
        services.AddTransient<LocalApplicationDetailsViewModel>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<NewInternationalLicenseApplicationViewModel>();
        services.AddTransient<PeopleViewModel>();
        services.AddTransient<ReleaseDetainedViewModel>();
        services.AddTransient<ReplacementDamagedLicenseViewModel>();
        services.AddTransient<RenewLicenseViewModel>();
        services.AddTransient<ScheduleTestViewModel>();
        services.AddTransient<TakeTestViewModel>();
        services.AddTransient<TestAppointmentViewModel>();
        services.AddTransient<TestTypeViewModel>();
        services.AddTransient<UpdateApplicationTypeViewModel>();
        services.AddTransient<UpdateTestTypeViewModel>();
        services.AddTransient<UsersViewModel>();

        // =====================================================
        // Windows
        // =====================================================

        services.AddTransient<AddEditPersonWin>();
        services.AddTransient<AddEditUserWin>();
        services.AddTransient<ChangePasswordWindow>();
        services.AddTransient<DetainLicenseWin>();
        services.AddTransient<EditApplicationTypeWindow>();
        services.AddTransient<EditTestTypeWindow>();
        services.AddTransient<LocalApplicationDetailsWin>();
        services.AddTransient<LoginWindow>();
        services.AddTransient<MainWindow>();
        services.AddTransient<NewInternationalLicenseApplicationWin>();
        services.AddTransient<NewLocalLicnnse>();
        services.AddTransient<ReleaseDetainedLicenseWin>();
        services.AddTransient<RenewLicenseApplicationWin>();
        services.AddTransient<ReplacementDamagedLicense>();
        services.AddTransient<ScheduleTestWin>();
        services.AddTransient<TakeTestWin>();
        services.AddTransient<TestAppointmentWin>();
        services.AddTransient<UserDetailsWindow>();

        // =====================================================
        // Pages
        // =====================================================

        services.AddTransient<DriversPage>();
        services.AddTransient<InterLAppPage>();
        services.AddTransient<LDLAppPage>();
        services.AddTransient<ManageApplicationTypePage>();
        services.AddTransient<ManageTestTypePage>();
        services.AddTransient<PeoplePage>();
        services.AddTransient<UserPage>();
        services.AddTransient<ListDetainedLicenses>();
    }
}