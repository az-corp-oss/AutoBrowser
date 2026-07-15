# AutoBrowser — UI & Behavior

## Theme Toggle
- **Control**: `ui:ToggleSwitch` in Settings Page, bound to `SettingsViewModel.IsDarkTheme`
- **Enum**: `AppThemeMode` — `Light` (0), `Dark` (1) — `System` removed
- **Default**: `Light` (set in `App.xaml` via `<ui:ThemesDictionary Theme="Light"/>`)
- **Flow**: `App.OnStartup` → `base.OnStartup` → `ShowMainWindow()` → `MainWindow_Loaded` → `ApplyTheme(savedTheme)`
- **Theme is applied AFTER MainWindow exists** (Gallery pattern) — `ApplicationThemeManager.Apply()` needs the window to update its resources
- **SettingsViewModel**: uses `ApplicationThemeManager.GetAppTheme()` to read current theme, `ApplicationThemeManager.Apply()` on toggle, subscribes to `ApplicationThemeManager.Changed` event
- **`IsDarkTheme`** is computed property wrapping `CurrentApplicationTheme == ApplicationTheme.Dark`

## Dark Theme Typography
- **RULE**: All `ui:TextBlock` in views MUST have explicit `Foreground` brush binding — `FontTypography` alone does NOT inherit theme foreground
- Primary text: `Foreground="{DynamicResource TextFillColorPrimaryBrush}"`
- Secondary text: `Foreground="{DynamicResource TextFillColorSecondaryBrush}"`
- Tertiary text: `Foreground="{DynamicResource TextFillColorTertiaryBrush}"`

## System Tray (Managed by App.xaml.cs)
- `NotifyIcon` with app icon + context menu (Show Window, Exit)
- Minimize → hides to tray + balloon tip + restore-on-click (`MainWindow_StateChanged`) — when `MinimizeToTray` enabled
- Close → minimizes to tray + balloon tip + restore-on-click (cancel `MainWindow_Closing` event) — when `CloseToTray` enabled
- Balloon tip click → `ShowWindow()` restores window (hooked via `_trayIcon.BalloonTipClicked`)
- Only Exit menu item truly terminates
- `SaveWindowState()` called on close
- `ShowNotification()` uses single `_trayIcon` (no per-call NotifyIcon) so `BalloonTipClicked` works
- Both options persist to `Data/settings.json`

## Update Check
- **Button**: "Check Update" in toolbar, bound to `CheckForUpdateCommand`, disabled while checking/downloading
- **Auto-check**: `_ = CheckForUpdateSilentAsync()` runs on startup, silently ignores no-update/offline
- **Throttled**: Checks once per hour via `LastUpdateCheckTime` in settings
- **Dialog**: `Wpf.Ui.Controls.MessageBox` (500px wide) with Yes/No — no third Cancel button
- **Silent flow**: fires `ShowUpdateDialogAsync` only when newer version found
- **Manual flow**: shows status for checking/up-to-date/failed, then delegates to `ShowUpdateDialogAsync`

## Single Instance (Managed by App.xaml.cs)
- Named pipe IPC (`System.IO.Pipes`) for single-instance signaling
- `SingleInstanceService` manages pipe server in background `Task.Run` loop
- Protocol: `"SHOW"` or `"SHOW|<url>"` — brings existing window to front
- `WindowForegroundHelper` uses Win32 P/Invoke (`SetForegroundWindow`, `ShowWindow`)
- `App.ActivateFromTray(url)` restores window and processes forwarded URL

## Re-Register Prompt (Managed by App.xaml.cs)
- On startup, compares registered protocol/default browser paths with `Environment.ProcessPath`
- If path differs (app was moved), shows WPF UI `MessageBox` with old/new paths
- Uses `ShowDialogAsync()` (async) with owner set to MainWindow
- Yes: unregisters and re-registers both handlers, shows notification
- No: logs decline, continues normally

### Testing Re-Register Prompt
- Save current path: `(Get-ItemProperty -Path $regPath -Name "(default)")."(default)"`
- Fake old path: `Set-ItemProperty ... "C:\OldLocation\AutoBrowser.exe"`
- Launch app → dialog should appear
- Restore original path
- Verify log shows `"App path has changed"`

## WPF UI Reference
- **Always check WPF UI Gallery source** (`lepoco/wpfui` on GitHub) for behavior/fixes before writing workarounds
- Key pattern: `NavigationViewContentPresenter` wraps pages in `DynamicScrollViewer` — set `ScrollViewer.CanContentScroll="False"` on pages with their own ScrollViewer to disable NavView's built-in scroll
- Related issues: #1041, #1230, #1503, #1606; PR #1610 fixed always-scrolling behavior

## Build & Run
- **Verify compilation (app running)**: `dotnet build src\AutoBrowser\AutoBrowser.csproj -o bin\staging` — does NOT kill the running app
- **Full build (kills app)**: `dotnet build src\AutoBrowser\AutoBrowser.csproj` — only when intentionally overwriting the running binary
- **Run**: `dotnet run --project src\AutoBrowser\AutoBrowser.csproj` or run EXE directly
- **Post-change ritual**: verify with `bin\staging` first (no kill), then exit app and run fresh copy
- **Tests**: `dotnet test src\AutoBrowser.Tests\AutoBrowser.Tests.csproj` (44 tests)