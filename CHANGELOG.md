# Changelog

All notable changes to AutoBrowser are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project
adheres to date-based entries (newest first). Entries were migrated from the former Serena daily
change memories (`AutoBrowser/changes/YYYY-MM/YYYY-MM-DD`).

## [2026-11-26]

### Changed

- Pre-push now runs UI tests only when `.xaml` or `.xaml.cs` changed, while unit tests still run on every push. A failing UI test blocks the push.

## [2026-11-25]

### Added

- Data storage switch in Settings: portable mode keeps data in `Data/` beside the EXE, global mode uses `~/.config/autobrowser`. Switching copies `rules.json`, `settings.json`, and `default_browser.txt` to the destination, and a small button next to the path opens that folder in Explorer.

### Fixed

- URLs whose last path segment is `AutoBrowser` were dropped at startup by `System.CommandLine`, so the app only came to the foreground and never routed the URL or used the fallback browser. `RouteCommand.Parse` now extracts the URL before parsing options; added `RouteCommandTests` covering both cases.
- URL scheme detection is shared in `UrlHelper` and now includes `microsoft-edge:`, so intercepted URLs passed as startup arguments are routed too.
- F2 rename now targets the selected group instead of an arbitrary one, and group rename goes through `HomeViewModel`.
- The empty-state visibility now updates when rules are added to or removed from the active group.
- Switching data storage modes now shows a conflict dialog when a file exists in both locations with different content: choose Keep global or Keep portable per file, with Merge rules available for `rules.json`. Existing destination files are backed up before any copy or merge.
- Fixed silent auto-update failure: `UpdateService` no longer passes unsupported `--target` and `--workspace` arguments to `AutoUpdater`, and `AutoUpdater` now tolerates those legacy options instead of exiting before it writes a log.

### Changed

- Replaced the accordion group headers in `RulesListView` with a left `GroupSidebarView` on the Home page. Selecting a group now shows only that group's rules; the sidebar uses a toggle for the group's enabled state alongside its name and sequence.
- Added `HomeViewModel.ActiveGroup`, `VisibleRules`, and `HasVisibleRules` to drive the master/detail layout, plus an empty state for groups without rules.
- Kept the toolbar outside the rules `ScrollViewer` so it stays fixed while the rule list scrolls.
- Added inline group rename in the sidebar: double-click the group name or press F2, Enter saves and Escape cancels.

## [2026-11-14]

### Added

- Windows Search Interception (`IWindowsSearchInterceptorService`, `IWindowsSearchReactiveService`):
  Intercepts MS Edge links (`microsoft-edge:`) launched from Windows Search and routes them according to AutoBrowser rules.
- Toggle switch in `SettingsPage.xaml` for "Intercept Windows Search Web Results".
- CLI arguments `--enable-interception` and `--disable-interception` for administrative control.
- Unit test for `microsoft-edge:` prefix handling in `UrlInterceptorServiceTests.cs` and enablement error handling in `SettingsViewModelTests.cs`.

### Changed

- Updated `docs/prd.md` to reflect v1.1 status, including Windows Search Interception specs, user stories (US-019), and updated system requirements.

## [2026-07-25]

### Fixed

- Rule sequence order persistence: Move Up/Down now re-writes each rule's `Sequence` via the
  `UpdateGroupSequences(RuleGroup)` helper in `HomeViewModel`, so custom ordering survives restart.
  Sequence updates integrated into `MoveRule`, `AddRule`, and `DeleteRule`; unit tests added in
  `HomeViewModelRuleTests.cs`.
- Test timeouts: added `AutoBrowser.Tests.runsettings` (TestCaseTimeout 30s, TestSessionTimeout 90s)
  so tests (especially UI automation) cannot hang forever. Docs updated to run tests with
  `--settings AutoBrowser.Tests.runsettings`.

### Changed

- Single-char `StartsWith("\"")` replaced with `.StartsWith('"')` in `BrowserDefinition.cs` and
  `DefaultBrowserService.cs` to avoid string allocation.
- Enabled `<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>` so IDE0005 (unnecessary usings)
  fails the build; convention documented to auto-fix with `dotnet format --diagnostics IDE0005`.

## [2026-07-22]

### Changed

- Replaced the nested `TreeView` in `RulesListView.xaml` with nested `ItemsControl`s — flat card
  layout with full-width group headers and indented rules, fixing inconsistent widths.
- Visual selection styling maintained via `EqualityToBooleanConverter` comparing against
  `HomeViewModel.SelectedItem`; click-to-select handlers (`GroupBorder_MouseLeftButtonDown`,
  `RuleBorder_MouseLeftButtonDown`) updated for the flat layout.

