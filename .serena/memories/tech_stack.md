# Tech Stack

## Runtime
- .NET 10 (net10.0-windows) — preview SDK, `global.json` pins `10.0.0` with `latestMinor` rollforward
- Windows-only (WPF + WinForms interop for NotifyIcon)

## Dependencies (AutoBrowser.csproj)
- `CommunityToolkit.Mvvm 8.4.2` — MVVM source generators (`ObservableObject`, `RelayCommand`)
- `Microsoft.Extensions.DependencyInjection 10.0.9` — DI container
- `WPF-UI 4.3.0` — Fluent Design controls, theming (`ApplicationThemeManager`)
- `Serilog 4.4.0` + `Serilog.Enrichers.Thread 4.0.0` + `Serilog.Sinks.Console 6.1.1` + `Serilog.Sinks.File 7.0.0`
- WPF + WinForms (for `NotifyIcon` tray support)

## Test Dependencies (AutoBrowser.Tests.csproj)
- `xunit 2.9.3` + `xunit.runner.visualstudio 3.1.5`
- `Moq 4.20.72`
- `Microsoft.NET.Test.Sdk 18.7.0`
- `coverlet.collector 10.0.1`

## AutoUpdater
- Separate console project, published as standalone EXE
- Copied to output via MSBuild `PublishUpdater`/`CopyUpdater` targets
