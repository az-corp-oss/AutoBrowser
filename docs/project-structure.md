# Project Structure

## Overview

AutoBrowser is a WPF desktop app for Windows that routes URLs to user-configured browsers. It uses .NET 10, WPF UI library, and follows MVVM pattern. It registers as `autobrowser://` protocol handler and optional default browser, then routes URLs to user-configured browsers by regex rules. Lives in system tray, minimizes on close.

## Directory Layout

```
src/AutoBrowser/
├── App.xaml / .cs           # Entry point, DI, single-instance mutex, pipe server, theme
├── MainWindow.xaml / .cs    # NavigationView root, tray icon, re-register prompt
├── App.Args.cs              # System.CommandLine CLI argument parsing
├── App.Registration.cs      # Re-register prompt logic
├── App.Routing.cs           # IsUrl(), TryRoute() URL dispatch
├── App.Tray.cs              # Tray icon management
├── AssemblyInfo.cs          # Assembly metadata
├── Helpers/
│   └── WindowForegroundHelper.cs  # Win32 P/Invoke
├── Models/
│   ├── AppSettings.cs       # Persisted settings (ThemeMode, LastUpdateCheckTime)
│   ├── AppThemeMode.cs      # Light/Dark enum
│   ├── RoutingRule.cs       # Rule model
│   └── BrowserDefinition.cs # Browser detection from filesystem/registry
├── Services/
│   ├── IRuleService.cs      # Rule service interface
│   ├── RuleService.cs       # Rule JSON persistence + auto-merge
│   ├── ISettingsService.cs  # Settings service interface
│   ├── SettingsService.cs   # Settings JSON persistence
│   ├── IProtocolService.cs  # Protocol service interface
│   ├── ProtocolService.cs   # autobrowser:// registry ops + path check
│   ├── IDefaultBrowserService.cs  # Default browser interface
│   ├── DefaultBrowserService.cs   # Default browser registration + path check
│   ├── IUpdateService.cs    # Update service interface
│   ├── UpdateService.cs     # Auto-update from GitHub releases
│   └── UrlInterceptorService.cs  # URL matching + browser launch
├── ViewModels/
│   ├── MainViewModel.cs     # Commands, IsDarkTheme, Status, update throttling
│   ├── SettingsViewModel.cs # Theme settings, save command
│   ├── HomeViewModel.cs     # Rule management (CRUD, reorder, groups)
│   └── AboutViewModel.cs    # App version + credits
├── Views/
│   ├── HomePage.xaml / .cs       # Rule list + editor (grouped tree view)
│   ├── SettingsPage.xaml / .cs   # Theme toggle, default browser
│   ├── AboutPage.xaml / .cs      # Credits
│   ├── RuleEditorView.xaml / .cs # Add/Edit rule dialog
│   └── RuleTesterView.xaml / .cs # Test URL input dialog
src/AutoUpdater/
├── Program.cs               # Standalone single-file EXE for file swap + relaunch
src/AutoBrowser.Tests/
├── Models/                  # Model unit tests
├── Services/                # Service unit tests
├── ViewModels/              # ViewModel unit tests
└── UI/                      # FlaUI UI automation tests
```

## Key Flows

### URL Routing
1. External process or pipe sends URL → `App.HandleUrlArgsAsync()` → `UrlInterceptorService.TryRouteAsync()`
2. Match rules by priority/name, try regex against URL, launch browser

### Rule Persistence
- `RuleService` loads/saves `Data/rules.json`, auto-merges built-in rules
- Rule groups supported via `RuleGroup` class

### Theme Toggle
- `SettingsViewModel.IsDarkTheme` → `SettingsService` → `App.ApplyTheme()`
- Persists to `Data/settings.json`

### Auto-Update
- `UpdateService` checks GitHub releases API
- Downloads zip, launches `AutoUpdater.exe` for file swap
- `AutoUpdater.exe` waits for main app to exit, swaps files, relaunches

## Testing

- **Unit Tests**: xUnit, in-memory services, no UI dependency
- **UI Tests**: FlaUI WPF automation, requires active desktop
  - Each test launches fresh app copy in temp directory
  - Tests run sequentially (`DisableParallelization = true`)
  - Session timeout: 90s, per-test timeout: 30s
