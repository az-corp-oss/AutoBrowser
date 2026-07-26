# Suggested Commands

## Build

```bash
dotnet build src\AutoBrowser\AutoBrowser.csproj -o bin\staging
```

## Run

```bash
dotnet run --project src\AutoBrowser\AutoBrowser.csproj
```

Or run `bin\staging\AutoBrowser.exe` directly.

## Tests

```bash
dotnet test --settings AutoBrowser.Tests.runsettings src\AutoBrowser.Tests\AutoBrowser.Tests.csproj
```

Filter out slow UI tests:

```bash
dotnet test --settings AutoBrowser.Tests.runsettings src\AutoBrowser.Tests\AutoBrowser.Tests.csproj --filter "FullyQualifiedName!~UI"
```

## Fix Unused Usings

```bash
dotnet format src\AutoBrowser\AutoBrowser.csproj --diagnostics IDE0005
```

## Post-Change Verification

1. Build
2. Launch, wait 20s, close
3. Check logs in `bin\staging\Logs/` for `[ERR]`
4. Fix unused usings if any

## Windows-Specific

- Use `taskkill /F /IM AutoBrowser.exe` to kill running instances
- Registry path for protocol: `HKCU:\Software\Classes\AutoBrowserLink\shell\open\command`
