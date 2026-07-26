# Technical Architecture: AutoBrowser

## 1. System Overview

AutoBrowser is a **single-process WPF desktop application** with an **optional companion process** (AutoUpdater) for atomic updates. The system follows the **MVVM pattern** with **dependency injection** for testability and modularity.

### Architecture Style
- **Monolithic desktop app** with service-based architecture
- **MVVM** (Model-View-ViewModel) with CommunityToolkit.Mvvm
- **Dependency Injection** via Microsoft.Extensions.DependencyInjection
- **Event-driven** for URL interception and system tray interactions

---

## 2. High-Level Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│                         AutoBrowser.exe                            │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌───────────┐ │
│  │  App.xaml   │  │  MainWindow │  │  System     │  │  Named    │ │
│  │  (Entry)    │──│  (Shell)    │  │  Tray       │  │  Pipe     │ │
│  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘  └─────┬─────┘ │
│         │                │                │                │         │
│         └────────────────┼────────────────┼────────────────┘         │
│                          │                │                          │
│                          ▼                ▼                          │
│  ┌─────────────────────────────────────────────────────────────┐    │
│  │                    DI Container                             │    │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────┐ │    │
│  │  │  Services   │  │  ViewModels │  │  Pages              │ │    │
│  │  │  - Rule     │  │  - Main     │  │  - HomePage         │ │    │
│  │  │  - Settings │  │  - Home     │  │  - SettingsPage     │ │    │
│  │  │  - Protocol │  │  - Settings │  │  - AboutPage        │ │    │
│  │  │  - Update   │  │  - About    │  │                     │ │    │
│  │  │  - URL      │  │             │  │                     │ │    │
│  │  └─────────────┘  └─────────────┘  └─────────────────────┘ │    │
│  └─────────────────────────────────────────────────────────────┘    │
│                          │                                          │
│                          ▼                                          │
│  ┌─────────────────────────────────────────────────────────────┐    │
│  │                    Data Layer                               │    │
│  │  Data/rules.json  Data/settings.json  Data/default_browser │    │
│  └─────────────────────────────────────────────────────────────┘    │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
                                      │
                                      │ (optional, for updates)
                                      ▼
┌─────────────────────────────────────────────────────────────────────┐
│                       AutoUpdater.exe                               │
│  - Standalone console app                                          │
│  - Waits for main app exit                                         │
│  - Swaps files with SHA256 verification                            │
│  - Relaunches main app                                             │
└─────────────────────────────────────────────────────────────────────┘
```

---

## 3. Component Architecture

### 3.1 Application Layer (App.xaml.cs)

**Responsibilities:**
- Configure DI container
- Single-instance mutex management
- CLI argument parsing (System.CommandLine)
- Theme application
- Window lifecycle events
- Named pipe server for second-instance URL handoff

**Key Components:**
```
App.xaml.cs
├── ConfigureDI()           # Register all services
├── OnStartup()             # Mutex, pipe server, theme
├── OnLoaded()              # CLI dispatch, re-register prompt
├── ApplyTheme()            # Light/Dark/System theme
├── HandleUrlArgsAsync()    # URL dispatch
└── ShowMainWindow()        # Window management
```

### 3.2 Service Layer

**Core Services (Interfaces):**
| Service | Responsibility |
|---|---|
| `IRuleService` | CRUD for routing rules, pattern validation |
| `ISettingsService` | Persist/load settings (theme, tray behavior) |
| `IProtocolService` | Register/unregister `autobrowser://` protocol |
| `IDefaultBrowserService` | Register/unregister as default browser |
| `IUpdateService` | Check/download/apply updates from GitHub |
| `IUrlInterceptorService` | Match URLs to rules, launch browsers |

**Service Implementation Details:**
- All services are **singletons** in DI container
- Services use **JSON file persistence** (no database)
- Services are **thread-safe** (file access with locking)
- Services **log operations** via Serilog

