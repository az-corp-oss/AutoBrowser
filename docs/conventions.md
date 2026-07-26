# Conventions

## Code Style

- **Language**: C# (.NET 10), XAML
- **Pattern**: MVVM with `INotifyPropertyChanged`
- **UI Library**: WPF UI (`xmlns:ui="http://schemas.lepo.co/wpfui/2022/xaml"`)
- **Data Storage**: JSON files in `Data/` folder next to EXE (portable)
- **Registry**: HKCU only, no admin elevation needed
- **Unused Usings**: Always remove unnecessary `using` directives (IDE0005). After any code change, verify no unused usings were left behind. `dotnet format --diagnostics IDE0005` can auto-fix them.

## Logging

Serilog with structured logging using message templates.

### Level Hierarchy

1. **Information** - Method entry and exit points (first and last logs)
   - Method calls with key parameters
   - Completion status with results
   - Example: `Log.Information("TryRoute called with URL: {Url}", url)`

2. **Debug** - Middle steps with braces/parameters
   - Variable values with placeholders
   - Conditional checks
   - Example: `Log.Debug("Loaded {Count} enabled rules", rules.Count)`

3. **Verbose** - Detailed internal steps (finest level)
   - Fine-grained operations
   - Individual loop iterations
   - Example: `Log.Verbose("Checking rule '{RuleName}'", rule.Name)`

4. **Error** - Exception handling
   - Example: `Log.Error(ex, "LaunchBrowser failed")`

**Note**: Serilog uses `Verbose` instead of `Trace` (equivalent level below Debug).

## Naming Conventions

| Element | Convention | Example |
|---|---|---|
| Local functions | `PascalCase` | `void TryAdd(...)` |
| Lambda parameters | `_` when unused | `_ => null` |
| Private fields | `_camelCase` | `_ruleService` |
| Data folders | `Path.Combine(...)` relative | `Data/rules.json` |
| Service interfaces | `I{Name}` | `IRuleService` |
| ViewModels | `{Page}ViewModel` | `HomeViewModel` |

## Error Handling

- Never use empty catch blocks `catch {}`.
- Avoid general catch blocks without handling or logging.
- Use `Log.Error(ex, "message")` for exception logging.

## WPF UI Rules

- **Typography Rule**: `ui:TextBlock` with `FontTypography` MUST specify an explicit `Foreground` brush binding (e.g. `Foreground="{DynamicResource TextFillColorPrimaryBrush}"`), otherwise it defaults to black in dark mode.
- **Page Layout**: Set `ScrollViewer.CanContentScroll="False"` on pages with their own `ScrollViewer` to disable NavigationView's built-in scroll.
- **Dialogs**: Use WPF UI `MessageBox` with `ShowDialogAsync()` instead of `System.Windows.MessageBox`.

## Data Paths

| Type | Path |
|---|---|
| Rules | `Data/rules.json` |
| Settings | `Data/settings.json` |
| Default browser | `Data/default_browser.txt` |
