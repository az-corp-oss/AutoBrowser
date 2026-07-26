# AutoBrowser — Core

WPF desktop app (.NET 10) that routes URLs to browsers by regex rules.

## Source Map

- `src/AutoBrowser/` — Main WPF application
  - `App.xaml.cs` + partials — Entry point, DI, single-instance, pipe server, theme
  - `Models/` — Data models (AppSettings, RoutingRule, BrowserDefinition)
  - `Services/` — Business logic (RuleService, SettingsService, ProtocolService, UpdateService, UrlInterceptorService)
  - `ViewModels/` — MVVM view models (Main, Home, Settings, About)
  - `Views/` — XAML pages and dialogs
  - `Helpers/` — Win32 P/Invoke utilities
- `src/AutoUpdater/` — Standalone EXE for file swap + relaunch during updates
- `src/AutoBrowser.Tests/` — xUnit tests (unit + FlaUI UI tests)
- `docs/` — Project documentation

## Invariants

- Portable app: data stored in `Data/` folder next to EXE
- Registry: HKCU only, no admin elevation
- Single-instance via mutex; pipe server for URL dispatch
- System tray app; minimizes on close
- Protocol handler: `autobrowser://`
- Always remove unused usings after changes (IDE0005)

## References

- Tech stack: `mem:tech_stack`
- Commands: `mem:suggested_commands`
- Conventions: `mem:conventions`
- Task completion: `mem:task_completion`
