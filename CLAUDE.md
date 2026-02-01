# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Editing Rules

When replacing or removing code, always comment out the original code instead of deleting it.

## Build & Run Commands

```bash
# Build
dotnet build Kursserver.sln

# Run (Development - launches with Swagger UI)
dotnet run --project Kursserver/Kursserver.csproj

# Run in Release mode (copies React frontend build to wwwroot)
dotnet run --project Kursserver/Kursserver.csproj -c Release

# Add a new EF Core migration
dotnet ef migrations add <MigrationName> --project Kursserver/Kursserver.csproj

# Apply database migrations
dotnet ef database update --project Kursserver/Kursserver.csproj
```

Development server runs on `localhost:5001`. Swagger UI is available at `/swagger` in Development mode only.

There are no unit tests or linting configured in this project.

## Architecture

ASP.NET Core 9.0 Minimal API backend for a course management platform (Swedish: "Kursserver"). Serves a React SPA frontend from `wwwroot/` in production.

### Project Layout

- **Endpoints/** - Static classes with extension methods that register API routes on `WebApplication` (e.g., `app.MapUserEndpoints()`). Each file covers one domain: Users, Posts, Projects, Exercises, Attendance, Permissions, Validation, Utilities.
- **Models/** - EF Core entity classes: `User`, `Post`, `Project`, `Exercise`, `Permission`, `Attendance`. The `Role` enum (Admin=1, Teacher=2, Coach=3, Student=4, Guest=5) lives in `User.cs`.
- **Dto/** - Separate DTOs for Add/Update/Fetch operations per entity.
- **Utils/** - `ApplicationDbContext` (EF Core DbContext), `EmailService` (MailKit SMTP), `HasAdminPriviligies` (authorization checks), `FromClaims` (JWT claim extraction).
- **Migrations/** - EF Core migration history.

### Key Patterns

**Endpoint registration:** Each endpoint file is a static class exposing a `Map*Endpoints(this WebApplication app)` extension method, called from `Program.cs`.

**Authorization:** `HasAdminPriviligies.IsTeacher(context, roleLevel)` returns `null` if authorized, or an `IResult` error if not. Admins and Teachers pass; others get 403.

**Authentication flow:** Email-based passwordless login. User requests a passcode via `/api/email-validation`, receives it by email (or in the response body during Development), then validates at `/api/passcode-validation` to receive a JWT. The JWT contains `id`, `email`, and `role` claims.

**User identity extraction:** `FromClaims.GetUserId(context)` reads the `id` claim from the JWT.

### Database

SQL Server via EF Core. Key relationships configured in `ApplicationDbContext.OnModelCreating`:
- `User.Coach` - self-referential FK, `NoAction` on delete
- `Permission` - one-to-one with User, cascade delete
- `Post.User` - FK with `SetNull` on delete
- `Attendance.User` - cascade delete

### Frontend Integration

In Release builds, the `.csproj` copies the Vite build output from an external React project (`dist/`) into `wwwroot/`. A fallback handler serves `index.html` for SPA routes (paths without file extensions) in non-Development environments.
