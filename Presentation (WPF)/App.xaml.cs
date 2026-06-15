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
            services.AddScoped<PersonRepository>();
            services.AddScoped<CountryRepository>();
            services.AddScoped<UserRepository>();
            services.AddScoped<ApplicationTypeRepository>();
            services.AddScoped<TestTypeRepository>();
            services.AddScoped<LicenseClassRepository>();
            services.AddScoped<ApplicationRepository>();
            services.AddTransient<LocalDrivingLicenseApplicationRepository>();
            services.AddTransient<LicenseRepository>();

            // 3. Services
            services.AddSingleton<ICurrentUserService, CurrentUserService>();
            services.AddScoped<IPersonService, PersonService>();
            services.AddScoped<ICountryService, CountryService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IApplicationTypeService, ApplicationTypeService>();
            services.AddScoped<ITestTypeService, TestTypeService>();
            services.AddScoped<ILicenseClassService, LicenseClassService>();
            services.AddTransient<ILocalDrivingLicenseApplicationService, LocalDrivingLicenseApplicationService>();
            services.AddScoped<IApplicationService, ApplicationService>();
            services.AddSingleton<IWindowService, WindowService>();
            services.AddScoped<ILicenseService, LicenseService>();

            // 4. ViewModels
            services.AddTransient<LoginViewModel>();       
            services.AddTransient<AddEditLDLAppViewModel>();
            services.AddTransient<AddEditUserViewModel>();
            services.AddTransient<ApplicationTypeViewModel>();
            services.AddTransient<ChangePasswordViewModel>();
            services.AddTransient<LDLAppViewModel>();
            services.AddTransient<PeopleViewModel>();
            services.AddTransient<AddEditPersonViewModel>();
            services.AddTransient<TestTypeViewModel>();
            services.AddTransient<UpdateApplicationTypeViewModel>();
            services.AddTransient<UpdateTestTypeViewModel>();
            services.AddTransient<UsersViewModel>();
            services.AddTransient<LocalApplicationDetailsViewModel>();

            // 5. Views (Pages & Windows)
            services.AddTransient<LoginWindow>();
            services.AddTransient<MainWindow>();
            services.AddTransient<HomePage>();           
            services.AddTransient<UserPage>();            
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



            // 6. Navigation Service (⚠️ مهم)
            services.AddSingleton<Presentation.Services.INavigationService,
                                  Presentation.Services.NavigationService>();

        }


    }
}