### 3.3 ViewModel Layer

**MVVM Pattern:**
- ViewModels inherit from `ObservableObject` (CommunityToolkit.Mvvm)
- Properties use `[ObservableProperty]` source generators
- Commands use `[RelayCommand]` source generators
- Auto-save: Rules save automatically on property change

**ViewModel Responsibilities:**
| ViewModel | Responsibilities |
|---|---|
| `MainViewModel` | Window title, theme toggle, update commands |
| `HomeViewModel` | Rule CRUD, reorder, groups, test URL |
| `SettingsViewModel` | Theme selection, tray behavior |
| `AboutViewModel` | Version info, credits |

### 3.4 View Layer (XAML)

**Page-Based Navigation:**
- `MainWindow.xaml` contains `<ui:NavigationView>` as root
- Pages resolved from DI container via `INavigationViewPageProvider`
- No custom title bar (OS title bar for reliability)

**UI Library:**
- WPF UI (Lepo.co) for modern Windows look
- Mica backdrop for translucent background
- Fluent design system controls

**Page Structure:**
```
MainWindow
├── HomePage          # Rule list, add/edit/delete/reorder
├── SettingsPage      # Theme, tray behavior
└── AboutPage         # Version, credits

Dialogs (shown as content dialogs)
├── RuleEditorView    # Add/Edit rule
└── RuleTesterView    # Test URL input
```

---

## 4. Data Architecture

### 4.1 Data Storage

**Location:** `Data/` folder next to EXE (portable)

**Files:**
| File | Format | Purpose |
|---|---|---|
| `rules.json` | JSON | Routing rules with groups |
| `settings.json` | JSON | App settings (theme, tray) |
| `default_browser.txt` | Text | Original default browser path |

**Schema:**
```json
// rules.json
{
  "Groups": [
    {
      "Name": "Work",
      "Sequence": 1,
      "Rules": [
        {
          "Name": "Google Apps",
          "Pattern": "^https?://(mail|drive|docs)\\.google\\.com",
          "BrowserPath": "C:\\...\\chrome.exe",
          "BrowserName": "Chrome",
          "IsEnabled": true,
          "Priority": 10,
          "Sequence": 1
        }
      ]
    }
  ]
}

// settings.json
{
  "ThemeMode": "Dark",
  "MinimizeToTray": true,
  "CloseToTray": true,
  "LastUpdateCheckTime": "2026-07-25T10:00:00Z"
}
```

### 4.2 Registry Integration

**Protocol Handler (HKCU):**
```
Software\Classes\autobrowser
  (Default) = "AutoBrowser:URL"
  FriendlyTypeName = "AutoBrowser"
  DefaultIcon
    (Default) = "\"C:\...\AutoBrowser.exe\",0"
  shell\open\command
    (Default) = "\"C:\...\AutoBrowser.exe\" \"%1\""
```

**Default Browser (HKCU):**
```
Software\Classes\AutoBrowserLink
  (Default) = "HTTP:AutoBrowser"
  shell\open\command
    (Default) = "\"C:\...\AutoBrowser.exe\" \"%1\""

Software\AutoBrowser\Capabilities
  ApplicationName = "AutoBrowser"
  ApplicationDescription = "Smart URL router"
  URLAssociations
    http = "AutoBrowserLink"
    https = "AutoBrowserLink"

Software\RegisteredApplications
  AutoBrowser = "Software\AutoBrowser\Capabilities"
```

---

## 5. Key Flows

### 5.1 URL Routing Flow

