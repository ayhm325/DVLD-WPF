using Application.Interfaces;
using Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Presentation;
using Presentation.ViewModels;
using Presentation.Views;
using Presentation.Views.Pages;
using Presentation.Views.Pages.Applications;
using Presentation.Views.Pages.Tests;
using Presentation.Views.Windows;
using Presentation.Views.Windows.Applications;
using Presentation.Views.Windows.Tests;
using System.Windows;
using Infrastructure;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DVLD_WPF
{
    public partial class App : System.Windows.Application
    {
        private const string ConnectionString =
            "Server=.;Database=DVLDf;Trusted_Connection=True;TrustServerCertificate=True";

        public static IServiceProvider ServiceProvider { get; private set; } = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var services = new ServiceCollection();
            ConfigureServices(services);

            ServiceProvider = services.BuildServiceProvider();

            var loginWindow = ServiceProvider.GetRequiredService<LoginWindow>();
            loginWindow.Show();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // 1. DbContext Factory
            services.AddDbContextFactory<Infrastructure.DVLDDbContext>(options =>
                options.UseSqlServer(ConnectionString));

            // 2. Repositories
            services.AddTransient<IDashboardRepository, DashboardRepository>();
            services.AddTransient<IApplicationRepository, ApplicationRepository>();
            services.AddTransient<IApplicationTypeRepository, ApplicationTypeRepository>();
            services.AddTransient<ICountryRepository, CountryRepository>();
            services.AddTransient<IDetainedLicenseRepository, DetainedLicenseRepository>();
            services.AddTransient<IDriverRepository, DriverRepository>();
            services.AddTransient<ILicenseClassRepository, LicenseClassRepository>();
            services.AddTransient<ILicenseRepository, LicenseRepository>();
            services.AddTransient<ILocalDrivingLicenseApplicationRepository, LocalDrivingLicenseApplicationRepository>();
            services.AddTransient<IPersonRepository, PersonRepository>();
            services.AddTransient<ITestAppointmentRepository, TestAppointmentRepository>();
            services.AddTransient<ITestRepository, TestRepository>();
            services.AddTransient<IUserRepository, UserRepository>();
            services.AddTransient<ITestTypeRepository, TestTypeRepository>();
            services.AddTransient<IInternationalRepository, InternationalRepository>();


            // 3. Services
            services.AddTransient<IDashboardService, DashboardService>();
            services.AddSingleton<IWindowService, WindowService>();
            services.AddTransient<IApplicationService, ApplicationService>();
            services.AddTransient<IApplicationTypeService, ApplicationTypeService>();
            services.AddTransient<ICountryService, CountryService>();
            services.AddSingleton<ICurrentUserService, CurrentUserService>();
            services.AddTransient<IDetainedLicenseService, DetainedLicenseService>();
            services.AddTransient<IDriverService, DriverService>();
            services.AddTransient<ILicenseClassService, LicenseClassService>();           
            services.AddTransient<ILocalDrivingLicenseApplicationService, LocalDrivingLicenseApplicationService>();
            services.AddTransient<IPersonService, PersonService>();
            services.AddTransient<ITestAppointmentService, TestAppointmentService>();
            services.AddTransient<ITestService, TestService>();
            services.AddTransient<ITestTypeService, TestTypeService>();
            services.AddTransient<IUserService, UserService>();
            services.AddTransient<IInternationalService, InternationalService>();
            // =========================================================
            services.AddTransient<ILicenseService, LicenseService>();
            services.AddScoped<ILicenseRenewalService,LicenseRenewalService>();
            services.AddScoped<ILicenseIssuanceService, LicenseIssuanceService>();
            services.AddScoped<ILicenseReplacementService,LicenseReplacementService>();
            //=========================================================
            services.AddScoped<ITestWorkflowService, TestWorkflowService>();
            // ========================================================
            services.AddScoped<ILicenseQueryService, LicenseQueryService>();


            // 4. ViewModels
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
            services.AddTransient<IssueDrivingLicenseForTheFirstTimeViewModel>();
            services.AddTransient<LicenseHistoryViewModel>();
            services.AddTransient<DriversViewModel>();
            services.AddTransient<InternationalViewModel>();
            services.AddTransient<NewInternationalLicenseApplicationViewModel>();
            services.AddTransient<RenewLicenseViewModel>();
            services.AddTransient<ReplacementDamagedLicenseViewModel>();
            services.AddTransient<ListDetainedLicensesViewModel>();
            services.AddTransient<DetainLicenseViewModel>();
            services.AddTransient<ReleaseDetainedViewModel>();

            // 5. Views (Pages & Windows)
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
            services.AddTransient<IssueDrivingLicenseForTheFirstTimeWin>();
            services.AddTransient<LicenseHistoryWin>();
            services.AddTransient<NewInternationalLicenseApplicationWin>();
            services.AddTransient<InterLAppPage>();
            services.AddTransient<RenewLicenseApplicationWin>();
            services.AddTransient<ReplacementDamagedLicense>();
            services.AddTransient<ListDetainedLicenses>();
            services.AddTransient<DetainLicenseWin>();
            services.AddTransient<ReleaseDetainedLicenseWin>();


            // 6. Navigation Service (⚠️ مهم)
            services.AddSingleton<Presentation.Services.INavigationService,
                                  Presentation.Services.NavigationService>();

        }


    }
}