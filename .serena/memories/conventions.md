# Conventions

\1
- **Error Handling**: Never use empty catch blocks `catch {}`. Avoid general catch blocks without handling or logging to prevent "Empty general catch clause suppresses any errors" warnings.

\2
- Properties in Models and ViewModels inherit from `ObservableObject` or use `CommunityToolkit.Mvvm` source generators.
- UI elements use data binding to ViewModel commands instead of code-behind events.

## WPF UI Conventions
- **Typography Rule**: `ui:TextBlock` with `FontTypography` MUST specify an explicit `Foreground` brush binding (e.g. `Foreground="{DynamicResource TextFillColorPrimaryBrush}"`), otherwise it defaults to black in dark mode.
- **Page Layout**: Set `ScrollViewer.CanContentScroll="False"` on pages with their own `ScrollViewer` to disable NavigationView's built-in scroll.
- **Dialogs**: Use WPF UI `MessageBox` with `ShowDialogAsync()` instead of `System.Windows.MessageBox`.

## Testing
- UI tests (FlaUI) require active desktop environment. Running headless (like CI/SSH without desktop session) causes `GetMainWindow()` to return null/fail. Run `HomeViewModelRuleTests` unit tests under headless environments.
- **WPF UI Controls**: Add `AutomationProperties.AutomationId` to `ui:TextBox`/`ui:Button` for FlaUI testability — `x:Name` alone is not exposed as AutomationId.
- **Git Hooks**: Husky.NET enforces unit tests on `git commit`. Manual trigger: `dotnet husky run --group pre-commit`.

## Logging Hierarchy
- `Information` - Method entry/exit points, key parameters, completion status.
- `Debug` - Intermediate steps, variable values, branch conditions.
- `Verbose` - Iterations, fine-grained details.
- `Error` - Exceptions.
- Serilog uses `Verbose` (not `Trace`).
