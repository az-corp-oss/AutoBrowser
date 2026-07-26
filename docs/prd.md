# Product Requirements Document: AutoBrowser

## 1. Executive Summary

**AutoBrowser** is a Windows desktop application that acts as an intelligent URL router, automatically directing web links to user-configured browsers based on customizable regex rules. It solves the problem of users who want different browsers for different types of URLs (e.g., Chrome for Google apps, Firefox for development, Edge for general browsing) without manually copying and pasting URLs.

**Target Users:**
- Power users who use multiple browsers for different tasks
- Developers who need specific browsers for testing
- Users who want to separate work and personal browsing

**Key Value Proposition:**
- Set-and-forget URL routing rules
- Seamless integration with Windows as a protocol handler
- Portable, no-installation-required design

---

## 2. Product Goals & Success Metrics

### Goals
1. **Primary**: Provide reliable, automatic URL routing to the correct browser based on user-defined rules
2. **Secondary**: Be invisible and non-intrusive — run in system tray, minimal startup footprint
3. **Tertiary**: Self-updating with minimal user intervention

### Success Metrics
| Metric | Target |
|---|---|
| Rule match accuracy | >99% for configured patterns |
| Startup time | <2 seconds to ready state |
| Memory footprint | <50MB typical usage |
| Update success rate | >95% without manual intervention |

---

## 3. User Stories

### Core URL Routing
| ID | Story | Priority |
|---|---|---|
| US-001 | As a user, I want to define rules that match URL patterns and open them in specific browsers | P0 |
| US-002 | As a user, I want rules evaluated by priority (lowest number first) so I can control match order | P0 |
| US-003 | As a user, I want to group rules into named categories for better organization | P1 |
| US-004 | As a user, I want to test a URL against my rules before they go live | P1 |
| US-005 | As a user, I want to enable/disable rules without deleting them | P0 |

### Browser Management
| ID | Story | Priority |
|---|---|---|
| US-006 | As a user, I want AutoBrowser to detect installed browsers automatically | P0 |
| US-007 | As a user, I want to set a fallback browser for unmatched URLs | P0 |
| US-008 | As a user, I want to add custom browser paths if detection misses one | P1 |

### Windows Integration
| ID | Story | Priority |
|---|---|---|
| US-009 | As a user, I want AutoBrowser to register as the default browser handler | P0 |
| US-010 | As a user, I want AutoBrowser to register the `autobrowser://` protocol | P0 |
| US-011 | As a user, I want a re-register prompt if Windows registry entries are missing | P1 |

### System Tray & UI
| ID | Story | Priority |
|---|---|---|
| US-012 | As a user, I want AutoBrowser to live in the system tray and not clutter my taskbar | P0 |
| US-013 | As a user, I want minimize-to-tray and close-to-tray as independent options | P1 |
| US-014 | As a user, I want to switch between light and dark themes | P2 |

### Updates
| ID | Story | Priority |
|---|---|---|
| US-015 | As a user, I want AutoBrowser to check for updates automatically on startup | P1 |
| US-016 | As a user, I want one-click updates that don't require re-downloading the entire app | P1 |
| US-017 | As a user, I want the update process to be atomic — if it fails, the old version is restored | P0 |

---

## 4. Functional Requirements

### 4.1 URL Routing Engine

**FR-001: Rule Matching**
- Rules are stored in `Data/rules.json`
- Each rule has: Name, Pattern (regex/substring), BrowserPath, BrowserName, IsEnabled, Priority, Sequence
- Rules are evaluated in Priority ASC order (lower number = higher priority)
- First matching rule wins
- If no rule matches, use fallback browser

**FR-002: Pattern Matching**
- Supports both substring and regex matching
- Case-insensitive matching by default
- Patterns are validated before saving (invalid regex is rejected)

**FR-003: Browser Launch**
- Browsers are launched with the URL as an argument
- Uses `Process.Start()` with the browser's executable path
- If browser path is invalid, falls back to system default

### 4.2 Rule Management

**FR-004: CRUD Operations**
- Add new rules via RuleEditorView dialog
- Edit existing rules inline or via dialog
- Delete rules with confirmation
- Reorder rules via Move Up/Move Down commands

**FR-005: Rule Groups**
- Rules can be organized into named groups
- Groups can be collapsed/expanded in the UI
- Groups have their own sequence ordering

**FR-006: Auto-Save**
- Rules are saved automatically when modified (no explicit save button)
- Debounced saving to avoid excessive disk writes

### 4.3 Windows Integration

**FR-007: Protocol Registration**
- Registers `autobrowser://` protocol in HKCU registry
- No admin elevation required
- Protocol handler points to AutoBrowser.exe with URL as argument

**FR-008: Default Browser Registration**
- Optional: Set as default browser via Windows Settings > Default Apps
- Uses standard Windows default browser registration APIs
- Reverts to previous default if uninstalled

**FR-009: Single Instance**
- Only one instance of AutoBrowser can run at a time
- Uses named mutex for instance detection
- Second instance sends URL to first instance via named pipe

