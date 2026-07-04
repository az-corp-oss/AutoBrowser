# SLNX Solution File

The solution file at `AutoBrowser.slnx` (repo root) uses the modern SLNX XML format.

```xml
<Solution>
  <Project Path="src/AutoBrowser/AutoBrowser.csproj" />
</Solution>```

- Uses forward slashes (`src/AutoBrowser/AutoBrowser.csproj`) per SLNX convention.
- Build configs: Debug, Release, Any CPU.
- SLNX has no built-in post-action command mechanism; auto-restore is handled by the .NET SDK on build.
- The `bin/` and `obj/` dirs are in `.gitignore`.
- Root-level `publish/` is also in `.gitignore`.
