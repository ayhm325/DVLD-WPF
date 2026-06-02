using Application.Interfaces;
using Application.Services;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Presentation.ViewModels;
using Presentation.Views;
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
            // DbContext Factory
            services.AddDbContextFactory<Infrastructure.DVLDDbContext>(options =>
                options.UseSqlServer(ConnectionString));

            // Repositories
            services.AddScoped<PersonRepository>();
            services.AddScoped<CountryRepository>();

            // Services
            services.AddScoped<IPersonService, PersonService>();
            services.AddScoped<ICountryService, CountryService>();

            // ViewModels
            services.AddTransient<PeopleViewModel>();
            services.AddTransient<PersonViewModel>();

            // Views
            services.AddTransient<MainWindow>();
        }
    }
}