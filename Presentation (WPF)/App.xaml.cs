using Application.Interfaces;
using Application.Services;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Presentation.ViewModels;
using Presentation.Views;
using Presentation.Views.Pages;
using Presentation.Views.Pages.Applications;
using Presentation.Views.Windows;
using Presentation.Views.Windows.Applications;
using Presentation.Views.Windows.Tests;
using Presentation.Views.Pages.Tests;
using System;
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

            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
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

            // 3. Services
            services.AddScoped<IPersonService, PersonService>();
            services.AddScoped<ICountryService, CountryService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IApplicationTypeService, ApplicationTypeService>();
            services.AddScoped<ITestTypeService, TestTypeService>();

            // 4. ViewModels
            services.AddTransient<PeopleViewModel>();
            services.AddTransient<PersonViewModel>();
            services.AddTransient<UsersViewModel>();
            services.AddTransient<AddEditUserViewModel>();
            services.AddTransient<ApplicationTypeViewModel>();
            services.AddTransient<UpdateApplicationTypeViewModel>();
            services.AddTransient<TestTypeViewModel>();
            services.AddTransient<UpdateTestTypeViewModel>();


            // 5. Views (Pages & Windows)
            services.AddTransient<MainWindow>();
            services.AddTransient<HomePage>();           
            services.AddTransient<UserPage>();
            services.AddTransient<AddEditUserPage>();    
            services.AddTransient<SettingsPage>();       
            services.AddTransient<PeoplePage>();         
            services.AddTransient<UserDetailsWindow>();
            services.AddTransient<ChangePasswordViewModel>();
            services.AddTransient<ManageApplicationTypePage>();
            services.AddTransient<EditApplicationTypeWindow>();
            services.AddTransient<ManageTestTypePage>();
            services.AddTransient<EditTestTypeWindow>();



        }


    }
}