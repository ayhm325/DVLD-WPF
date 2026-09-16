using System.Text;
using Application.Interfaces;
using Application.Options;
using Application.Services;
using DVLD.Api.Security;
using Infrastructure;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IValidateOptions<JwtOptions>, JwtOptionsValidator>();

builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .ValidateOnStart();

var jwtOptions = builder.Configuration
    .GetSection(JwtOptions.SectionName)
    .Get<JwtOptions>()
    ?? throw new InvalidOperationException("JWT configuration is missing.");

if (string.IsNullOrWhiteSpace(jwtOptions.SecretKey))
    throw new InvalidOperationException("JWT SecretKey is required.");

if (Encoding.UTF8.GetByteCount(jwtOptions.SecretKey) < 32)
    throw new InvalidOperationException("JWT SecretKey must be at least 32 bytes long.");

if (string.IsNullOrWhiteSpace(jwtOptions.Issuer))
    throw new InvalidOperationException("JWT Issuer is required.");

if (string.IsNullOrWhiteSpace(jwtOptions.Audience))
    throw new InvalidOperationException("JWT Audience is required.");

if (jwtOptions.ExpirationMinutes <= 0)
    throw new InvalidOperationException("JWT ExpirationMinutes must be greater than zero.");

var key = new SymmetricSecurityKey(
    Encoding.UTF8.GetBytes(jwtOptions.SecretKey));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,

            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,

            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(
        "AdminOnly",
        policy => policy.RequireRole("Admin"));

    options.AddPolicy(
        "StaffOnly",
        policy => policy.RequireRole("Staff"));
});

builder.Services.AddHttpContextAccessor();

var connectionString =
    builder.Configuration.GetConnectionString("DVLDConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DVLDConnection' was not found.");

builder.Services.AddDbContext<DVLDDbContext>(
    options => options.UseSqlServer(
        connectionString,
        sql => sql.EnableRetryOnFailure()));

// Unit of Work
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Repositories
builder.Services.AddScoped<IDashboardRepository, DashboardRepository>();
builder.Services.AddScoped<IApplicationRepository, ApplicationRepository>();
builder.Services.AddScoped<IApplicationTypeRepository, ApplicationTypeRepository>();
builder.Services.AddScoped<ICountryRepository, CountryRepository>();
builder.Services.AddScoped<IDetainedLicenseRepository, DetainedLicenseRepository>();
builder.Services.AddScoped<IDriverRepository, DriverRepository>();
builder.Services.AddScoped<ILicenseClassRepository, LicenseClassRepository>();
builder.Services.AddScoped<ILicenseRepository, LicenseRepository>();
builder.Services.AddScoped<ILocalDrivingLicenseApplicationRepository,
    LocalDrivingLicenseApplicationRepository>();
builder.Services.AddScoped<IPersonRepository, PersonRepository>();
builder.Services.AddScoped<ITestAppointmentRepository, TestAppointmentRepository>();
builder.Services.AddScoped<ITestRepository, TestRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ITestTypeRepository, TestTypeRepository>();
builder.Services.AddScoped<IInternationalRepository, InternationalRepository>();

// Application Services
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<ICurrentUserService, ApiCurrentUserService>();
builder.Services.AddScoped<IApplicationService, ApplicationService>();
builder.Services.AddScoped<IApplicationTypeService, ApplicationTypeService>();
builder.Services.AddScoped<ICountryService, CountryService>();
builder.Services.AddScoped<IDetainedLicenseService, DetainedLicenseService>();
builder.Services.AddScoped<IDriverService, DriverService>();
builder.Services.AddScoped<ILicenseClassService, LicenseClassService>();
builder.Services.AddScoped<ILocalDrivingLicenseApplicationService,
    LocalDrivingLicenseApplicationService>();
builder.Services.AddScoped<IPersonService, PersonService>();
builder.Services.AddScoped<ITestAppointmentService, TestAppointmentService>();
builder.Services.AddScoped<ITestService, TestService>();
builder.Services.AddScoped<ITestTypeService, TestTypeService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IInternationalService, InternationalService>();

// Authentication & License Workflows
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<ILicenseService, LicenseService>();
builder.Services.AddScoped<ILicenseRenewalService, LicenseRenewalService>();
builder.Services.AddScoped<ILicenseIssuanceService, LicenseIssuanceService>();
builder.Services.AddScoped<ILicenseReplacementService, LicenseReplacementService>();
builder.Services.AddScoped<ITestWorkflowService, TestWorkflowService>();
builder.Services.AddScoped<ILicenseQueryService, LicenseQueryService>();

// API
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "DVLD API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token. Example: Bearer eyJhbGciOi..."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }