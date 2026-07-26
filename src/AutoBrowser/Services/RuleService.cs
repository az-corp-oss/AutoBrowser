using System.IO;
using System.Text.Json;
using AutoBrowser.Helpers;
using AutoBrowser.Models;
using Serilog;

namespace AutoBrowser.Services;

public class RuleService : IRuleService
{
    private static readonly string DataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
    private static readonly string ConfigPath = Path.Combine(DataDir, "rules.json");

    private readonly IBrowserProvider _browserProvider;

    public RuleService(IBrowserProvider browserProvider)
    {
        _browserProvider = browserProvider;
    }

    public List<RuleGroup> LoadGroups()
    {
        try
        {
            EnsureDataDir();

            if (!File.Exists(ConfigPath))
                return GetDefaultGroups();

            var json = File.ReadAllText(ConfigPath);

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

            // Migration: load old flat format and convert to groups
            var savedRules = JsonSerializer.Deserialize<List<RoutingRule>>(json);
            if (savedRules == null || savedRules.Count == 0)
                return GetDefaultGroups();

            var groups = GetDefaultGroups();
            var migratedGroup = new RuleGroup
            {
                Id = UlidHelper.NewUlid(),
                Name = "Migrated Rules",
                IsEnabled = true,
                Sequence = 1,
                Rules = new System.Collections.ObjectModel.ObservableCollection<RoutingRule>(savedRules)
            };

            foreach (var g in groups)
            {
                g.Sequence++;
            }
            groups.Insert(0, migratedGroup);

            SaveGroups(groups);
            return groups;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "LoadGroups failed");
            return GetDefaultGroups();
        }
    }

    public void SaveGroups(List<RuleGroup> groups)
    {
        EnsureDataDir();

        var config = new RoutingConfig { Groups = groups };
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ConfigPath, json);
    }

    public async Task SaveGroupsAsync(List<RuleGroup> groups)
    {
        EnsureDataDir();

        var config = new RoutingConfig { Groups = groups };
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(ConfigPath, json);
    }

    public List<RoutingRule> LoadRules()
    {
        return LoadGroups()
            .Where(g => g.IsEnabled)
            .OrderBy(g => g.Sequence)
            .SelectMany(g => g.Rules.OrderBy(r => r.Sequence))
            .ToList();
    }

    public async Task<List<RuleGroup>> LoadGroupsAsync()
    {
        return await Task.Run(() => LoadGroups());
    }

    public async Task<List<RoutingRule>> LoadRulesAsync()
    {
        var groups = await LoadGroupsAsync();
        return groups
            .Where(g => g.IsEnabled)
            .OrderBy(g => g.Sequence)
            .SelectMany(g => g.Rules.OrderBy(r => r.Sequence))
            .ToList();
    }

    private static void EnsureDataDir()
    {
        if (!Directory.Exists(DataDir))
            Directory.CreateDirectory(DataDir);
    }

    private List<RuleGroup> GetDefaultGroups()
    {
        var browsers = _browserProvider.GetInstalledBrowsers();
        var edge = browsers.FirstOrDefault(b => b.Name.Contains("edge"));
        var chrome = browsers.FirstOrDefault(b => b.Name.Contains("chrome"));

        var groups = new List<RuleGroup>();

        // Default Group: Work
        var workRules = new System.Collections.ObjectModel.ObservableCollection<RoutingRule>();
        if (edge != null)
        {
            workRules.Add(new RoutingRule
            {
                Name = "Work sites",
                UrlPattern = @"(teams|office|sharepoint|outlook|microsoft)\.com",
                BrowserPath = edge.ExecutablePath,
                BrowserArguments = edge.ArgumentsTemplate,
                Sequence = 1
            });
        }
        groups.Add(new RuleGroup
        {
            Id = UlidHelper.NewUlid(),
            Name = "Work",
            IsEnabled = true,
            Sequence = 1,
            Rules = workRules
        });

        // Default Group: Social & Entertainment
        var socialRules = new System.Collections.ObjectModel.ObservableCollection<RoutingRule>();
        if (chrome != null)
        {
            socialRules.Add(new RoutingRule
            {
                Name = "Social & Entertainment",
                UrlPattern = @"(youtube|reddit|twitter|x\.com|instagram|facebook)\.com",
                BrowserPath = chrome.ExecutablePath,
                BrowserArguments = chrome.ArgumentsTemplate,
                Sequence = 1
            });
        }
        groups.Add(new RuleGroup
        {
            Id = UlidHelper.NewUlid(),
            Name = "Social",
            IsEnabled = true,
            Sequence = 2,
            Rules = socialRules
        });

        // Default Group: Development
        var devRules = new System.Collections.ObjectModel.ObservableCollection<RoutingRule>
        {
            new RoutingRule
            {
                Name = "Development",
                UrlPattern = @"(github|gitlab|stackoverflow|npmjs|docker)\.(com|io)",
                BrowserPath = chrome?.ExecutablePath ?? edge?.ExecutablePath ?? "",
                BrowserArguments = chrome?.ArgumentsTemplate ?? edge?.ArgumentsTemplate ?? "{url}",
                Sequence = 1
            }
        };
        groups.Add(new RuleGroup
        {
            Id = UlidHelper.NewUlid(),
            Name = "Development",
            IsEnabled = true,
            Sequence = 3,
            Rules = devRules
        });

        return groups;
    }
}
