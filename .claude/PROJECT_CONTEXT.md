# Kursserver — Project Context

## Project Overview
ASP.NET Core 9.0 Minimal API backend ("Kursserver" = course server in Swedish).
Serves a React SPA frontend from `wwwroot/` in Release builds.
GitHub: https://github.com/Badwolfpup/Kursserver

## Key Architecture
- **Endpoints/** — static classes with `Map*Endpoints(this WebApplication app)` extension methods
- **Models/** — EF Core entities: User, Post, Project, Exercise, Attendance, Thread, Message, ThreadView, BugReport, Booking, NoClass, RecurringEvent, RecurringEventException, ExerciseHistory, ProjectHistory
- **Dto/** — Add/Update/Fetch DTOs per entity
- **Utils/** — ApplicationDbContext, EmailService, HasAdminPriviligies, FromClaims
- Role enum (User.cs): Admin=1, Teacher=2, Coach=3, Student=4, Guest=5

## Auth Pattern
- `HasAdminPriviligies.IsTeacher(context, roleLevel)` — returns `null` if authorized, `IResult` error if not; Admin + Teacher pass
- `HasAdminPriviligies.IsTeacher(context, roleLevel, 1)` — 3-param overload; Admin + Teacher + **Coach** pass
- `FromClaims.GetUserId(context)` — reads `id` claim from JWT
- Passwordless email login: `/api/email-validation` → passcode → `/api/passcode-validation` → JWT
- Coach-student link: `User.CoachId` (nullable FK) points to the coach assigned to that student — used for ownership checks in booking endpoints

## React Frontend
- Lives in a **separate repo** at `C:\Users\adam_\source\repos\kurshemsida` (sibling folder)
- Added to `.gitignore` in Kursserver repo
- `.csproj` copies Vite dist to `wwwroot/` on Release builds via `ReactAppPath` MSBuild property
- Relative path set: `<ReactAppPath>..\kurshemsida</ReactAppPath>`

## Important Conventions
- Dev server: `localhost:5001`, Swagger at `/swagger` (Dev only)

## SCENARIO Comment Convention
Add XML doc comments above every `app.Map*` endpoint that was added or modified. Used by `/static-trace` to verify code matches intent.

```csharp
/// <summary>
/// SCENARIO: One sentence — who does what
/// CALLS: useHookName() → serviceName.method() (kurshemsida)
/// SIDE EFFECTS:
///   - Sets Field = value on Model
///   - Sends email if EmailNotifications = true (EmailService)
///   - Does NOT create X (useful to document non-obvious absence)
/// </summary>
app.MapPost("/api/route", [Authorize] async (...) =>
```

Skip service methods and helpers — only endpoint registrations get these comments.

## Messaging System (MessageEndpoints.cs)
- Thread model: `User1Id < User2Id` enforced for uniqueness, `StudentContextId` nullable (null = direct DM, N = about-student)
- Student-context threads: one per (coach, studentContextId) pair — looked up by coachId, not exact user pair
- Visibility: admins/teachers see ALL student-context threads; coaches see only their own students' threads; students see only direct DMs
- `ApplyThreadVisibilityFilter`: shared helper that enforces role-based thread filtering (students: no student-context, coaches: own students only, admins/teachers: unfiltered)
- Email notifications on new messages: respects `EmailNotifications` flag, skipped in dev environment

## Known Gotchas
- `update-user` endpoint: auth check must use caller's claims, not the ContactId's role
- Old ticket system was replaced by messaging (Thread/Message) — TicketEndpoints.cs no longer exists
- `*.pubxml` and `appsettings.json` are gitignored — must be copied manually on new publish machines
- `AllowedOrigin` key must be set in production `appsettings.json` (e.g. `"https://culprogrammering.se"`) — server throws `InvalidOperationException` on startup if missing
- `wwwroot/` must exist as an empty folder locally in dev — gitignored, not created on clone
