# Kursserver — Project Rules

## SCENARIO Comments & Static Trace
- After implementing a feature, add SCENARIO comments to every endpoint that was added or modified. See comment convention in MEMORY.md.
- **Plan mode**: After adding comments, automatically run `/static-trace` on every changed endpoint. Always show the full PASS/FAIL report.
- **Quick edits**: After finishing, if any logic changed (endpoints, email triggers, flag sets), ask if user wants to run `/static-trace`. Skip for config, model-only, and cosmetic changes.
