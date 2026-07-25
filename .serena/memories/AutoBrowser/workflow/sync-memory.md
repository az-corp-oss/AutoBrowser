# Workflow: Sync Memory

## ALWAYS do at end of every task:
1. **Update changes history** — Write to `AutoBrowser/changes/YYYY-MM/YYYY-MM-DD` (use today's date).
   - Append new changes under appropriate headings.
   - Keep existing entries, only add new ones.
   - Use terse bullet format: what changed, why.

## Format for daily change file:
```markdown
# Changes YYYY-MM-DD

## Category Name
- Brief description of change.
- Another change.
```

## Naming:
- Month folder: `YYYY-MM` (e.g. `2026-07`)
- Day file: `YYYY-MM-DD` (e.g. `2026-07-25`)
- If file doesn't exist, create it. If it does, append to it.
