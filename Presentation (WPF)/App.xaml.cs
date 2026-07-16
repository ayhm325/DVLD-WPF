using Application.Interfaces;
using Application.Services;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
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
            services.AddScoped<ApplicationRepository>();
            services.AddScoped<ApplicationTypeRepository>();
            services.AddScoped<CountryRepository>();
            services.AddScoped<DetainedLicenseRepository>();
            services.AddScoped<DriverRepository>();
            services.AddScoped<LicenseClassRepository>();
            services.AddScoped<LicenseRepository>();
            services.AddScoped<LocalDrivingLicenseApplicationRepository>();
            services.AddScoped<PersonRepository>();
            services.AddScoped<TestAppointmentRepository>();
            services.AddScoped<TestRepository>();
            services.AddScoped<UserRepository>();           
            services.AddScoped<TestTypeRepository>();
            services.AddScoped<InternationalRepository>();


            // 3. Services
            services.AddSingleton<IWindowService, WindowService>();
            services.AddScoped<IApplicationService, ApplicationService>();
            services.AddScoped<IApplicationTypeService, ApplicationTypeService>();
            services.AddScoped<ICountryService, CountryService>();
            services.AddSingleton<ICurrentUserService, CurrentUserService>();
            services.AddScoped<IDetainedLicenseService, DetainedLicenseService>();
            services.AddScoped<IDriverService, DriverService>();
            services.AddScoped<ILicenseClassService, LicenseClassService>();
            services.AddScoped<ILicenseService, LicenseService>();
            services.AddScoped<ILocalDrivingLicenseApplicationService, LocalDrivingLicenseApplicationService>();
            services.AddScoped<IPersonService, PersonService>();
            services.AddScoped<ITestAppointmentService, TestAppointmentService>();
            services.AddScoped<ITestService, TestService>();
            services.AddScoped<ITestTypeService, TestTypeService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IInternationalService, InternationalService>();


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



            // 5. Views (Pages & Windows)
            services.AddTransient<LoginWindow>();
            services.AddTransient<MainWindow>();
            services.AddTransient<HomePage>();           
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


            // 6. Navigation Service (⚠️ مهم)
            services.AddSingleton<Presentation.Services.INavigationService,
                                  Presentation.Services.NavigationService>();

        }


    }
}