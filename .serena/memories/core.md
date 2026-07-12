# AutoBrowser — Core

## Project Type
Windows WPF desktop app (.NET 10) that routes URLs to user-configured browsers via regex rules.

## Repository Layout
```
src/
├── AutoBrowser/          # Main WPF app (net10.0-windows)
│   ├── Models/           # RoutingRule, BrowserDefinition, AppSettings, AppThemeMode
│   ├── Services/         # RuleService, ProtocolService, DefaultBrowserService, SettingsService, UpdateService, UrlInterceptorService, SingleInstanceService
│   ├── ViewModels/       # MainViewModel, SettingsViewModel
│   └── Views/            # HomePage, SettingsPage, AboutPage, ToolbarView, RulesListView, StatusControl, RuleEditorView, RuleTesterView
├── AutoBrowser.Tests/    # xUnit + Moq unit tests
└── AutoUpdater/          # Standalone single-file console EXE for file swap + relaunch
```

## Key Invariants
- Portable: all data stored in `Data/` folder next to EXE
- Single-instance via named pipe IPC + named mutex
- Registers `autobrowser://` protocol handler; optionally registers as default browser
- URL routing: pattern match (regex with substring fallback) → launch browser by path
- Infinite-loop protection: unmatched URLs launch previous default browser directly by EXE path
- Auto-update from GitHub releases (throttled to once per hour)
- Theme persistence via `AppSettings.ThemeMode`
- DI container via `Microsoft.Extensions.DependencyInjection`

## Memories Index
- `mem:tech_stack` — languages, frameworks, dependencies
- `mem:conventions` — code style, naming, patterns
- `mem:suggested_commands` — build, test, run commands
- `mem:task_completion` — verification steps after changes
- `mem:AutoBrowser/architecture` — high-level design, DI, page layout, theme init
- `mem:AutoBrowser/flow` — URL routing, update flow, startup sequence
- `mem:AutoBrowser/services` — browser detection, persistence, registry
- `mem:AutoBrowser/ui-behavior` — theme toggle, tray, re-register prompt, WPF UI patterns
- `mem:git/commit-strategy` — split per logical context, 3-5min delay between commits
- `mem:workflow/sync-memory` — sync project state to memory after changes
