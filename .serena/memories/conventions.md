# Conventions

## General
- **Error Handling**: Never use empty catch blocks `catch {}`. Avoid general catch blocks without handling or logging.
- **Properties**: In Models and ViewModels, inherit from `ObservableObject` or use `CommunityToolkit.Mvvm` source generators.
- **UI Interaction**: Use data binding to ViewModel commands instead of code-behind events.

## WPF UI Conventions
- **Typography Rule**: `ui:TextBlock` with `FontTypography` MUST specify an explicit `Foreground` brush binding (e.g. `Foreground="{DynamicResource TextFillColorPrimaryBrush}"`), otherwise it defaults to black in dark mode.
- **Page Layout**: Set `ScrollViewer.CanContentScroll="False"` on pages with their own `ScrollViewer` to disable NavigationView's built-in scroll.
- **Dialogs**: Use WPF UI `MessageBox` with `ShowDialogAsync()` instead of `System.Windows.MessageBox`.

## Testing
- **Headless Environments**: UI tests (FlaUI) require an active desktop. Running headless causes `GetMainWindow()` to return null. Use `HomeViewModelRuleTests` for headless.
- **WPF UI Controls**: Add `AutomationProperties.AutomationId` to `ui:TextBox`/`ui:Button` for FlaUI testability (`x:Name` is not enough).
- **Git Hooks**: Husky.NET enforces unit tests on `git commit`. Manual trigger: `dotnet husky run --group pre-commit`.

## Logging Hierarchy
- `Information` - Method entry/exit points, key parameters, completion status.
- `Debug` - Intermediate steps, variable values, branch conditions.
- `Verbose` - Iterations, fine-grained details.
- `Error` - Exceptions.
- **Note**: Serilog uses `Verbose` (not `Trace`).
