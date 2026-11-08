# DVLD — Driving & Vehicle License Department System

DVLD is a .NET 8 driving-license department management system with an ASP.NET Core Web API, SQL Server persistence, transactional business workflows, authentication/authorization, a WPF desktop client (`Presentation (WPF)`), a browser-based React frontend (`ClientApp`), and automated testing.

## Functional scope

The system manages:

- People and users
- Drivers
- Countries
- Applications and application types
- License classes
- Local driving-license applications
- Test types, appointments, and results
- First-license issuance
- License renewal and replacement
- License detention and release
- International licenses
- Dashboard statistics

## Architecture

```text
Presentation (WPF)          ClientApp (React)
        │                           │
        └─────────────┬─────────────┘
                      │ HTTP + JSON + JWT
                      v
                  DVLD.Api
                      │
                      v
                 Application
                      │
                      v
               Infrastructure
                      │
                      v
                 SQL Server
```

`DVLD.Contracts` is the shared .NET HTTP contract boundary between `DVLD.Api` and `Presentation (WPF)`. The WPF project does not reference the API project directly. `ClientApp` is a standalone React + TypeScript + Vite frontend that communicates with `DVLD.Api` over HTTP + JSON + JWT (`http://localhost:5260/api`) via the `"ReactApp"` CORS policy (`http://localhost:5173`).

## Solution structure

```text
Domain/
Application/
Infrastructure/
DVLD.Contracts/
API/DVLD.Api/
Presentation (WPF)/
  └── DVLD_WPF.sln
ClientApp/
  ├── README.md
  └── docs/
Test/
docs/
```

## Technology stack

- C# / .NET 8
- ASP.NET Core Web API
- Entity Framework Core 8
- SQL Server
- WPF (`Presentation (WPF)`) with `CommunityToolkit.Mvvm`
- React 19 + TypeScript + Vite (`ClientApp`)
- JWT bearer authentication
- ASP.NET Core policy-based authorization (`AdminOnly`, `StaffOnly`, `StaffOrAdmin`)
- BCrypt password hashing (`BCrypt.Net-Next`)
- Dependency Injection
- Repository + Unit of Work
- Result / Result<T> pattern
- DTOs and API contracts (`DVLD.Contracts`)
- HttpClientFactory (WPF) / Fetch API (React)
- Unit and integration testing (xUnit, Moq, `WebApplicationFactory`)
- Azure DevOps Pipelines

## Architectural characteristics

- Thin API controllers
- Application services own use-case orchestration
- Infrastructure owns EF Core and SQL Server persistence
- Shared scoped `DVLDDbContext` / `UnitOfWork` for transactional workflows
- Deferred physical commit, automatic rollback + `ChangeTracker.Clear()` on failure `Result`, and ambiguous-commit verification in `UnitOfWork`
- Serializable transactions for concurrency-sensitive workflows
- Database constraints (unique and filtered unique indexes) and `UPDLOCK` row-lock reads on base tables reinforce business invariants
- Backend authorization is the security boundary
- Client role-based UI is a usability mechanism, not the security boundary
- WPF Presentation is MVVM-oriented, not claimed to be pure MVVM
- React frontend (`ClientApp`) is under active iterative development and documented separately under [`ClientApp/README.md`](ClientApp/README.md) and [`ClientApp/AGENTS.md`](ClientApp/AGENTS.md)

## Run the API and Clients

### 1. Build and run the .NET API

From the repository root, pass `"Presentation (WPF)\DVLD_WPF.sln"` to solution-level `.NET` CLI commands:

```powershell
dotnet restore "Presentation (WPF)\DVLD_WPF.sln"
dotnet build "Presentation (WPF)\DVLD_WPF.sln" --configuration Release
dotnet run --project ".\API\DVLD.Api\DVLD.Api.csproj" --launch-profile http
```

The local HTTP profile runs at `http://localhost:5260`; current launch settings (`API/DVLD.Api/Properties/launchSettings.json`) are authoritative if the port changes.