## [2026-07-14]

### Added

- `ShowPushNotifications` option (default true) in `AppSettings`, `SettingsViewModel`, and
  `SettingsPage.xaml` — users can enable/disable tray/balloon notifications dynamically.

### Fixed

- Balloon notifications: `NotifyIcon` now kept alive for 4 seconds before disposal so the OS
  displays the balloon tip instead of it disappearing immediately.

### Changed

- Routing (`App.Routing.cs`, `HomeViewModel.cs`) checks `ShowPushNotifications` before calling
  `ShowNotification`.
- `.husky/task-runner.json` pre-push task now runs all tests (including UI tests).

## [2026-07-13]

### Added

- `AutomationProperties.AutomationId` on all `RuleEditorView.xaml` controls
  (NameBox, PatternBox, BrowserCombo, BrowserPathBox, OkButton, CancelButton) for FlaUI testability;
  fixed `MainWindow_AddRule_FillForm_Save` (FlaUI could not find controls by `x:Name` alone).
- `MainWindow_DeleteRule` FlaUI integration test (launch, add rule, select, delete, assert gone).
- Husky.NET v0.9.1 git hooks: `.husky/task-runner.json` pre-commit task runs 79 unit tests
  (UI tests excluded) and blocks the commit on failure; manual trigger
  `dotnet husky run --group pre-commit`; teammate setup `dotnet tool restore` + `dotnet husky install`.

## [2026-07-12]

### Fixed

- Theme startup order: `ApplyTheme()` moved from `App.xaml.cs` to `MainWindow_Loaded`
  (`App.Window.cs`) following the WPF UI Gallery pattern — `ApplicationThemeManager.Apply()`
  requires the window to exist to update background/resources.
- Dark theme typography: explicit `Foreground` (`TextFillColorPrimaryBrush` /
  `TextFillColorSecondaryBrush`) on all `FontTypography` TextBlocks (HomePage, SettingsPage,
  AboutPage, RulesListView) — `FontTypography` alone does not inherit the theme foreground.

### Changed

- Replaced all fully qualified namespace references with `using` directives; aliases centralized in
  `GlobalUsings.cs` only (rule: no local `using` aliases).

## [2026-07-10]

### Added

- `Serilog.Enrichers.Thread` package; logging enriched with `WithThreadId()`, `[ThreadId]` in both
  console and file templates.

### Fixed

- StatusControl binding: `RootNavigation.Navigated` event dynamically applies page DataContext.
- Unit tests: `SettingsViewModel` uses safe cast (`as App`); removed dummy `App` initialization in
  `MainViewModelTests` (caused AppDomain exceptions).
- Exit hangs: `Current.Shutdown()` invoked in the MainWindow closing handler (when not closing to
  tray) and in the tray context-menu exit handler.
- `App.Registration.cs` checks `GetRegisteredPath()` directly instead of `IsDefaultBrowser()` /
  `IsProtocolRegistered()` (which return false on path mismatch).
- Non-movable title bar after MessageBox: MainWindow explicitly focused on dialog completion.
- `DefaultBrowserService.IsDefaultBrowser` compares the actual registered `shell/open/command` exe
  path against the current process path instead of just checking the app-name key exists.

### Changed

- Startup initialization (silent update check, path re-registration, CLI URL dispatch) bound to
  `MainWindow.ContentRendered` instead of a timer.
- `StatusControl` moved out of `NavigationView.ContentOverlay` to a root-Grid sibling; margins clear
  the LeftFluent pane; `IsHitTestVisible="False"` on the overlay (inner `ui:InfoBar` stays
  interactive); replaced custom status border with native `ui:InfoBar`; `ScrollViewer` wrappers and
  bottom padding on pages/ListView to stop content clipping.

## [2026-07-09]

### Added

- MVVM & DI: `Microsoft.Extensions.DependencyInjection`, global `ServiceProvider`, zero code-behind
  `MainWindow`; sliced UserControls (`ToolbarView`, `RulesListView`, `FooterView`, `StatusControl`);
  pages `HomePage`, `AboutPage`, `SettingsPage`; `NavigationViewPageProvider`;
  `ui:NavigationView` with `PaneDisplayMode="LeftFluent"`.
- `KillBeforeBuild` MSBuild target (taskkill `AutoBrowser.exe` before build) to avoid file locks.
- About page third-party credits (WPF-UI, CommunityToolkit.Mvvm, Serilog, M.E.DI).

### Fixed

- Double-click edit bubble: `MouseBinding` moved into `Border.InputBindings` inside the
  `RulesListView` DataTemplate (event bubbling).
