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

            _rootServiceProvider = services.BuildServiceProvider(
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
        // 1. APPLICATION / PRESENTATION SERVICES
        // =====================================================

        services.AddSingleton<IWindowService, WindowService>();
        services.AddSingleton<ICurrentUserSession, CurrentUserService>();
        services.AddSingleton<IApiHostService, ApiHostService>();

        // =====================================================
        // 2. API CLIENTS
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

        services.AddScoped<IUsersApiClient, UsersApiClient>();
        services.AddScoped<IPeopleApiClient, PeopleApiClient>();
        services.AddScoped<ICountriesApiClient, CountriesApiClient>();
        services.AddScoped<IApplicationTypesApiClient, ApplicationTypesApiClient>();
        services.AddScoped<IApplicationsApiClient, ApplicationsApiClient>();
        services.AddScoped<ILicenseClassesApiClient, LicenseClassesApiClient>();
        services.AddScoped<ILocalDrivingLicenseApplicationsApiClient, LocalDrivingLicenseApplicationsApiClient>();
        services.AddScoped<ILicensesApiClient, LicensesApiClient>();
        services.AddScoped<IDriversApiClient, DriversApiClient>();
        services.AddScoped<IInternationalLicensesApiClient, InternationalLicensesApiClient>();
        services.AddScoped<ITestAppointmentsApiClient, TestAppointmentsApiClient>();
        services.AddScoped<ILicenseIssuanceApiClient, LicenseIssuanceApiClient>();
        services.AddScoped<ITestsApiClient, TestsApiClient>();
        services.AddScoped<ITestWorkflowApiClient, TestWorkflowApiClient>();
        services.AddScoped<IDetainedLicensesApiClient, DetainedLicensesApiClient>();
        services.AddScoped<ITestTypesApiClient, TestTypesApiClient>();
        services.AddScoped<ILicenseRenewalApiClient, LicenseRenewalApiClient>();
        services.AddScoped<ILicenseReplacementApiClient, LicenseReplacementApiClient>();

        services.AddScoped<IDashboardApiClient, DashboardApiClient>();

        // =====================================================
        // 3. VIEW MODELS
        // =====================================================

        services.AddTransient<AddEditLDLAppViewModel>();
        services.AddTransient<AddEditPersonViewModel>();
        services.AddTransient<AddEditUserViewModel>();
        services.AddTransient<ApplicationTypeViewModel>();
        services.AddTransient<ChangePasswordViewModel>();
        services.AddTransient<LDLAppViewModel>();
        services.AddTransient<LocalApplicationDetailsViewModel>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<PeopleViewModel>();
        services.AddTransient<ScheduleTestViewModel>();
        services.AddTransient<TakeTestViewModel>();
        services.AddTransient<TestAppointmentViewModel>();
        services.AddTransient<TestTypeViewModel>();
        services.AddTransient<UpdateApplicationTypeViewModel>();
        services.AddTransient<UpdateTestTypeViewModel>();
        services.AddTransient<UsersViewModel>();

        // services.AddTransient<IssueDrivingLicenseForTheFirstTimeViewModel>();

        services.AddTransient<LicenseHistoryViewModel>();
        services.AddTransient<DriversViewModel>();
        services.AddTransient<InternationalViewModel>();
        services.AddTransient<NewInternationalLicenseApplicationViewModel>();
        services.AddTransient<RenewLicenseViewModel>();
        services.AddTransient<ReplacementDamagedLicenseViewModel>();
        services.AddTransient<ListDetainedLicensesViewModel>();
        services.AddTransient<DetainLicenseViewModel>();
        services.AddTransient<ReleaseDetainedViewModel>();

        services.AddTransient<DashboardViewModel>();

        // =====================================================
        // 4. VIEWS
        // =====================================================

        services.AddTransient<LoginWindow>();
        services.AddTransient<MainWindow>();
        services.AddTransient<UserPage>();
        services.AddTransient<DriversPage>();
        services.AddTransient<ChangePasswordWindow>();
        services.AddTransient<PeoplePage>();
        services.AddTransient<UserDetailsWindow>();
        services.AddTransient<ManageApplicationTypePage>();
        services.AddTransient<EditApplicationTypeWindow>();
        services.AddTransient<ManageTestTypePage>();
        services.AddTransient<EditTestTypeWindow>();
        services.AddTransient<NewLocalLicnnse>();
        services.AddTransient<LDLAppPage>();
        services.AddTransient<AddEditPersonWin>();
        services.AddTransient<AddEditUserWin>();
        services.AddTransient<LocalApplicationDetailsWin>();
        services.AddTransient<TestAppointmentWin>();
        services.AddTransient<ScheduleTestWin>();
        services.AddTransient<TakeTestWin>();

        // services.AddTransient<IssueDrivingLicenseForTheFirstTimeWin>();
        // services.AddTransient<LicenseHistoryWin>();

        services.AddTransient<NewInternationalLicenseApplicationWin>();
        services.AddTransient<InterLAppPage>();
        services.AddTransient<RenewLicenseApplicationWin>();
        services.AddTransient<ReplacementDamagedLicense>();
        services.AddTransient<ListDetainedLicenses>();
        services.AddTransient<DetainLicenseWin>();
        services.AddTransient<ReleaseDetainedLicenseWin>();
    }
}