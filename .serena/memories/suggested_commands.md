# Suggested Commands

## Codebase Commands
- **Verify compilation (non-destructive)**:
  `dotnet build src\AutoBrowser\AutoBrowser.csproj -o bin\staging`
- **Full build (destroys running app)**:
  `dotnet build src\AutoBrowser\AutoBrowser.csproj`
- **Run project**:
  `dotnet run --project src\AutoBrowser\AutoBrowser.csproj`
- **Run unit tests (full)**:
  `dotnet test src\AutoBrowser.Tests\AutoBrowser.Tests.csproj`
- **Run unit tests (no UI)**:
  `dotnet test src\AutoBrowser.Tests\AutoBrowser.Tests.csproj --filter "FullyQualifiedName!~UI"`
- **Run husky pre-commit hook manually**:
  `dotnet husky run --group pre-commit`
- **Restore husky tools (after clone)**:
  `dotnet tool restore && dotnet husky install`

## Windows Registry Checks
- **Read protocol command**:
  `Get-ItemProperty -Path "HKCU:\Software\Classes\AutoBrowserLink\shell\open\command" -Name "(default)"`
- **Write fake registry path (testing prompt)**:
  `Set-ItemProperty -Path "HKCU:\Software\Classes\AutoBrowserLink\shell\open\command" -Name "(default)" -Value '"C:\OldLocation\AutoBrowser.exe" "%1"'`
