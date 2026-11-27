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

Default run (UI tests excluded automatically via runsettings):

```bash
dotnet test --settings AutoBrowser.Tests.runsettings src\AutoBrowser.Tests\AutoBrowser.Tests.csproj
```

During development, run only the affected tests (faster, no UI):

```bash
dotnet test --settings AutoBrowser.Tests.runsettings src\AutoBrowser.Tests\AutoBrowser.Tests.csproj --filter "FullyQualifiedName~<TestClass>"
```

Run UI tests explicitly (only when needed / before release):

```bash
dotnet test src\AutoBrowser.Tests\AutoBrowser.Tests.csproj --filter "FullyQualifiedName~UI"
```

Automatic pre-push validation (Husky): unit tests always run with `AutoBrowser.Tests.runsettings`; UI tests run automatically only when a `*.xaml` or `*.xaml.cs` file changed in the pushed commits. A failing UI test blocks the push. The explicit UI-test command above remains available for manual runs.

Test timeouts are configured in `AutoBrowser.Tests.runsettings`:
- Per-test: 20s
- Session: 600s

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
