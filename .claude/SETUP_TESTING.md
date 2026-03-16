# Testing Infrastructure Setup — Kursserver

This document describes the testing infrastructure that must be in place for pre-merge reviews to work correctly. If you're setting up a fresh clone or the infrastructure is missing, follow these steps.

## Prerequisites

- .NET 9 SDK installed
- Solution restored (`dotnet restore`)

## Step 1: Verify analyzer packages

Open `Kursserver/Kursserver.csproj` and confirm these two PackageReferences exist:

```xml
<PackageReference Include="Meziantou.Analyzer" Version="*">
  <PrivateAssets>all</PrivateAssets>
</PackageReference>
<PackageReference Include="SonarAnalyzer.CSharp" Version="*">
  <PrivateAssets>all</PrivateAssets>
</PackageReference>
```

These are compile-time-only analyzers (`PrivateAssets=all`) — they don't ship with the app. They will produce new warnings in existing code — **do not fix them now**. They catch issues in new code going forward and get fixed per-branch during pre-merge Step 0.

**What they catch:**
- **Meziantou**: string comparison culture, async patterns, security pitfalls, null handling
- **SonarAnalyzer**: code smells, cognitive complexity, dead code, common bug patterns

## Step 2: Verify test project packages

Open `Kursserver.Tests/Kursserver.Tests.csproj` and confirm these PackageReferences exist alongside the existing xUnit/FluentAssertions/Moq references:

```xml
<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="9.*" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="9.*" />
```

- **Mvc.Testing** — provides `WebApplicationFactory<Program>` for in-process HTTP testing
- **EF Core SQLite** — in-memory database for integration tests (replaces SQL Server in test context)

## Step 3: Verify Program.cs accessibility

Open `Kursserver/Program.cs` and confirm this line exists at the very end of the file:

```csharp
public partial class Program { }
```

This makes the implicit `Program` class accessible to `WebApplicationFactory<Program>` in the test project. Without it, integration tests won't compile.

## Step 4: Verify integration test scaffolding

Check that `Kursserver.Tests/Integration/` exists and contains these three files:

### `CustomWebApplicationFactory.cs`

A `WebApplicationFactory<Program>` subclass that:
- Removes the existing SQL Server `DbContextOptions<ApplicationDbContext>` registration
- Adds SQLite in-memory (`DataSource=:memory:`) as replacement
- Replaces JWT Bearer auth with `TestAuthHandler`
- Calls `EnsureCreated()` to set up the schema
- Disposes the SQLite connection on cleanup

Key details:
- DbContext class: `ApplicationDbContext` (namespace `Kursserver.Utils`)
- The SQLite connection must be kept open for the lifetime of the factory (in-memory DB disappears when connection closes)

### `TestAuthHandler.cs`

An `AuthenticationHandler<AuthenticationSchemeOptions>` that:
- Uses scheme name `"TestScheme"`
- Returns a configurable `ClaimsPrincipal` via a static `Claims` property
- Default claims: `NameIdentifier = "test-user-id"`, `Name = "Test User"`, `Role = "Admin"`
- Tests can override `TestAuthHandler.Claims` before making requests to test different roles

### `IntegrationTestBase.cs`

An abstract base class that:
- Implements `IClassFixture<CustomWebApplicationFactory>` (shared factory per test class)
- Provides `CreateClient()` helper
- Implements `IDisposable`

### If any files are missing

Create them. Reference the existing files in the repo for the exact implementation, or use the descriptions above to write them from scratch. The key contract:
- Factory swaps SQL Server for SQLite and JWT for test auth
- Base class provides a client wired to the test server
- Tests override `TestAuthHandler.Claims` to simulate different users/roles

## Step 5: Restore and verify

```bash
dotnet restore
dotnet build
dotnet test
```

- **Build**: will succeed with analyzer warnings (expected — do not fix now)
- **Tests**: all existing unit tests should pass. Integration scaffolding is inert until actual integration tests are written.

## What NOT to do

- **Do not fix existing analyzer warnings.** They are pre-existing and get fixed per-branch during pre-merge Step 0.
- **Do not write endpoint integration tests yet.** Those are written during future pre-merge runs when Step 3.5 detects untested endpoints.
- **Do not remove or modify `TestAuthHandler.Claims` defaults** — they're designed to give integration tests authenticated admin access by default.

## How this fits together

The pre-merge pipeline (`/pre-merge`) uses this infrastructure:
1. **Step 0 (Lint/Analyze)** — runs `dotnet build --no-restore` which triggers Meziantou + SonarAnalyzer; fixes errors only in changed files
2. **Step 2 (Build and test)** — runs `dotnet build` then `dotnet test`
3. **Step 3.5 (Integration tests)** — runs `dotnet test --filter Category=Integration` if integration tests exist
4. **Step 4 (Unit tests)** — xUnit + FluentAssertions, tests live in `Kursserver.Tests/`
