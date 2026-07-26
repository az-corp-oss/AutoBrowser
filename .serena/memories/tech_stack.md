# Tech Stack

## Runtime

- .NET 10.0 (SDK 10.0.0, `rollForward: latestMinor`, no prereleases)
- `net10.0-windows` TFM
- WPF + WinForms (for FlaUI UI tests)

## Key Packages (Main)

| Package | Version | Purpose |
|---|---|---|
| CommunityToolkit.Mvvm | 8.4.2 | MVVM source generators ([ObservableProperty], [RelayCommand]) |
| Microsoft.Extensions.DependencyInjection | 10.0.9 | DI container |
| Serilog | 4.4.0 | Structured logging |
| Serilog.Sinks.File | 7.0.0 | Log file output |
| Serilog.Sinks.Console | 6.1.1 | Console log output |
| Serilog.Enrichers.Thread | 4.0.0 | Thread enrichment |
| System.CommandLine | 2.0.0-beta4 | CLI argument parsing |
| WPF-UI | 4.3.0 | Modern WPF UI controls |

## Key Packages (Tests)

| Package | Version | Purpose |
|---|---|---|
| xunit | 2.9.3 | Test framework |
| Moq | 4.20.72 | Mocking |
| FlaUI.UIA3 | 5.0.0 | WPF UI automation |
| coverlet.collector | 10.0.1 | Code coverage |

## Build

- SDK-style csproj with `<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>`
- `<ImplicitUsings>enable</ImplicitUsings>` and `<Nullable>enable</Nullable>`
- Custom MSBuild targets: KillBeforeBuild, PublishUpdater, CopyUpdater