### 2. Run the React frontend (`ClientApp`)

With `DVLD.Api` running at `http://localhost:5260`:

```powershell
cd ClientApp
npm install
npm run dev
```

See [`ClientApp/README.md`](ClientApp/README.md) and [`ClientApp/AGENTS.md`](ClientApp/AGENTS.md) for React setup, scripts, and frontend development standards.

## Run all .NET tests

```powershell
dotnet test "Presentation (WPF)\DVLD_WPF.sln" --configuration Release
```

Individual suites:

```powershell
dotnet test ".\Test\Application.UnitTests\Application.UnitTests.csproj" --configuration Release
dotnet test ".\Test\API.IntegrationTests\API.IntegrationTests.csproj" --configuration Release
dotnet test ".\Test\Infrastructure.IntegrationTests\Infrastructure.IntegrationTests.csproj" --configuration Release
```

## Documentation Hierarchy & System References

The DVLD repository maintains a strictly governed, streamlined documentation hierarchy:

### 1. Root Engineering Standards & Governance
- [**AI Engineering Governance** (`AGENTS.md`)](AGENTS.md) — Universal mandatory AI rules, safety gates, and Git protocols.
- [**AI Engineering Instructions** (`AI-ENGINEERING-INSTRUCTIONS.md`)](AI-ENGINEERING-INSTRUCTIONS.md) — Mandatory 10-step engineering workflow.
- [**Engineering Standards** (`ENGINEERING-STANDARDS.md`)](ENGINEERING-STANDARDS.md) — Architectural invariants, UnitOfWork, Jordan Time, and concurrency rules.
- [**Security Standards** (`SECURITY-STANDARDS.md`)](SECURITY-STANDARDS.md) — Authentication architecture, SoD (ADR-O01 Model B), and profile photo upload protection.
- [**Testing Standards** (`TESTING-STANDARDS.md`)](TESTING-STANDARDS.md) — Test isolation, ephemeral database lifecycle (`DVLD_IntegrationTests_<GUID>`), and CI rules.
- [**Technology Stack** (`TECHNOLOGY-STACK.md`)](TECHNOLOGY-STACK.md) — Verified inventory of frameworks, package manifests, and dependencies.
- [**Architectural Decisions** (`ENGINEERING-DECISIONS.md`)](ENGINEERING-DECISIONS.md) — Formal Architectural Decision Records (ADRs).
- [**Engineering Roadmap** (`ENGINEERING-ROADMAP.md`)](ENGINEERING-ROADMAP.md) — Prioritized remediation backlog and evidence tracking.

### 2. Session Continuity Suite (`docs/continuity/`)
- [**Project Charter & Continuity** (`docs/continuity/PROJECT-CHARTER-AND-CONTINUITY.md`)](docs/continuity/PROJECT-CHARTER-AND-CONTINUITY.md) — Operational charter, authority model, and continuity framework.
- [**Current Project State** (`docs/continuity/CURRENT-PROJECT-STATE.md`)](docs/continuity/CURRENT-PROJECT-STATE.md) — Point-in-time snapshot of codebase health and active milestones.
- [**Correction Log** (`docs/continuity/CORRECTION-LOG.md`)](docs/continuity/CORRECTION-LOG.md) — Immutable chronological log of corrections.
- [**Session Logs** (`docs/continuity/SESSION-LOGS/`)](docs/continuity/SESSION-LOGS/README.md) — Session handoff log conventions.

### 3. Modernization Development Planning (`docs/development/`)
- [**Development Vision** (`docs/development/DEVELOPMENT-VISION.md`)](docs/development/DEVELOPMENT-VISION.md) — Modernization objectives, civic capabilities, and target user experience.
- [**Development Plan** (`docs/development/DEVELOPMENT-PLAN.md`)](docs/development/DEVELOPMENT-PLAN.md) — Phased workstreams, milestones, licensing business constraints, and 3D findings register.
- [**Target Architecture** (`docs/development/TARGET-ARCHITECTURE.md`)](docs/development/TARGET-ARCHITECTURE.md) — Clean Architecture layers, API contracts, React client patterns, and media storage.
- [**Risks & Open Decisions** (`docs/development/RISKS-AND-OPEN-DECISIONS.md`)](docs/development/RISKS-AND-OPEN-DECISIONS.md) — Centralized catalog of unresolved decisions, risks, and scoped workstream blockers.

