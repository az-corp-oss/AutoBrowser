# Task Completion Checklist

After completing any coding task, run these commands in order:

## 1. Build

```bash
dotnet build src\AutoBrowser\AutoBrowser.csproj -o bin\staging
```

## 2. Fix Unused Usings

```bash
dotnet format src\AutoBrowser\AutoBrowser.csproj --diagnostics IDE0005
```

## 3. Run Tests

```bash
dotnet test --settings AutoBrowser.Tests.runsettings src\AutoBrowser.Tests\AutoBrowser.Tests.csproj
```

Filter out UI tests if desktop not available:

```bash
dotnet test --settings AutoBrowser.Tests.runsettings src\AutoBrowser.Tests\AutoBrowser.Tests.csproj --filter "FullyQualifiedName!~UI"
```

## 4. Manual Smoke Test (if UI changes)

Launch, wait 20s, close:

```powershell
$proc = Start-Process -FilePath "bin\staging\AutoBrowser.exe" -PassThru; Start-Sleep -Seconds 20; Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
```

Check logs for `[ERR]` entries in `bin\staging\Logs/`.

## 5. Update Memories

After significant changes, update relevant Serena memories.