- Dynamic command enabling: `[NotifyCanExecuteChangedFor(...)]` on `_selectedRule` for Edit/Delete/
  MoveUp/MoveDown.
- `NavigationView.UpdateContent` NullReferenceException: initial page navigation moved from the
  constructor to the `Loaded` event.
- NavigationView icons: `Icon="{ui:SymbolIcon Home24}"` — `Icon` expects an `IconElement`, not a string.

### Changed

- 600+ line `App.xaml.cs` split into partials: `App.Tray.cs`, `App.Routing.cs`, `App.Registration.cs`,
  `App.Window.cs`.
- `RoutingRule` → `ObservableObject` with MVVM source generators; rules auto-save on collection or
  property change.
- Startup geometry defaults (1075×725) in `AppSettings`; sequential dialog flow (re-register before
  update check); dev-mode CLI detection ignores wrappers like `dotnet.exe`.
- Title bar drag fix: `ui:TitleBar` + `ui:NavigationView` wrapped in a root `Grid` (Row 0/1).
- `.gitignore` ignores JetBrains `.idea/` completely.

## [2026-07-08]

### Added

- Single-instance with tray restore (named-pipe IPC, mutex, `WindowForegroundHelper`).
- Push notifications for all URL routing results.
- Update-check throttling (once per hour via `LastUpdateCheckTime` setting).
- Re-register prompt when the app is moved.

### Fixed

- `BrowserDefinition` self-exclusion: `Path.GetFileName()` comparison instead of full path.
- Mutex disposed too early — promoted to class-level `_mutex`, disposed in `OnExit`.
- Non-movable title bar after MessageBox — switched to WPF UI `MessageBox` with `ShowDialogAsync()`.

### Changed

- `App.xaml.cs` refactored into focused helper methods; Win32 P/Invoke extracted to
  `WindowForegroundHelper`.

## [2026-07-06]

### Changed

- README synced to codebase (Fallback Browser, Auto-Update, System Tray independent minimize/close,
  tech stack).
- All Serena memories synced: `architecture` (Views folder, removed stale ReleaseInfo),
  `project_overview` (folder structure), `services` (AutoUpdater single-file, settings schema),
  `ui-behavior` (System Tray), `flow` (configurable `FallbackBrowserPath`).

## [2026-07-05]

### Added

- `opencode.json` configuring the Serena MCP server
  (`serena start-mcp-server --project-from-cwd`).
- Serilog file-sink logging in `AutoUpdater/Program.cs` (`Logs/updater-*.log`, 7-day rolling
  retention); single-file publishing (`PublishSingleFile=true`, `SelfContained=false`).

### Fixed

- AutoUpdater argument parsing: `UpdateService` sent named args (`--pid`, `--source`, `--target`,
  `--exe`, `--workspace`) but `Program.cs` expected positional args — added `ParseArguments()`;
  auto-update was silently failing.
- `App.xaml.cs ApplyTheme()` now loads existing settings before saving (theme no longer overwrites
  other settings).

### Changed

- AutoUpdater backup directory moved to `UpdateBackup/` next to the exe (was `%TEMP%`).
- Removed `PublishAot=true` (Serilog needs reflection) and unused `FileLogger.cs`.
- Main csproj uses `dotnet publish` for the updater (single `AutoUpdater.exe`, ~374KB).

## [2026-07-04]

### Added

- `src/AutoUpdater/` — console EXE for file swap, SHA256 validation, rollback, relaunch.
- `src/AutoBrowser/Services/UpdateService.cs` — GitHub release check + download + install.
- Post-build `CopyUpdater` MSBuild target copies `AutoUpdater*` into the main app output.
- `MainViewModel.CheckForUpdateAsync` (manual button with status feedback),
  `CheckForUpdateSilentAsync` (fire-and-forget on startup), `ShowUpdateDialogAsync`
  (WPF UI `MessageBox`, 500px wide).

### Changed

- CI revision number: `[Math]::Floor(UTC_seconds * 65535 / 86400)` (full UInt16 range); tag builds
  `MAJOR.MINOR.PATCH.REV`, branch builds `1.MINOR.PATCH.REV`; updater publish step
  `dotnet publish src\AutoUpdater\AutoUpdater.csproj`.
- Folder rename `src/AutoBrowser.AutoUpdater` → `src/AutoUpdater`; assembly rename
  `AutoBrowser.Updater` → `AutoUpdater` (all references updated); project reference uses
  `ReferenceOutputAssembly="false"`.

### Fixed

- Restored `Margin=8` on the content Grid (lost during ContentDialogHost removal).
- Removed third Cancel button from the update dialog; clearer missing-updater error message.