### 4. Foundational Engineering Discovery Reports (`docs/audits/`)
- [**Phase A Discovery Report** (`docs/audits/phase-a-repository-discovery.md`)](docs/audits/phase-a-repository-discovery.md) — Deep architectural baseline discovery (Commit `29e43d6`).
- [**Phase A.1 Supplementary Review** (`docs/audits/phase-a-supplementary-review.md`)](docs/audits/phase-a-supplementary-review.md) — 11 detailed workflow analyses, UnitOfWork deep analysis, and SQL locking.

### 5. Frontend Development & Governance (`ClientApp/`)
- [**ClientApp Overview & Setup** (`ClientApp/README.md`)](ClientApp/README.md) — Vite SPA setup, scripts, and feature baseline summary.
- [**Frontend Agent Governance** (`ClientApp/AGENTS.md`)](ClientApp/AGENTS.md) — Frontend AI instructions, TypeScript standards, and design system rules.

## Test inventory and verification status

### Current source & VSTest inventory (commit `4f75658`)

| Suite | Source Methods (`[Fact]+[Theory]`) | Source Test Cases (`[Fact]+[InlineData]`) | VSTest Discovered (`--list-tests`) | Audit Execution Outcome |
|---|---:|---:|---:|---|
| `Application.UnitTests` | 603 | 642 | 633 | Executed (`--no-build`): **633 Passed, 0 Failed, 0 Skipped** (~5 s) |
| `API.IntegrationTests` | 362 | 442 | 435 | Discovered (`435` entries); not executed against live SQL Server during audit |
| `Infrastructure.IntegrationTests` | 469 | 507 | 494 | Discovered (`494` entries); not executed against live SQL Server during audit |
| **Total** | **1,434** | **1,591** | **1,562** | `Application.UnitTests` passed; integration suites enumerated only |

Discovered VSTest counts (`435` and `494`) represent test discovery only and are not claimed as passing test runs. During the audit inside a network-isolated sandbox, `dotnet build` failed (`NU1900` / sandbox MSBuild restriction), so `--no-build` execution and discovery used existing `bin/Debug/net8.0` assemblies. Those assemblies were built on 2026-10-26 (12:39–15:21), before commits `b5dc945`, `3056f88`, and `61cbdcc` added 29 source test cases; this is why the VSTest counts (`1,562`) are lower than the source attribute counts at `HEAD` (`1,591`). Source counts are static attribute counts, not executed test results; only the `633` `Application.UnitTests` entries were actually executed.

### Historical full test run result

The latest complete full-suite result reported during earlier development was:

| Suite | Passed | Failed | Skipped |
|---|---:|---:|---:|
| Application.UnitTests | 631 | 0 | 0 |
| API.IntegrationTests | 364 | 0 | 0 |
| Infrastructure.IntegrationTests | 492 | 0 | 0 |
| **Total** | **1487** | **0** | **0** |

These are historical user-reported results, not a claim that the documentation update freshly executed all integration tests against SQL Server.

## Continuous Integration

The Azure DevOps pipeline (`azure-pipelines.yml`) currently provides Continuous Integration for the `.NET` solution (`"Presentation (WPF)/DVLD_WPF.sln"`):

```text
main push
    ↓
.NET 8 SDK
    ↓
Restore
    ↓
Release Build
    ↓
Tests
    ↓
Publish Test Results
```

`azure-pipelines.yml` does not currently include build or lint steps for `ClientApp/`, and no production deployment/CD stage is claimed by the current documentation.

## Documentation principle

Implemented behavior, verified test behavior, and future improvements are kept distinct. When source behavior changes, update the relevant documentation in the same change.
