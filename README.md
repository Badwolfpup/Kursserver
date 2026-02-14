# Kursserver

The backend API for a full-stack Learning Management System (LMS) built to manage a programming course. This server powers the [kurshemsida](https://github.com/Badwolfpup/kurshemsida) frontend and is **deployed in production with real students using it daily**.

## About

Built to solve a real need as a coding instructor - managing students, exercises, projects, attendance, and course content in one place. The server also integrates AI (Anthropic Claude and DeepSeek) to auto-generate coding exercises and project suggestions.

## Technologies

- **Framework:** ASP.NET Core (Minimal APIs)
- **Database:** Entity Framework Core with code-first migrations (20+ migrations)
- **Auth:** JWT authentication with email-based passcode validation
- **AI:** Anthropic Claude API & DeepSeek API with prompt templates and response parsers
- **Real-time:** Serves Vite-built React frontend from wwwroot

## Features

- **AI-Generated Content** - Auto-generate coding exercises and project suggestions using Claude and DeepSeek with customized prompt templates
- **Student Management** - User registration, permissions, profile management
- **Exercise System** - Create, manage, and track coding exercises with history
- **Project Management** - Project assignments with difficulty levels and tracking
- **Attendance Tracking** - Daily attendance with "no class" day support
- **Blog/Posts** - Rich text posts for course announcements and content
- **Schedule Management** - Course schedule with start dates and planning
- **Admin Authorization** - Role-based access with admin privileges

## API Endpoints

| Group | Description |
|-------|-------------|
| `/api/users` | User management and profiles |
| `/api/exercises` | Exercise CRUD and history |
| `/api/projects` | Project CRUD and history |
| `/api/posts` | Blog posts and announcements |
| `/api/attendance` | Attendance tracking |
| `/api/permissions` | User permission management |
| `/api/validation` | Email and passcode validation |
| `/api/anthropic` | AI exercise/project generation (Claude) |
| `/api/deepseek` | AI exercise/project generation (DeepSeek) |
| `/api/noclass` | No-class day management |
| `/api/utility` | Utility endpoints |

## Project Structure

```
Kursserver/
â”œâ”€â”€ Endpoints/            # Minimal API endpoint groups
â”œâ”€â”€ Models/               # EF Core entity models
â”œâ”€â”€ Dto/                  # Data transfer objects (20+)
â”œâ”€â”€ Migrations/           # EF Core migrations
â”œâ”€â”€ Utils/
â”‚   â”œâ”€â”€ ApplicationDbContext.cs
â”‚   â”œâ”€â”€ AnthropicService.cs
â”‚   â”œâ”€â”€ DeepSeekService.cs
â”‚   â”œâ”€â”€ EmailService.cs
â”‚   â”œâ”€â”€ ExercisePromptTemplates.cs
â”‚   â”œâ”€â”€ ProjectPromptTemplates.cs
â”‚   â”œâ”€â”€ ExerciseResponseParser.cs
â”‚   â”œâ”€â”€ ProjectResponseParser.cs
â”‚   â”œâ”€â”€ FromClaims.cs
â”‚   â””â”€â”€ HasAdminPriviligies.cs
â”œâ”€â”€ wwwroot/              # Built React frontend
â””â”€â”€ Program.cs            # App configuration and startup
```

## Getting Started

```bash
dotnet restore
dotnet ef database update
dotnet run
```

Requires configuration of API keys and email settings in `appsettings.json`.

## Related

- **Frontend:** [kurshemsida](https://github.com/Badwolfpup/kurshemsida) - React + TypeScript frontend for this API