```
External Process
    │
    ▼
AutoBrowser.exe (new instance)
    │
    ├─► OnStartup() → Mutex check
    │       │
    │       ├─► First instance: Create pipe server
    │       │       │
    │       │       ▼
    │       │   WaitForConnectionAsync()
    │       │       │
    │       │       ▼
    │       │   ReadUrlFromPipe()
    │       │       │
    │       │       ▼
    │       │   HandleUrlArgsAsync(url)
    │       │       │
    │       │       ▼
    │       │   UrlInterceptorService.TryRouteAsync(url)
    │       │       │
    │       │       ├─► StripProtocolPrefix(url)
    │       │       ├─► LoadRules() from Data/rules.json
    │       │       ├─► Filter IsEnabled=true
    │       │       ├─► Sort by Priority ASC
    │       │       ├─► First regex/substring match → Launch browser
    │       │       └─► No match → Launch fallback browser
    │       │
    │       └─► Second instance: Send URL via pipe, exit
    │
    └─► No URL: Show MainWindow (normal startup)
```

### 5.2 Update Flow

```
┌─────────────────────────────────────────────────────────────┐
│  User clicks "Check Update" / App starts                    │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│  UpdateService.CheckForUpdateAsync()                        │
│  ├─► Query GitHub releases API (/releases?per_page=10)      │
│  ├─► Compare versions (Major.Minor.Patch)                   │
│  └─► Return: No release / Up-to-date / New version          │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│  New version available                                      │
│  ├─► Show dialog: "Update Available" (Yes/No)               │
│  └─► User clicks Yes                                        │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│  UpdateService.DownloadAndUpdateAsync()                     │
│  ├─► Download ZIP from GitHub                               │
│  ├─► Extract to temp workspace                              │
│  ├─► Copy AutoUpdater.exe to runner dir                     │
│  ├─► Launch AutoUpdater.exe (waits for main exit)           │
│  └─► Main app shuts down                                    │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│  AutoUpdater.exe                                            │
│  ├─► Wait for main process exit (2 min timeout)             │
│  ├─► Back up old files                                      │
│  ├─► Copy new files with SHA256 verification                │
│  ├─► Verify hashes                                          │
│  ├─► On failure → Restore backup                            │
│  └─► Relaunch main app                                      │
└─────────────────────────────────────────────────────────────┘
```

### 5.3 Rule Reorder Flow

```
User clicks "Move Up" / "Move Down"
    │
    ▼
HomeViewModel.MoveRule(command)
    │
    ├─► Find rule index in list
    ├─► Swap with adjacent rule
    ├─► UpdateGroupSequences(group)
    │       │
    │       └─► Loop: Set each rule.Sequence = index + 1
    ├─► RuleService.SaveRules()
    └─► Refresh UI
```

---

## 6. Dependency Injection

### 6.1 Registration

```csharp
// App.xaml.cs
private void ConfigureDI()
{
    var services = new ServiceCollection();

    // Services (Singletons)
    services.AddSingleton<IRuleService, RuleService>();
    services.AddSingleton<ISettingsService, SettingsService>();
    services.AddSingleton<IProtocolService, ProtocolService>();
    services.AddSingleton<IDefaultBrowserService, DefaultBrowserService>();
    services.AddSingleton<IUpdateService, UpdateService>();
    services.AddSingleton<IUrlInterceptorService, UrlInterceptorService>();

    // ViewModels (Transient)
    services.AddTransient<MainViewModel>();
    services.AddTransient<HomeViewModel>();
    services.AddTransient<SettingsViewModel>();
    services.AddTransient<AboutViewModel>();

    // Pages (Transient)
    services.AddTransient<HomePage>();
    services.AddTransient<SettingsPage>();
    services.AddTransient<AboutPage>();

    // Main Window
    services.AddSingleton<MainWindow>();

    _serviceProvider = services.BuildServiceProvider();
}
```

### 6.2 Lifetime Management

| Component | Lifetime | Reason |
|---|---|---|
| Services | Singleton | Shared state, thread-safe |
| ViewModels | Transient | Fresh state per navigation |
| Pages | Transient | Fresh state per navigation |
| MainWindow | Singleton | Single instance |

---

## 7. Error Handling

### 7.1 Exception Strategy

