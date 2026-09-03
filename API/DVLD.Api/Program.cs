using System.Text;
using Application.Interfaces;
using Application.Options;
using Application.Services;
using DVLD.Api.Security;
using Infrastructure;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// JWT Configuration
// ============================================================

builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection(
        JwtOptions.SectionName));

var jwtOptions =
    builder.Configuration
        .GetSection(JwtOptions.SectionName)
        .Get<JwtOptions>()
    ?? throw new InvalidOperationException(
        "JWT configuration is missing.");

if (string.IsNullOrWhiteSpace(jwtOptions.SecretKey))
{
    throw new InvalidOperationException(
        "JWT SecretKey is not configured.");
}

if (Encoding.UTF8.GetByteCount(jwtOptions.SecretKey) < 32)
{
    throw new InvalidOperationException(
        "JWT SecretKey must be at least 32 bytes long.");
}

builder.Services.AddAuthentication(
        JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtOptions.Issuer,

                ValidateAudience = true,
                ValidAudience = jwtOptions.Audience,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(
                            jwtOptions.SecretKey)),

                ValidateLifetime = true,

                ClockSkew = TimeSpan.Zero
            };
    });

builder.Services.AddAuthorization();

builder.Services.AddHttpContextAccessor();


// ============================================================
// Database
// ============================================================

var connectionString =
    builder.Configuration.GetConnectionString(
        "DVLDConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DVLDConnection' was not found.");

builder.Services.AddDbContext<DVLDDbContext>(options =>
    options.UseSqlServer(connectionString));


// ============================================================
// Repositories
// ============================================================

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.AddScoped<IDashboardRepository, DashboardRepository>();
builder.Services.AddScoped<IApplicationRepository, ApplicationRepository>();
builder.Services.AddScoped<IApplicationTypeRepository, ApplicationTypeRepository>();
builder.Services.AddScoped<ICountryRepository, CountryRepository>();
builder.Services.AddScoped<IDetainedLicenseRepository, DetainedLicenseRepository>();
builder.Services.AddScoped<IDriverRepository, DriverRepository>();
builder.Services.AddScoped<ILicenseClassRepository, LicenseClassRepository>();
builder.Services.AddScoped<ILicenseRepository, LicenseRepository>();
builder.Services.AddScoped<ILocalDrivingLicenseApplicationRepository, LocalDrivingLicenseApplicationRepository>();
builder.Services.AddScoped<IPersonRepository, PersonRepository>();
builder.Services.AddScoped<ITestAppointmentRepository, TestAppointmentRepository>();
builder.Services.AddScoped<ITestRepository, TestRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ITestTypeRepository, TestTypeRepository>();
builder.Services.AddScoped<IInternationalRepository, InternationalRepository>();


// ============================================================
// Application Services
// ============================================================

builder.Services.AddScoped<IDashboardService, DashboardService>();

// API-specific current user implementation.
// Do NOT register Application.Services.CurrentUserService here.
builder.Services.AddScoped<ICurrentUserService, ApiCurrentUserService>();

builder.Services.AddScoped<IApplicationService, ApplicationService>();
builder.Services.AddScoped<IApplicationTypeService, ApplicationTypeService>();
builder.Services.AddScoped<ICountryService, CountryService>();
builder.Services.AddScoped<IDetainedLicenseService, DetainedLicenseService>();
builder.Services.AddScoped<IDriverService, DriverService>();
builder.Services.AddScoped<ILicenseClassService, LicenseClassService>();
builder.Services.AddScoped<ILocalDrivingLicenseApplicationService, LocalDrivingLicenseApplicationService>();
builder.Services.AddScoped<IPersonService, PersonService>();
builder.Services.AddScoped<ITestAppointmentService, TestAppointmentService>();
builder.Services.AddScoped<ITestService, TestService>();
builder.Services.AddScoped<ITestTypeService, TestTypeService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IInternationalService, InternationalService>();


// ============================================================
// Authentication Services
// ============================================================

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();


// ============================================================
// License Services
// ============================================================

builder.Services.AddScoped<ILicenseService, LicenseService>();
builder.Services.AddScoped<ILicenseRenewalService, LicenseRenewalService>();
builder.Services.AddScoped<ILicenseIssuanceService, LicenseIssuanceService>();
builder.Services.AddScoped<ILicenseReplacementService, LicenseReplacementService>();
builder.Services.AddScoped<ITestWorkflowService, TestWorkflowService>();
builder.Services.AddScoped<ILicenseQueryService, LicenseQueryService>();


// ============================================================
// Controllers
// ============================================================

builder.Services.AddControllers();


// ============================================================
// Swagger
// ============================================================

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


// ============================================================
// Build Application
// ============================================================

var app = builder.Build();


// ============================================================
// Development Tools
// ============================================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


// ============================================================
// HTTP Pipeline
// ============================================================

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();