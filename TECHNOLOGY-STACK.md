# Technology Stack & Dependency Inventory

> **Authority Level:** Authoritative Technical Inventory  
> **Parent Document:** [`AGENTS.md`](./AGENTS.md)  
> **Repository Root:** `D:\DVLD-WPF`  
> **Baseline Commit:** `29e43d6ce26ff6e7f516c45f4f475b10bdc2bfd3`  

---

## 1. Overview and Principles

This document inventories the runtime environments, frameworks, libraries, build tools, and CI/CD configurations verified within the DVLD repository. 

All version records in this document are derived from static inspection of configuration files (`global.json`, `*.csproj`, `package.json`, `azure-pipelines.yml`). AI agents and engineers must **not** upgrade, downgrade, or add packages without explicit written approval and an approved Architecture Decision Record.

---

## 2. Verified Backend Technology Stack (.NET & C#)

### 2.1 Core Runtime & SDK
- **.NET SDK Version:** `8.0.427` (Pinned in `global.json` at repository root).
- **Target Framework:** `net8.0` (Configured across all backend projects: `Domain`, `Application`, `Infrastructure`, `DVLD.Api`, `DVLD.Contracts`, and `Test/*`).
- **Language Version:** C# 12 (Default for .NET 8 SDK; nullable reference types `<Nullable>enable</Nullable>` enforced).

### 2.2 Backend Package Manifest Inventory

| Package / Library | Declared Version | Configuration File | Layer / Project | Purpose / Role | Status |
|---|---|---|---|---|---|
| **Microsoft.AspNetCore.App** | Framework Reference (`8.0`) | Implicit | `DVLD.Api` | Web API hosting, routing, middleware, controllers | **Verified Active** |
| **Microsoft.EntityFrameworkCore** | `8.0.18` | `Infrastructure.csproj` | `Infrastructure` | Object-Relational Mapping (ORM) data access | **Verified Active** |
| **Microsoft.EntityFrameworkCore.SqlServer** | `8.0.0` | `Infrastructure.csproj` | `Infrastructure` | SQL Server database provider for EF Core | **Discrepancy (See Section 4.1)** |
| **Microsoft.EntityFrameworkCore.Design** | `8.0.0` | `Infrastructure.csproj`, `DVLD.Api.csproj` | Infrastructure / API | Migration tooling and design-time scaffolding | **Verified Active** |
| **Microsoft.AspNetCore.Authentication.JwtBearer** | `8.0.19` | `DVLD.Api.csproj` | `DVLD.Api` | JWT token authentication handler & claims validation | **Verified Active** |
| **BCrypt.Net-Next** | `4.2.0` | `Application.csproj` | `Application` | Secure password hashing (WorkFactor = 11) | **Verified Active** |
| **SkiaSharp** | `4.154.0` | `Application.csproj` | `Application` | Server-side image resizing and profile photo processing | **Verified Active** |
| **Swashbuckle.AspNetCore** | `6.6.2` | `DVLD.Api.csproj` | `DVLD.Api` | Swagger UI and OpenAPI 3.0 document generation | **Verified Active** |
| **CommunityToolkit.Mvvm** | `8.2.2` | `Presentation.csproj` | `Presentation (WPF)` | MVVM messaging, observable objects, relay commands | **Frozen / Excluded Scope** |

---

## 3. Verified Frontend Technology Stack (React SPA)

The web frontend resides in `ClientApp/` and is built independently using Node.js and npm.

### 3.1 Core Runtime & Frameworks
- **Package Manager:** `npm` (Configured via `package.json` and `package-lock.json`).
- **Node.js Compatibility:** Node.js 20+ LTS recommended.
- **UI Library:** `React 19.3.1` and `React-DOM 19.3.1`.
- **Language / Transpiler:** `TypeScript 6.0.3` (`tsconfig.json`, strict mode enabled).
- **Bundler & Dev Server:** `Vite 8.3.4` (`vite.config.ts`, Fast Refresh, proxy configurations).
- **Styling Engine:** `Tailwind CSS 4.3.3` with `@tailwindcss/vite`.

### 3.2 Key Frontend Libraries (`ClientApp/package.json`)

| Package | Declared Version | Purpose | Status |
|---|---|---|---|
| **@radix-ui/react-*** | `^1.x` – `^2.x` | Accessible, unstyled UI primitives (Dialog, Dropdown, Tabs, Popover) | **Verified Active** |
| **lucide-react** | `1.65.0` | Enterprise iconography | **Verified Active** |
| **clsx** / **tailwind-merge** | `2.1.1` / `3.5.0` | Conditional class manipulation and Tailwind conflict resolution | **Verified Active** |
| **eslint** | `10.13.0` | Static code analysis and TypeScript linting | **Verified Active** |
| **@vitejs/plugin-react** | `5.0.0` | Official Vite React plugin for Babel/Fast Refresh | **Verified Active** |

