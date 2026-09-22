# DVLD — Driving & Vehicle License Department System

A multi-layered .NET 8 application for managing people, applications, driving-license workflows, tests, licenses, detained licenses, international licenses, drivers, and users.

## Documentation

- [Complete System Documentation](docs/SYSTEM-DOCUMENTATION.md)
- [Architecture](docs/ARCHITECTURE.md)
- [Database](docs/DATABASE.md)
- [API Reference](docs/API.md)
- [Business Workflows](docs/BUSINESS-WORKFLOWS.md)
- [Testing](docs/TESTING.md)
- [Security](docs/SECURITY.md)

## Solution Structure

```text
DVLD-WPF/
├── Domain/
├── Application/
├── Infrastructure/
├── DVLD.Contracts/
├── API/
│   └── DVLD.Api/
├── Presentation (WPF)/
├── Test/
└── docs/
```

## Technology

- C# / .NET 8
- ASP.NET Core Web API
- Entity Framework Core 8
- SQL Server
- WPF / MVVM
- JWT authentication
- Policy-based authorization
- Dependency Injection
- Repository + Unit of Work
- DTOs and Result Pattern
- Unit and integration testing
- Azure DevOps Pipeline

## Running

```powershell
dotnet restore
dotnet build --configuration Release
dotnet run --project ".\API\DVLD.Api\DVLD.Api.csproj" --launch-profile http
```

Run all tests:

```powershell
dotnet test --configuration Release
```

> Documentation rule: describe implemented behavior only. If a behavior cannot be confirmed from source code, mark it **Needs verification**.
