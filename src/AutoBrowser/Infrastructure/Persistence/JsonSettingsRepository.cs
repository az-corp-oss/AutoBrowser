using System.IO;
using System.Text.Json;
using AutoBrowser.Models;
using Serilog;

namespace AutoBrowser.Services;

public class JsonSettingsRepository : ISettingsRepository
{
    private static readonly string SettingsPath = Path.Combine(Constants.DataDir, "settings.json");

    public AppSettings Load()
    {
        try
        {
            EnsureDataDir();

            if (!File.Exists(SettingsPath))
                return new AppSettings();

            var json = File.ReadAllText(SettingsPath);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to load settings, falling back to defaults");
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        EnsureDataDir();
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(SettingsPath, json);
    }

    public async Task<AppSettings> LoadAsync(CancellationToken ct = default)
    {
        try
        {
            EnsureDataDir();

            if (!File.Exists(SettingsPath))
                return new AppSettings();

            var json = await File.ReadAllTextAsync(SettingsPath, ct).ConfigureAwait(false);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to load settings, falling back to defaults");
            return new AppSettings();
        }
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken ct = default)
    {
        EnsureDataDir();
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(SettingsPath, json, ct).ConfigureAwait(false);
    }

    private static void EnsureDataDir()
    {
        if (!Directory.Exists(Constants.DataDir))
        {
            Log.Debug("Creating data directory: {Path}", Constants.DataDir);
            Directory.CreateDirectory(Constants.DataDir);
        }
    }
}