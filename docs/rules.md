# 🛑 CRITICAL AGENT RULES

> **AGENT VIOLATION = WASTED TIME.** Follow these explicitly.

### 1. BEFORE YOU START
- **READ FIRST**: Read `docs/architecture.md`, `docs/conventions.md`, and `docs/build-run.md`.
- **RESEARCH**: Read Serena memories: `AutoBrowser/architecture`, `AutoBrowser/flow`, `AutoBrowser/services`.

### 2. COMMIT PROTOCOL (NEVER BYPASS)
- **NO BYPASS**: `git commit --no-verify` is **FORBIDDEN**. If hooks fail, **FIX THE CODE**.
- **SPLIT COMMITS**: Changes must be small, atomic, and logically grouped. 
- **DELAY**: Pause 3-5 minutes between commits (do not simulate robot speed).
- **PERMISSION**: **NEVER** push, commit, or create PRs without implicit "GO" from the user.

### 3. CODING & CLEANLINESS
- **FORMAT**: After *every edit*, run `dotnet format src/AutoBrowser/AutoBrowser.csproj --diagnostics IDE0005`.
- **CLEANUP**: No empty `catch {}`. Log, don't silent-fail.
- **WPF**: `MessageBox` usage: **ALWAYS** use `ShowDialogAsync()` (WPF UI), never `System.Windows.MessageBox`.
- **NO COMMENTS**: Unless specifically requested.

### 4. MEMORY/STATE
- **SYNC**: Update `AutoBrowser/changes/YYYY-MM/YYYY-MM-DD` and other memories immediately upon finishing (sync `memory_name`, `content`).

