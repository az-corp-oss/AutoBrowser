# AutoBrowser — Architecture

## Overview
WPF desktop app for Windows. Registers as `autobrowser://` protocol handler and optional default browser, then routes URLs to user-configured browsers by regex rules. Lives in system tray, minimizes on close (minimize-to-tray and close-to-tray independently configurable).

## Dependency Injection (DI) & MVVM
- Migrated to a Dependency Injection container using `Microsoft.Extensions.DependencyInjection`.
- Core services, `MainWindow`, pages, and `MainViewModel` are registered on startup in `App.xaml.cs`.
- `MainWindow` uses page-based navigation via a `<ui:NavigationView>` that resolves pages (`HomePage`, `AboutPage`, `SettingsPage`) dynamically using `INavigationViewPageProvider` from the DI container.
- Initial navigation is triggered inside the Window's `Loaded` handler to prevent timing issues before control templates are initialized.
- `RoutingRule` properties use CommunityToolkit.Mvvm source generators and auto-saves to rule service on property change events.

## AutoUpdater (src/AutoUpdater/)
- Standalone single-file console EXE for file swap + relaunch (no runtime dependency)
- Build dependency via `ReferenceOutputAssembly="false"` in main csproj
- Post-build MSBuild target copies all `AutoUpdater*` files into main app output

## Project Structure
```
AutoBrowser/
├── app.ico                      # Multi-res icon
├── App.xaml / App.xaml.cs       # Entry: Configure DI services, Single-instance mutex, CLI URL dispatch, ApplyTheme(), Window lifecycle events (Loaded, Closing, StateChanged), Tray Icon management
├── MainWindow.xaml / .cs        # UI: NavigationView is the sole root (no wrapping Grid, no custom TitleBar). ExtendsContentIntoTitleBar is NOT used — OS title bar ensures reliable drag + focus. Mica backdrop via WindowBackdropType="Mica".
├── AutoBrowser.csproj           # SDK-style, net10.0-windows, WPF + WinForms + WPF-UI + Microsoft.Extensions.DependencyInjection
├── Models/
│   ├── AppSettings.cs           # ThemeMode (Light/Dark)
│   ├── AppThemeMode.cs          # Light=0, Dark=1 (System removed)
│   ├── RoutingRule.cs           # Name, Pattern, BrowserPath, Sequence, IsEnabled, IsMatch (inherits from ObservableObject)
│   └── BrowserDefinition.cs     # Browser detection logic
├── Services/
│   ├── IRuleService.cs / RuleService.cs           # Rules JSON persistence + auto-merge
│   ├── ISettingsService.cs / SettingsService.cs   # Settings JSON persistence
│   ├── IProtocolService.cs / ProtocolService.cs   # autobrowser:// registry ops
│   ├── IDefaultBrowserService.cs / DefaultBrowserService.cs  # Default browser reg
│   ├── SingleInstanceService.cs                   # Named pipe IPC for single-instance
│   ├── UrlInterceptorService.cs                   # URL matching + browser launch
│   ├── NavigationViewPageProvider.cs             # Page resolver for NavigationView
│   └── UpdateService.cs + ReleaseInfo record      # GitHub release check, download, update install
├── Views/
│   ├── HomePage.xaml / .cs         # Page: Sticky header + scrollable toolbar/rules. Uses ScrollViewer.CanContentScroll="False" to disable NavView DynamicScrollViewer.
│   ├── SettingsPage.xaml / .cs     # Page: Sticky header + scrollable settings cards. Uses ScrollViewer.CanContentScroll="False".
│   ├── AboutPage.xaml / .cs        # Page: Sticky header + scrollable app info + credits. Uses ScrollViewer.CanContentScroll="False".
│   ├── ToolbarView.xaml / .cs      # Sliced control: Add/Edit/Delete, Up/Down, Update check, URL test
│   ├── RulesListView.xaml / .cs    # Sliced control: ListView of Rules with double-click edit
│   ├── StatusControl.xaml / .cs    # Floating overlay control at bottom-center, IsHitTestVisible="False" on root
│   ├── RuleEditorView.xaml / .cs   # FluentWindow: Add/Edit rule with browser dropdown
│   └── RuleTesterView.xaml / .cs   # FluentWindow: Test URL input dialog
├── ViewModels/
│   ├── MainViewModel.cs           # ViewModel containing commands and state properties for views.
│   └── SettingsViewModel.cs       # Theme toggle, browser registration, tray settings. Uses ApplicationThemeManager directly.
```

## Key Design Decisions
- **Dependency Injection**: Services and views resolved from centralized `ServiceProvider` in `App.xaml.cs`. ViewModels registered as `AddTransient`, services as `AddSingleton`.
- **Portable**: All data in `Data/` folder next to EXE, not %APPDATA%
- **Per-user registry**: No admin elevation needed
- **Default browser via HKCU**: `RegisteredApplications` approach (user confirms in Settings)
- **Auto-merge rules**: Default rules merged by `Name`, never overwrites user edits
- **Infinite-loop protection**: Always launch unmatched URLs via saved-default-browser EXE path, not shell association

## Dark Theme Typography (Critical Rule)
All `ui:TextBlock` with `FontTypography` MUST have explicit `Foreground` brush binding. `FontTypography` alone does NOT inherit theme foreground — WPF defaults to black text.
- Primary: `Foreground="{DynamicResource TextFillColorPrimaryBrush}"`
- Secondary: `Foreground="{DynamicResource TextFillColorSecondaryBrush}"`

## Theme Initialization (Gallery Pattern)
`ApplicationThemeManager.Apply()` requires `MainWindow` to exist. Startup order: `base.OnStartup` → `ShowMainWindow()` → `MainWindow_Loaded` → `ApplyTheme(savedTheme)`.

## Page Layout Pattern
Pages use sticky header + scrollable content: `Grid` with Row="0" (Auto header) + Row="1" (*) ScrollViewer. `ScrollViewer.CanContentScroll="False"` disables NavView's built-in DynamicScrollViewer.