- **No empty catch blocks** — all exceptions must be logged or handled
- **Service-level try/catch** — catch exceptions at service boundaries
- **Log.Error(ex, "message")** — always log exceptions with context
- **Graceful degradation** — if a service fails, continue with defaults

### 7.2 Specific Error Scenarios

| Scenario | Handling |
|---|---|
| Rule file missing | Create empty file, log warning |
| Rule file corrupted | Log error, use default rules |
| Settings file missing | Create default settings |
| Browser not found | Fall back to system default |
| Registry access denied | Log error, continue without registration |
| Update download fails | Log error, show message, continue |
| Update extraction fails | Restore backup, log error |

---

## 8. Security Considerations

### 8.1 Data Privacy
- **No telemetry** — no data leaves the machine except update checks
- **No network requests** — except GitHub API for updates
- **Local storage only** — all data in `Data/` folder

### 8.2 Registry Safety
- **HKCU only** — no admin elevation required
- **Reversible changes** — can unregister protocol/browser
- **Backup before modify** — save current default browser before changing

### 8.3 Update Security
- **HTTPS only** — all GitHub API calls use HTTPS
- **SHA256 verification** — verify file hashes after extraction
- **Atomic updates** — backup before, restore on failure

---

## 9. Testing Strategy

### 9.1 Unit Tests (xUnit)
- **Service tests** — in-memory file system
- **ViewModel tests** — mock services
- **Model tests** — property validation

### 9.2 UI Tests (FlaUI)
- **Full app launch** — test in temp directory
- **UI automation** — click buttons, verify state
- **Sequential execution** — no parallelization
- **Timeout protection** — 30s per test, 90s session

### 9.3 Test Coverage
| Area | Coverage |
|---|---|
| Services | >90% |
| ViewModels | >80% |
| Models | >95% |
| UI | Critical paths only |

---

## 10. Build & Deployment

### 10.1 Build Process

```
dotnet build src\AutoBrowser\AutoBrowser.csproj -o bin\staging
    │
    ├─► Restore NuGet packages
    ├─► Compile C# code
    ├─► Generate XAML (BAML)
    ├─► KillBeforeBuild target (taskkill AutoBrowser.exe)
    ├─► PublishUpdater target (publish AutoUpdater.csproj)
    └─► CopyUpdater target (copy AutoUpdater to staging)
```

### 10.2 Deployment
- **Portable** — copy `bin\staging` folder anywhere
- **No installer** — just run `AutoBrowser.exe`
- **Self-contained** — includes all dependencies (except .NET runtime)

### 10.3 Versioning
- **Semantic Versioning** — Major.Minor.Patch
- **GitHub Releases** — ZIP archives with changelog
- **Auto-update** — compare versions, download delta

---

## 11. Monitoring & Logging

### 11.1 Logging (Serilog)

**Sinks:**
- Console (for debugging)
- File (`Logs/AutoBrowser-.log`)

**Log Levels:**
- Information — Method entry/exit, key operations
- Debug — Intermediate steps, variable values
- Verbose — Fine-grained details
- Error — Exceptions with stack traces

**Log Rotation:**
- Daily rotation
- Retention: 7 days (default)

### 11.2 Metrics (Future)
- Rule match success rates
- Update success/failure rates
- Startup time tracking

---

## 12. Future Architecture Considerations

### 12.1 Potential Enhancements
- **Plugin system** — allow custom rule matchers
- **Cloud sync** — sync rules across machines
- **Browser profiles** — support Chrome profiles, Firefox containers
- **Schedule-based routing** — time-of-day rules
- **URL pattern templates** — common patterns library

### 12.2 Scalability
- **Current limit** — ~1000 rules (O(n) evaluation)
- **Optimization** — regex compilation, caching
- **Alternative storage** — SQLite for complex queries

### 12.3 Extensibility
- **Service interfaces** — easy to swap implementations
- **MVVM pattern** — easy to add new views
- **DI container** — easy to add new services
