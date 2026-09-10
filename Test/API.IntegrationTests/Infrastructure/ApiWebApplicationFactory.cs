using Application.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace API.IntegrationTests.Infrastructure;

public sealed class ApiWebApplicationFactory
    : WebApplicationFactory<Program>
{
    public Mock<IAuthService> AuthServiceMock { get; } = new();

    public Mock<IUserService> UserServiceMock { get; } = new();

    public Mock<ICurrentUserService> CurrentUserServiceMock { get; } = new();

    public Mock<IPersonService> PersonServiceMock { get; } = new();

    public Mock<IDriverService> DriverServiceMock { get; } = new();

    public Mock<IApplicationTypeService> ApplicationTypeServiceMock { get; } = new();

    public Mock<ICountryService> CountryServiceMock { get; } = new();

    public Mock<ILicenseClassService> LicenseClassServiceMock { get; } = new();

    public Mock<ILicenseIssuanceService> LicenseIssuanceServiceMock { get; } = new();

    public Mock<ILicenseRenewalService> LicenseRenewalServiceMock { get; } = new();

    public Mock<ILicenseService> LicenseServiceMock { get; } = new();

    public Mock<IInternationalService> InternationalServiceMock { get; } = new();

    public Mock<ILicenseReplacementService> LicenseReplacementServiceMock { get; } = new();

    public Mock<ILocalDrivingLicenseApplicationService> LocalDrivingLicenseApplicationServiceMock { get; } = new();

    public Mock<ITestAppointmentService> TestAppointmentServiceMock { get; } = new();

    public Mock<ITestService> TestServiceMock { get; } = new();

    public Mock<IDetainedLicenseService> DetainedLicenseServiceMock { get; } = new();

    public Mock<IApplicationService> ApplicationServiceMock { get; } = new();

    public Mock<ITestTypeService> TestTypeServiceMock { get; } = new();

    public Mock<IDashboardService> DashboardServiceMock { get; } = new();

    public Mock<ITestWorkflowService> TestWorkflowServiceMock { get; } = new();

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IAuthService>();
            services.AddScoped(_ => AuthServiceMock.Object);

            services.RemoveAll<IUserService>();
            services.AddScoped(_ => UserServiceMock.Object);

            services.RemoveAll<ICurrentUserService>();
            services.AddScoped(_ => CurrentUserServiceMock.Object);

            services.RemoveAll<IPersonService>();
            services.AddScoped(_ => PersonServiceMock.Object);

            services.RemoveAll<IDriverService>();
            services.AddScoped(_ => DriverServiceMock.Object);

            services.RemoveAll<IApplicationTypeService>();
            services.AddScoped(_ => ApplicationTypeServiceMock.Object);

            services.RemoveAll<ICountryService>();
            services.AddScoped(_ => CountryServiceMock.Object);

            services.RemoveAll<ILicenseClassService>();
            services.AddScoped(_ => LicenseClassServiceMock.Object);

            services.RemoveAll<ILicenseIssuanceService>();
            services.AddScoped(_ => LicenseIssuanceServiceMock.Object);

            services.RemoveAll<ILicenseRenewalService>();
            services.AddScoped(_ => LicenseRenewalServiceMock.Object);

            services.RemoveAll<ILicenseService>();
            services.AddScoped(_ => LicenseServiceMock.Object);

            services.RemoveAll<IInternationalService>();
            services.AddScoped(_ => InternationalServiceMock.Object);

            services.RemoveAll<ILicenseReplacementService>();
            services.AddScoped(_ => LicenseReplacementServiceMock.Object);

            services.RemoveAll<ILocalDrivingLicenseApplicationService>();
            services.AddScoped(_ => LocalDrivingLicenseApplicationServiceMock.Object);

            services.RemoveAll<ITestAppointmentService>();
            services.AddScoped(_ => TestAppointmentServiceMock.Object);

            services.RemoveAll<ITestService>();
            services.AddScoped(_ => TestServiceMock.Object);

            services.RemoveAll<IDetainedLicenseService>();
            services.AddScoped(_ => DetainedLicenseServiceMock.Object);

            services.RemoveAll<IApplicationService>();
            services.AddScoped(_ => ApplicationServiceMock.Object);

            services.RemoveAll<ITestTypeService>();
            services.AddScoped(_ => TestTypeServiceMock.Object);

            services.RemoveAll<IDashboardService>();
            services.AddScoped(_ => DashboardServiceMock.Object);

            services.RemoveAll<ITestWorkflowService>();
            services.AddScoped(_ => TestWorkflowServiceMock.Object);

            services
                .AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions,
                    TestAuthenticationHandler>(
                    "Test",
                    _ =>
                    {
                    });
        });
    }
}