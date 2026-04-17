# Kursserver — Project Context

## Project Overview
ASP.NET Core 9.0 Minimal API backend ("Kursserver" = course server in Swedish).
Serves a React SPA frontend from `wwwroot/` in Release builds.
GitHub: https://github.com/Badwolfpup/Kursserver

## Tech Stack
- **Framework:** ASP.NET Core 9.0 (Minimal APIs, no controllers)
- **ORM:** Entity Framework Core 9.0 + SQL Server
- **Auth:** JWT Bearer tokens stored in HttpOnly cookies
- **Email:** Resend.com (primary), MailKit SMTP (fallback)
- **AI:** Anthropic (Claude Haiku 4.5), DeepSeek, Grok (xAI)
- **Testing:** xUnit + FluentAssertions
- **Target:** net9.0, nullable enabled, implicit usings

## Project Structure
```
Kursserver/
  Endpoints/        # Minimal API endpoint files (18 files)
  Models/            # EF Core entity models (17 files)
  Dto/               # Request/response DTOs (36 files)
  Utils/             # Services, helpers, DbContext
  Migrations/        # EF Core migrations
  wwwroot/           # Static files + React build output
  Program.cs         # App startup, DI, middleware

Kursserver.Tests/    # xUnit test project
  Dto/
  Helpers/
  Parsers/
  Utils/
```

## Endpoint Pattern
All endpoints use Minimal API extension methods registered in `Program.cs`:
```csharp
app.MapBookingEndpoints();   // in BookingEndpoints.cs
app.MapUserEndpoints();      // in UserEndpoints.cs
// ...18 total endpoint files
```

Each file defines a static class with `Map*Endpoints(this WebApplication app)`.

## All Endpoint Files & Routes