### 4.4 System Tray

**FR-010: Tray Behavior**
- Minimize to tray on close (configurable)
- Minimize to tray on minimize (configurable)
- Right-click context menu: Show/Hide, Check for Updates, Exit

**FR-011: Theme Support**
- Light and Dark theme options
- Follows system theme by default
- Persists user preference to `Data/settings.json`

### 4.5 Auto-Update

**FR-012: Update Check**
- Checks GitHub releases API on startup
- Compares versions (Major.Minor.Patch)
- Shows dialog if newer version available

**FR-013: Update Process**
- Downloads ZIP from GitHub release
- Extracts to temp workspace
- Launches AutoUpdater.exe for file swap
- AutoUpdater waits for main app exit (2 min timeout)
- Swaps files with SHA256 verification
- Relaunches main app

**FR-014: Update Rollback**
- Backs up current version before update
- Restores backup if update fails
- Logs all update operations for debugging

---

## 5. Non-Functional Requirements

### 5.1 Performance
| Requirement | Target |
|---|---|
| Startup time | <2 seconds |
| URL routing latency | <500ms |
| Memory usage | <50MB typical |
| Disk space | <10MB installed |

### 5.2 Reliability
| Requirement | Target |
|---|---|
| Uptime | 99.9% (excluding updates) |
| Update success rate | >95% |
| Rule match accuracy | >99% |

### 5.3 Usability
- No installation required (portable)
- No admin privileges needed
- Minimal configuration required (just set rules and forget)
- Clear, non-technical UI

### 5.4 Security
- No data collection or telemetry
- All data stored locally in `Data/` folder
- Registry changes limited to HKCU (user-level)
- No network requests except update checks and GitHub API

### 5.5 Compatibility
- Windows 10 (1809+)
- Windows 11
- .NET 10 runtime required

---

## 6. UI/UX Requirements

### 6.1 Main Window
- NavigationView-based layout (WPF UI library)
- Three pages: Home (Rules), Settings, About
- Mica backdrop for modern Windows look
- OS-native title bar (no custom title bar)

### 6.2 Home Page (Rules)
- Grouped tree view of rules
- Add/Edit/Delete/Reorder actions
- Test URL input dialog
- Visual indicators for rule state (enabled/disabled)

### 6.3 Settings Page
- Theme toggle (Light/Dark/System)
- Default browser selection
- Tray behavior toggles (minimize-to-tray, close-to-tray)

### 6.4 About Page
- App version and credits
- GitHub link for updates

---

## 7. Technical Constraints

### 7.1 Technology Stack
- **Framework**: .NET 10 (WPF)
- **UI Library**: WPF UI (Lepo.co)
- **Language**: C# (latest)
- **Pattern**: MVVM with CommunityToolkit.Mvvm
- **DI**: Microsoft.Extensions.DependencyInjection
- **Logging**: Serilog (Console + File)
- **CLI**: System.CommandLine
- **Testing**: xUnit + FlaUI (UI automation)

### 7.2 Build & Deployment
- Portable EXE (no installer)
- Single-file publishing for AutoUpdater
- MSBuild targets for post-build copy
- NuGet package management (Central Package Management optional)

### 7.3 Data Storage
- JSON files in `Data/` folder next to EXE
- No database required
- Portable (can be moved between machines)

---

## 8. Future Considerations

### 8.1 Potential Enhancements
- Rule import/export
- Rule sharing between users
- Browser profiles (not just executables)
- Schedule-based routing (time of day)
- URL pattern templates (common patterns)

### 8.2 Scalability
- Current design supports ~1000 rules without performance issues
- Rule evaluation is O(n) where n = number of enabled rules
- No known bottlenecks for typical usage

---

## 9. Open Questions

1. Should we support rule import/export in v1?
2. Should we add telemetry for rule match success rates?
3. Should we support browser profiles (e.g., Chrome profiles) in v1?

---

## 10. Appendices

### Appendix A: Data Formats

**Rule Format (rules.json)**
```json
{
  "Name": "Google Apps",
  "Pattern": "^https?://(mail|drive|docs|calendar|meet)\\.google\\.com",
  "BrowserPath": "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe",
  "BrowserName": "Chrome",
  "IsEnabled": true,
  "Priority": 10,
  "Sequence": 1
}
```

**Settings Format (settings.json)**
```json
{
  "ThemeMode": "Dark",
  "MinimizeToTray": true,
  "CloseToTray": true,
  "LastUpdateCheckTime": "2026-07-25T10:00:00Z"
}
```

### Appendix B: Registry Entries

**Protocol Handler (HKCU)**
```
Software\Classes\autobrowser
  (Default) = "AutoBrowser:URL"
  shell\open\command
    (Default) = "\"C:\Path\To\AutoBrowser.exe\" \"%1\""
```

**Default Browser (HKCU)**
```
Software\Classes\AutoBrowserLink
  (Default) = "HTTP:AutoBrowser"
  shell\open\command
    (Default) = "\"C:\Path\To\AutoBrowser.exe\" \"%1\""
```