---

## 4. Test Frameworks & Tooling

Backend testing is partitioned across three projects in `Test/`:

| Package | Declared Version | Test Project | Role |
|---|---|---|---|
| **xunit** | `2.5.3` / `2.9.2` | All Test Projects | Primary test framework and test runner |
| **xunit.runner.visualstudio** | `2.5.3` / `2.8.2` | All Test Projects | Visual Studio and `dotnet test` test adapter |
| **Moq** | `4.20.72` | `Application.UnitTests` | Interface isolation and behavioral mocking |
| **Microsoft.AspNetCore.Mvc.Testing** | `8.0.0` | `API.IntegrationTests` | In-memory API hosting (`WebApplicationFactory`) |
| **Microsoft.NET.Test.Sdk** | `17.8.0` / `17.11.1` | All Test Projects | MSBuild test host infrastructure |
| **coverlet.collector** | `6.0.0` / `6.0.2` | All Test Projects | Code coverage metrics collection |

---

## 5. Continuous Integration & Pipeline Stack

### 5.1 Pipeline Configuration (`azure-pipelines.yml`)
- **CI Platform:** Azure Pipelines (`windows-latest` VM image).
- **Database Service:** Dynamic SQL Server LocalDB instance (`DVLD_CI_$(Build.BuildId)_$(System.JobAttempt)`).
- **Build Step:** `dotnet build Presentation (WPF)/DVLD_WPF.sln --configuration Release`.
- **Test Execution Steps (Configured in YAML — Runtime Status: Needs Verification):**
  - `Application.UnitTests`: Configured in YAML.
  - `Infrastructure.IntegrationTests`: Configured in YAML with `DVLD_TEST_CONNECTION`.
  - `API.IntegrationTests`: Configured in YAML with `DVLD_TEST_CONNECTION`.
- **Historical Evidence vs. Current Pipeline Status:**
  - The static YAML pipeline configuration specifies steps for all three test suites. However, current automated CI execution and end-to-end pass status cannot be proven from static YAML inspection alone and is classified as **Needs Verification**.
  - Historical Phase 12.1 test execution records confirm that while `Application.UnitTests` (919 passed) and `Infrastructure.IntegrationTests` (611 passed, 1 SSL timeout failure) were executed, `API.IntegrationTests` was cataloged (436 tests) with only 1 smoke test actually executed and passed; 435 tests were cataloged but remained unexecuted. Discovered tests must never be conflated with executed tests.

---

## 6. Discrepancies, Unverified Compatibility & Planned Technologies

### 6.1 Discovered Entity Framework Core Version Discrepancy
In `Infrastructure/Infrastructure.csproj`:
- `Microsoft.EntityFrameworkCore` is declared at version **`8.0.18`**.
- `Microsoft.EntityFrameworkCore.SqlServer` is declared at version **`8.0.0`**.
- `Microsoft.EntityFrameworkCore.Design` is declared at version **`8.0.0`**.

> **Compatibility Status: UNVERIFIED (Pending Runtime Restoration)**  
> While minor patch variations within EF Core 8.x often interoperate cleanly, semantic version alignment (`8.0.18` across all EF packages) is Microsoft's recommended practice. Binary compatibility and dependency resolution cannot be conclusively confirmed until a clean `dotnet restore` is executed and `project.assets.json` is inspected.  
> **Policy:** Do **not** silently align this version during documentation phases. Any dependency update requires explicit authorization and validation under Phase 12.

### 6.2 Application Layer Clean Architecture Purity (BCrypt & SkiaSharp)
- `BCrypt.Net-Next` and `SkiaSharp` are referenced directly in `Application.csproj`.
- Pure Clean Architecture strictly isolates external library implementations in `Infrastructure`.
- *Finding Assessment:* BCrypt and SkiaSharp are CPU-bound algorithms with zero I/O or database dependencies. They do not introduce infrastructure coupling. Decoupling them behind interfaces (`IPasswordHasher`, `IImageProcessor`) is documented as a future architectural improvement (P3, Phase 12.5), not an urgent defect.

### 6.3 Planned Technologies & Architectural Boundaries
The following technologies are **NOT** part of the current repository and must **NOT** be introduced without an approved Architecture Decision Record:
- **MediatR / CQRS libraries** (The project utilizes explicit application services).
- **Redis or external caching layers** (In-memory caching is handled in-process).
- **Dapper or alternative micro-ORMs** (EF Core 8 and raw SQL queries via EF Core are standard).
- **Docker / Kubernetes manifests** (Currently target Windows Azure Pipelines and LocalDB).