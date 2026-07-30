using System.IO;
using System.Text.Json;
using AutoBrowser.Helpers;
using AutoBrowser.Models;
using Serilog;

namespace AutoBrowser.Services;

public class JsonRuleRepository : IRuleRepository
{
    private static readonly string ConfigPath = Path.Combine(Constants.DataDir, "rules.json");

    public List<RuleGroup> LoadGroups()
    {
        EnsureDataDir();

        if (!File.Exists(ConfigPath))
            return [];

        var json = File.ReadAllText(ConfigPath);

        if (string.IsNullOrWhiteSpace(json))
            return [];

        RoutingConfig? config = null;
        try
        {
            config = JsonSerializer.Deserialize<RoutingConfig>(json);
        }
        catch (Exception ex)
        {
            Log.Verbose(ex, "Failed to deserialize new RoutingConfig format, trying legacy format");
        }

        if (config?.Groups is { Count: > 0 })
            return config.Groups;

        var savedRules = JsonSerializer.Deserialize<List<RoutingRule>>(json);
        if (savedRules == null || savedRules.Count == 0)
            return [];

        return
        [
            new RuleGroup
            {
                Id = UlidHelper.NewUlid(),
                Name = "Migrated Rules",
                IsEnabled = true,
                Sequence = 1,
                Rules = new System.Collections.ObjectModel.ObservableCollection<RoutingRule>(savedRules)
            }
        ];
    }

    public void SaveGroups(List<RuleGroup> groups)
    {
        EnsureDataDir();
        var config = new RoutingConfig { Groups = groups };
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ConfigPath, json);
    }

    public async Task<List<RuleGroup>> LoadGroupsAsync(CancellationToken ct = default)
    {
        EnsureDataDir();

        if (!File.Exists(ConfigPath))
            return [];

        var json = await File.ReadAllTextAsync(ConfigPath, ct).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(json))
            return [];

        RoutingConfig? config = null;
        try
        {
            config = JsonSerializer.Deserialize<RoutingConfig>(json);
        }
        catch (Exception ex)
        {
            Log.Verbose(ex, "Failed to deserialize new RoutingConfig format, trying legacy format");
        }

        if (config?.Groups is { Count: > 0 })
            return config.Groups;

        var savedRules = JsonSerializer.Deserialize<List<RoutingRule>>(json);
        if (savedRules == null || savedRules.Count == 0)
            return [];

        return
        [
            new RuleGroup
            {
                Id = UlidHelper.NewUlid(),
                Name = "Migrated Rules",
                IsEnabled = true,
                Sequence = 1,
                Rules = new System.Collections.ObjectModel.ObservableCollection<RoutingRule>(savedRules)
            }
        ];
    }

    public async Task SaveGroupsAsync(List<RuleGroup> groups, CancellationToken ct = default)
    {
        EnsureDataDir();

        var config = new RoutingConfig { Groups = groups };
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(ConfigPath, json, ct).ConfigureAwait(false);
    }

    private static void EnsureDataDir()
    {
        if (!Directory.Exists(Constants.DataDir))
            Directory.CreateDirectory(Constants.DataDir);
    }
}