| File | Key Routes |
|------|-----------|
| ValidationEndpoints.cs | POST email-validation, POST passcode-validation, GET me, POST logout |
| UserEndpoints.cs | POST add-user, DELETE delete-user, PUT update-user, PUT update-student-profile, PUT update-my-settings, GET fetch-users, PUT update-activity |
| PostEndpoints.cs | GET/POST/PUT/DELETE posts, POST upload-image |
| ExerciseEndpoints.cs | GET/POST/PUT/DELETE exercises, GET exercise-history, POST exercise-feedback |
| ProjectEndpoints.cs | GET/POST/PUT/DELETE projects, GET project-history, POST project-feedback |
| BookingEndpoints.cs | GET /api/bookings (role-filtered), POST /api/bookings, PUT /api/bookings/{id}/status, PUT /api/bookings/{id}/cancel, PUT /api/bookings/{id}/transfer, PUT /api/bookings/{id}/reschedule |
| AvailabilityEndpoints.cs | GET/POST/PUT/DELETE /api/availability (decorative overlays only — no booking interaction) |
| BusyTimeEndpoints.cs | GET/POST/PUT/DELETE /api/busy-time (no overlap checks — admin resolves visually) |
| RecurringEventEndpoints.cs | GET/POST/PUT/DELETE recurring-events, POST/PUT recurring-events/{id}/exceptions |
| MessageEndpoints.cs | GET/POST threads, GET threads/{id}, POST threads/{id}/messages, PUT threads/{id}/view |
| AttendanceEndpoints.cs | GET weekly-attendance/{date}/{count}, PUT update-attendance |
| NoClassEndpoints.cs | GET noclass, POST noclass/{date} |
| BugReportEndpoints.cs | GET/POST/DELETE bug-reports |
| AnthropicEndpoints.cs | POST anthropic/exercise-asserts, exercise-feedback, project, project-feedback |
| DeepSeekEndpoints.cs | POST deepseek/* (same pattern as Anthropic) |
| GrokEndpoints.cs | POST grok/* (same pattern as Anthropic) |
| HelpbotEndpoints.cs | POST helpbot/chat |
| UtillityEndpoints.cs | GET get-week/{date}/{count} |

## Auth Pattern
- **Login flow:** Email → passcode (6-digit, sent via Resend) → JWT cookie
- **JWT claims:** sub (email), id (user ID), role (Admin/Teacher/Coach/Student/Guest)
- **Token expiry:** Admin/Teacher 30 days, Coach/Student 6 days, Guest 1 hour
- **Cookie:** `jwt`, HttpOnly, Secure, SameSite=Strict
- **Lockout:** 15 minutes after 10 failed passcode attempts (in-memory ConcurrentDictionary)
- `HasAdminPriviligies.IsTeacher(context, roleLevel)` — returns `null` if authorized, `IResult` error if not; Admin + Teacher pass
- `HasAdminPriviligies.IsTeacher(context, roleLevel, 1)` — 3-param overload; Admin + Teacher + **Coach** pass
- `FromClaims.GetUserId(context)` — reads `id` claim from JWT
- Coach-student link: `User.CoachId` (nullable FK) points to the coach assigned to that student

## Domain Models

| Model | Key Fields | Relationships |
|-------|-----------|---------------|
| User | id, firstName, lastName, email, authLevel, isActive, coachId, contactId, schedule bools, emailNotifications, telephone | Coach (self-ref), Contact (self-ref), Permission (1:1) |
| Permission | id, userId, html/css/js/variables/conditionals/loops/functions/arrays/objects (all bool) | User (1:1, cascade) |
| Post | id, html, delta, publishedAt, author, pinned | User (nullable, SetNull) |
| Project | id, title, description, html, css, js, difficulty, projectType | — |
| Exercise | id, title, description, js, expectedResult, difficulty, exerciseType, clues, goodToKnow | — |
| Booking | id, adminId, coachId, studentId, startTime, endTime, status (enum), meetingType (enum), note, seen, reason, rescheduledBy (nullable enum), createdByRole (enum) | Admin, Coach, Student |
| AdminAvailability | id, adminId, startTime, endTime | Admin (decorative calendar overlay — does not gate bookings) |
| RecurringEvent | id, name, weekday, startTime, endTime, frequency (enum), startDate, adminId, classroom, createdAt | Admin |
| BusyTime | id, adminId, startTime, endTime, note | Admin |
| RecurringEventException | id, recurringEventId, date, name, startTime, endTime, isDeleted | RecurringEvent (cascade) |
| Thread | id, user1Id, user2Id, studentContextId, createdAt, updatedAt | User1, User2, StudentContext (unique constraint) |
| Message | id, threadId, senderId, content, createdAt | Thread (cascade), Sender |
| ThreadView | id, threadId, userId, lastViewedAt | Thread, User |
| Attendance | id, userId, date | User (cascade) |
| ExerciseHistory | id, userId, topic, language, difficulty, title, description, solution, asserts, isCompleted, feedback fields | User |
| ProjectHistory | id, userId, techStack, difficulty, title, starterHtml, solutionHtml/Css/Js, isCompleted, feedback fields | User |
| NoClass | id, date | — |
| BugReport | id, type, content, senderId, createdAt | Sender |

Role enum (User.cs): Admin=1, Teacher=2, Coach=3, Student=4, Guest=5

Booking-domain enums (stored as PascalCase strings via EF `HasConversion<string>()`):
- `BookingStatus`: Pending | Accepted | Declined
- `MeetingType`: Intro | Followup | Other
- `BookingActor` (used for CreatedByRole + RescheduledBy): Admin | Coach
- `RecurringFrequency`: Weekly | Biweekly

## Services

| Service | Purpose |
|---------|---------|
| EmailService | Resend.com (ResendEmailAsync) + MailKit SMTP fallback. From: noreply@culprogrammering.net |
| AnthropicService | Claude Haiku 4.5 completions (claude-haiku-4-5-20251001) |
| DeepSeekService | DeepSeek API completions |
| GrokService | Grok/xAI completions (grok-code-fast-1, grok-3-mini-fast) |
| BookingNotifier | Email notifications for booking lifecycle (created, status changed, cancelled, rescheduled, transferred). Swedish language. Respects EmailNotifications flag. Coach↔Admin only (no student paths). |
| ConflictDetection | `CheckAcceptedBookingConflicts` — returns accepted-status bookings overlapping a time range. Pendings intentionally not reported (they coexist). |
| RecurringEventExpander | Expands recurring events into instances for a date range, respects exceptions and NoClass dates |
| ExercisePromptTemplates | Prompt templates for AI exercise generation |
| ProjectPromptTemplates | Prompt templates for AI project generation |
| ExerciseResponseParser | Parses AI exercise response into structured data |
| ProjectResponseParser | Parses AI project response into structured data |

## Middleware Stack (Program.cs order)
1. DefaultFiles + StaticFiles (wwwroot/)
2. HTTPS redirect
3. CORS (dev: any origin; prod: culprogrammering.se only)
4. Authentication (JWT Bearer)
5. Authorization
6. Fallback → index.html (SPA routing)

## Messaging System (MessageEndpoints.cs)
- Thread model: `User1Id < User2Id` enforced for uniqueness, `StudentContextId` nullable (null = direct DM, N = about-student)
- Student-context threads: one per (coach, studentContextId) pair — looked up by coachId, not exact user pair
- Visibility: admins/teachers see ALL student-context threads; coaches see only their own students' threads; students see only direct DMs
- `ApplyThreadVisibilityFilter`: shared helper that enforces role-based thread filtering
- Email notifications on new messages: respects `EmailNotifications` flag, skipped in dev environment

## AI Integration
- Three parallel providers (Anthropic, DeepSeek, Grok) with identical endpoint patterns
- Prompt templates for exercises & projects accept topic, language/tech-stack, difficulty, and recent history
- Response parsers extract structured data from AI output
- History tracked in ExerciseHistory/ProjectHistory tables

## Error Handling
- Endpoints use try-catch returning `Results.BadRequest/NotFound/Conflict/Problem`
- 409 Conflict for bookings is **Accepted-overlap only** (hard block). Pending overlaps and busy-time/recurring overlaps are not gated at the backend — admin resolves manually or frontend warns coaches
- No global exception middleware
- Fire-and-forget email can fail silently

## SCENARIO Comment Convention
Add XML doc comments above every `app.Map*` endpoint that was added or modified:
```csharp
/// <summary>
/// SCENARIO: One sentence — who does what
/// CALLS: useHookName() → serviceName.method() (kurshemsida)
/// SIDE EFFECTS:
///   - Sets Field = value on Model
///   - Sends email if EmailNotifications = true (EmailService)
/// </summary>
app.MapPost("/api/route", [Authorize] async (...) =>
```
Skip service methods and helpers — only endpoint registrations get these comments.

## Configuration
- Connection: SQL Server (site4now.net production, local dev with Integrated Security)
- Email: Resend API key + SMTP fallback (noreply@culprogrammering.se)
- AI keys: Anthropic, DeepSeek, Grok in appsettings.json
- JWT: Key, Issuer, Audience in Jwt section
- CORS: `AllowedOrigin` key required in production

## React Frontend
- Lives in a **separate repo** at `C:\Users\adam_\source\repos\kurshemsida` (sibling folder)
- `.csproj` copies Vite dist to `wwwroot/` on Release builds via `ReactAppPath` MSBuild property

## Naming Conventions
- Endpoints: `Map{Feature}Endpoints` extension methods
- DTOs: `Add{Resource}Dto`, `Update{Resource}Dto`, `{Action}Request/Response`
- Models: PascalCase, singular (User, Booking, Thread)
- Routes: `/api/{resource}` or `/api/{verb}-{resource}`

## Known Gotchas
- `update-user` endpoint: auth check must use caller's claims, not the ContactId's role
- Old ticket system was replaced by messaging — TicketEndpoints.cs no longer exists
- `*.pubxml` and `appsettings.json` are gitignored — must be copied manually on new publish machines
- `AllowedOrigin` key must be set in production appsettings.json — server throws on startup if missing
- `wwwroot/` must exist as an empty folder locally in dev — gitignored, not created on clone
- Dev server: `localhost:5001`, Swagger at `/swagger` (Dev only)
- **JSON enum serialization**: uses both Minimal APIs and Controllers. `JsonStringEnumConverter` must be configured on **both** `AddControllers().AddJsonOptions(...)` and `ConfigureHttpJsonOptions(...)` — missing either breaks one surface. Same applies to `PropertyNamingPolicy = JsonNamingPolicy.CamelCase`.
- **Booking approval flow**: every new booking starts `Status=Pending` regardless of creator. Reschedule also returns to Pending + stamps RescheduledBy. Availability does not gate bookings — it's a decorative overlay. Busy-time does not block or cancel bookings.

## Unit Tests
- `Kursserver.Tests` covers pure helpers/parsers only (RecurringEventExpander, HasAdminPriviligies, FromClaims, exercise/project parsers)
- No HTTP endpoint integration tests
- New endpoints only need tests if they extract pure logic into a static helper method
