# Kursserver — Project Rules

## Architecture Reference
- `.claude/PROJECT_CONTEXT.md` — full architecture, tech stack, endpoints, domain models, services
- `.claude/INTENT.md` — organizational purpose, who we serve, guiding principles for design decisions
- After completing implementation that changes architecture, endpoints, models, or services, check if this file needs updating.

## Roles
- **Teacher** and **Admin** are the same role with the same permissions — the only difference is that Admins can add new teachers. Treat them as interchangeable unless the task explicitly distinguishes them.
- **Coach** is a separate role with its own permissions.
- **Student** is a separate role with its own permissions.

## Pull Requests
- **Never push directly to main** — all changes must go through a PR. Create a branch, push it, and create a PR.

## Unit Tests
- After implementing a feature, invoke the `unit-test` skill. It will scan changed code, identify testable logic, write tests, and run 3 independent reviewers.
- **Quick edits**: If the change is config, model-only, or cosmetic — skip. Otherwise, invoke the skill.

## SCENARIO Comments & Static Trace
- After implementing a feature, add SCENARIO comments to every endpoint that was added or modified. See comment convention in MEMORY.md.
- **Plan mode**: After adding comments, automatically run `/static-trace` on every changed endpoint. Always show the full PASS/FAIL report.
- **Quick edits**: After finishing, if any logic changed (endpoints, email triggers, flag sets), ask if user wants to run `/static-trace`. Skip for config, model-only, and cosmetic changes.
