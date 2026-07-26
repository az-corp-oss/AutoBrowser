# Conventions

## Code Style

- C# with C# 13+ features, XAML
- MVVM with `CommunityToolkit.Mvvm` ([ObservableProperty], [RelayCommand])
- Remove unused usings after every change (IDE0005)
- No comments, no emoji

## Naming

| Element | Convention | Example |
|---|---|---|
| Local functions | PascalCase | `TryAdd(...)` |
| Lambda parameters | `_` when unused | `_ => null` |
| Private fields | _camelCase | `_ruleService` |
| Data folders | Path.Combine relative | `Data/rules.json` |
| Service interfaces | I{Name} | `IRuleService` |
| ViewModels | {Page}ViewModel | `HomeViewModel` |

## Logging

Serilog with structured logging:
- Information — method entry/exit, completion status
- Debug — middle steps, variable values
- Verbose — fine-grained internal steps (below Debug)
- Error — exception handling with `Log.Error(ex, "message")`

## WPF UI Rules

- `ui:TextBlock` with `FontTypography` MUST have explicit `Foreground` binding (e.g. `Foreground="{DynamicResource TextFillColorPrimaryBrush}"`)
- Pages with `ScrollViewer`: set `ScrollViewer.CanContentScroll="False"`
- Dialogs: use WPF UI `MessageBox` with `ShowDialogAsync()`, NOT `System.Windows.MessageBox`

## Error Handling

- No empty catch blocks
- No general catch blocks without handling/logging
- Always log exceptions with `Log.Error(ex, "message")`

## Data Paths

| Type | Path |
|---|---|
| Rules | `Data/rules.json` |
| Settings | `Data/settings.json` |
| Default browser | `Data/default_browser.txt` |
| Logs | `Logs/` |
