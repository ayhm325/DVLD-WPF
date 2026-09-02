using Application.Interfaces;
using Application.Services;
using Infrastructure;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Presentation;
using Presentation.Services;
using Presentation.ViewModels;
using Presentation.Views;
using Presentation.Views.Pages;
using Presentation.Views.Pages.Applications;
using Presentation.Views.Pages.Tests;
using Presentation.Views.Windows;
using Presentation.Views.Windows.Applications;
using Presentation.Views.Windows.Tests;
using System.Windows;

namespace DVLD_WPF
{
    public partial class App : System.Windows.Application
    {
        private const string ConnectionString =
            "Server=.;Database=DVLDf;Trusted_Connection=True;TrustServerCertificate=True";

        private IServiceProvider _rootServiceProvider = null!;
        private IServiceScope _applicationScope = null!;

        public static IServiceProvider ServiceProvider { get; private set; } = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var services = new ServiceCollection();

            ConfigureServices(services);

            _rootServiceProvider = services.BuildServiceProvider(
                new ServiceProviderOptions
                {
                    ValidateScopes = true,
                    ValidateOnBuild = true
                });

            // Create a scope for the WPF application.
            _applicationScope = _rootServiceProvider.CreateScope();

            // Expose the scoped provider to the Presentation layer.
            ServiceProvider = _applicationScope.ServiceProvider;

            var loginWindow =
                ServiceProvider.GetRequiredService<LoginWindow>();

            loginWindow.Show();
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
            // 1. DATABASE
            // =====================================================

            services.AddDbContextFactory<DVLDDbContext>(
                options =>
                    options.UseSqlServer(ConnectionString));

            // =====================================================
            // 2. UNIT OF WORK
            // =====================================================

            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // =====================================================
            // 3. REPOSITORIES
            // =====================================================

            services.AddScoped<IDashboardRepository, DashboardRepository>();
            services.AddScoped<IApplicationRepository, ApplicationRepository>();
            services.AddScoped<IApplicationTypeRepository, ApplicationTypeRepository>();
            services.AddScoped<ICountryRepository, CountryRepository>();
            services.AddScoped<IDetainedLicenseRepository, DetainedLicenseRepository>();
            services.AddScoped<IDriverRepository, DriverRepository>();
            services.AddScoped<ILicenseClassRepository, LicenseClassRepository>();
            services.AddScoped<ILicenseRepository, LicenseRepository>();
            services.AddScoped<ILocalDrivingLicenseApplicationRepository, LocalDrivingLicenseApplicationRepository>();
            services.AddScoped<IPersonRepository, PersonRepository>();
            services.AddScoped<ITestAppointmentRepository, TestAppointmentRepository>();
            services.AddScoped<ITestRepository, TestRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<ITestTypeRepository, TestTypeRepository>();
            services.AddScoped<IInternationalRepository, InternationalRepository>();

            // =====================================================
            // 4. APPLICATION SERVICES
            // =====================================================

            services.AddSingleton<IWindowService, WindowService>();
            services.AddSingleton<ICurrentUserService, CurrentUserService>();

            services.AddScoped<IDashboardService, DashboardService>();
            services.AddScoped<IApplicationService, ApplicationService>();
            services.AddScoped<IApplicationTypeService, ApplicationTypeService>();
            services.AddScoped<ICountryService, CountryService>();
            services.AddScoped<IDetainedLicenseService, DetainedLicenseService>();
            services.AddScoped<IDriverService, DriverService>();
            services.AddScoped<ILicenseClassService, LicenseClassService>();
            services.AddScoped<ILocalDrivingLicenseApplicationService, LocalDrivingLicenseApplicationService>();
            services.AddScoped<IPersonService, PersonService>();
            services.AddScoped<ITestAppointmentService, TestAppointmentService>();
            services.AddScoped<ITestService, TestService>();
            services.AddScoped<ITestTypeService, TestTypeService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IInternationalService, InternationalService>();

            // =====================================================
            // 5. LICENSE SERVICES
            // =====================================================

            services.AddScoped<ILicenseService, LicenseService>();
            services.AddScoped<ILicenseRenewalService, LicenseRenewalService>();
            services.AddScoped<ILicenseIssuanceService, LicenseIssuanceService>();
            services.AddScoped<ILicenseReplacementService, LicenseReplacementService>();
            services.AddScoped<ITestWorkflowService, TestWorkflowService>();
            services.AddScoped<ILicenseQueryService, LicenseQueryService>();

            // =====================================================
            // 6. VIEW MODELS
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
            //services.AddTransient<IssueDrivingLicenseForTheFirstTimeViewModel>();
            services.AddTransient<LicenseHistoryViewModel>();
            services.AddTransient<DriversViewModel>();
            services.AddTransient<InternationalViewModel>();
            services.AddTransient<NewInternationalLicenseApplicationViewModel>();
            services.AddTransient<RenewLicenseViewModel>();
            services.AddTransient<ReplacementDamagedLicenseViewModel>();
            services.AddTransient<ListDetainedLicensesViewModel>();
            services.AddTransient<DetainLicenseViewModel>();
            services.AddTransient<ReleaseDetainedViewModel>();

            // =====================================================
            // 7. VIEWS
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
            //services.AddTransient<IssueDrivingLicenseForTheFirstTimeWin>();
            //services.AddTransient<LicenseHistoryWin>();
            services.AddTransient<NewInternationalLicenseApplicationWin>();
            services.AddTransient<InterLAppPage>();
            services.AddTransient<RenewLicenseApplicationWin>();
            services.AddTransient<ReplacementDamagedLicense>();
            services.AddTransient<ListDetainedLicenses>();
            services.AddTransient<DetainLicenseWin>();
            services.AddTransient<ReleaseDetainedLicenseWin>();

            
        }
    }
}