# Task Completion

## Verification Protocol
1. **Run Unit Tests**:
   `dotnet test src\AutoBrowser.Tests\AutoBrowser.Tests.csproj`
2. **Run Unit Tests via Husky (simulates pre-commit)**:
   `dotnet husky run --group pre-commit`
3. **Build Staging**:
   `dotnet build src\AutoBrowser\AutoBrowser.csproj -o bin\staging`
4. **Launch/Exit Verification**:
   `$proc = Start-Process -FilePath "bin\staging\AutoBrowser.exe" -PassThru; Start-Sleep -Seconds 20; Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue`
5. **Log Inspection**: Check logs under `bin\staging\Logs/` for `[ERR]` entries.
6. **Memory Synchronization (MANDATORY)**: ALWAYS document changes in `AutoBrowser/changes/YYYY-MM-DD` and sync any updated architectural memories. This step is required after every change, no exceptions.
