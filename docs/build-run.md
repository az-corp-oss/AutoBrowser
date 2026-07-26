# Build & Run

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

Test timeouts are configured in `AutoBrowser.Tests.runsettings`:
- Per-test: 30s
- Session: 90s

## Post-Change Verification

After **any** code or XAML change, run this sequence:

1. **Build**
   ```bash
   dotnet build src\AutoBrowser\AutoBrowser.csproj -o bin\staging
   ```

2. **Launch, wait 20s, close**
   ```powershell
   $proc = Start-Process -FilePath "bin\staging\AutoBrowser.exe" -PassThru; Start-Sleep -Seconds 20; Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
   ```

3. **Check logs** for `[ERR]` entries in `bin\staging\Logs/`

4. **Test re-register prompt**
   ```powershell
   $regPath = "HKCU:\Software\Classes\AutoBrowserLink\shell\open\command"
   $original = (Get-ItemProperty -Path $regPath -Name "(default)")."(default)"
   Set-ItemProperty -Path $regPath -Name "(default)" -Value '"C:\OldLocation\AutoBrowser.exe" "%1"'
   $proc = Start-Process -FilePath "bin\staging\AutoBrowser.exe" -PassThru; Start-Sleep -Seconds 20; Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
   Set-ItemProperty -Path $regPath -Name "(default)" -Value $original
   ```

5. **Fix unused usings** if any were introduced
   ```bash
   dotnet format src\AutoBrowser\AutoBrowser.csproj --diagnostics IDE0005
   ```

If tests fail or the app crashes, fix before proceeding.
