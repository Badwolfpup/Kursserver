# Kursserver — Project Rules

## Unit Tests
- After implementing a feature, check if any modified or new logic is covered by a unit test in `Kursserver.Tests/`.
- **If you modified a tested function**: update the existing test(s) to reflect the change.
- **If you added new testable logic** (pure functions, parsers, date/schedule calculations, auth helpers): add unit tests for it.
- **Testable logic** means: pure functions, parsers, date/schedule calculations, auth helpers. It does NOT mean EF Core queries, HTTP endpoints, or email sending.
- Run `dotnet test Kursserver.Tests/Kursserver.Tests.csproj` after writing or changing tests to confirm all pass.
- **Quick edits**: If the change is config, model-only, or cosmetic — skip. Otherwise, check whether tests need updating.

## SCENARIO Comments & Static Trace
- After implementing a feature, add SCENARIO comments to every endpoint that was added or modified. See comment convention in MEMORY.md.
- **Plan mode**: After adding comments, automatically run `/static-trace` on every changed endpoint. Always show the full PASS/FAIL report.
- **Quick edits**: After finishing, if any logic changed (endpoints, email triggers, flag sets), ask if user wants to run `/static-trace`. Skip for config, model-only, and cosmetic changes.
