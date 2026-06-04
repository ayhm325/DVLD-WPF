using Application.Interfaces;
using Application.Services;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Presentation.ViewModels;
using Presentation.Views;
using Presentation.Views.Pages;
using Presentation.Views.Windows;
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

            // 3. Services
            services.AddScoped<IPersonService, PersonService>();
            services.AddScoped<ICountryService, CountryService>();
            services.AddScoped<IUserService, UserService>();

            // 4. ViewModels
            services.AddTransient<PeopleViewModel>();
            services.AddTransient<PersonViewModel>();
            services.AddTransient<UsersViewModel>();
            services.AddTransient<AddEditUserViewModel>(); // تأكد من إضافته هنا

            // 5. Views (Pages & Windows)
            services.AddTransient<MainWindow>();
            services.AddTransient<HomePage>();           // كان مفقوداً
            services.AddTransient<UserPage>();
            services.AddTransient<AddEditUserPage>();    // كان مفقوداً
            services.AddTransient<SettingsPage>();       // إذا كنت تستخدمها
            services.AddTransient<PeoplePage>();         // إذا كنت تستخدمها
            services.AddTransient<UserDetailsWindow>();
            services.AddTransient<ChangePasswordViewModel>();

        }


    }
}