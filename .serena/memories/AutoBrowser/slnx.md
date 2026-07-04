# SLNX Solution File

The solution file at `AutoBrowser.slnx` (repo root) uses the modern SLNX XML format.

```xml
<Solution>
  <Project Path="src/AutoBrowser/AutoBrowser.csproj" />
  <Project Path="src/AutoUpdater/AutoUpdater.csproj" />
</Solution>
```

- Uses forward slashes per SLNX convention.
- Build configs: Debug, Release, Any CPU.
- `bin/` and `obj/` dirs are in `.gitignore`.
- Root-level `publish/` also in `.gitignore`.
- AutoUpdater is a build dependency only (`ReferenceOutputAssembly="false"` in main csproj).
- Post-build `CopyUpdater` target copies `AutoUpdater*` files into main app output.
